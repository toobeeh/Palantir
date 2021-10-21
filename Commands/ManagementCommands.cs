﻿using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Linq;
using MoreLinq.Extensions;
using Newtonsoft.Json;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System.Globalization;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;

namespace Palantir.Commands
{
    public class ManagementCommands : BaseCommandModule
    {
        [Description("Creates a new theme ticket which can be used by anyone to add a new theme to typo.")]
        [Command("themeticket")]
        [RequirePermissionFlag((byte)4)]
        public async Task CreateThemeTicket(CommandContext context)
        {
            TypoThemeEntity empty = new TypoThemeEntity();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const int length = 6;
            Random random = new Random();
            PalantirDbContext dbcontext = new PalantirDbContext();
            do
            {
                empty.Ticket = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            }
            while (dbcontext.Themes.Any(theme => theme.Ticket == empty.Ticket));
            dbcontext.Themes.Add(empty);
            dbcontext.SaveChanges();
            dbcontext.Dispose();
            string response = "Successfully created theme ticket!\nUse it with the command `>addtheme`.\nTicket: `" + empty.Ticket + "`";
            if (context.Channel.IsPrivate) await context.RespondAsync(response);
            else await context.Member.SendMessageAsync(response);
        }

        [Description("Adds a new theme. You'll need to have a valid theme ticket.")]
        [Command("addtheme")]
        public async Task AddTheme(CommandContext context)
        {
            var interactivity = Program.Client.GetInteractivity();
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within one minute with your theme ticket.\nThe ticket is a 6-digit code which Palantir moderators can generate.");
            InteractivityResult<DiscordMessage> msgTicket = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(1));
            TypoThemeEntity ticket = new TypoThemeEntity();
            ticket.Ticket = msgTicket.TimedOut ? "0" : msgTicket.Result.Content;
            PalantirDbContext dbcontext = new PalantirDbContext();
            ticket = dbcontext.Themes.FirstOrDefault(theme => theme.Ticket == ticket.Ticket && theme.Theme != " ");
            dbcontext.Dispose();
            if (ticket is null)
            {
                await Program.SendEmbed(context.Channel, "Invalid theme ticket :(", "");
                return;
            }
            ticket.Author = context.User.Username;
            //get name
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with the theme Name.");
            InteractivityResult<DiscordMessage> msgName = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
            if (msgName.TimedOut)
            {
                await Program.SendEmbed(context.Channel, "Timed out :(", "");
                return;
            }
            ticket.Name = msgName.Result.Content;
            // get theme
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with the theme text.");
            InteractivityResult<DiscordMessage> msgTheme = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
            if (msgTheme.TimedOut)
            {
                await Program.SendEmbed(context.Channel, "Timed out :(", "");
                return;
            }
            ticket.Theme = msgTheme.Result.Content;
            // get description
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with the theme description.");
            InteractivityResult<DiscordMessage> msgDesc = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
            if (msgDesc.TimedOut)
            {
                await Program.SendEmbed(context.Channel, "Timed out :(", "");
                return;
            }
            ticket.Description = msgDesc.Result.Content;
            // get thumbnail landing
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with a screenshot from the skribbl landing page.\nRespond without attachment to skip.");
            InteractivityResult<DiscordMessage> msgLanding = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
            if (msgLanding.TimedOut)
            {
                await Program.SendEmbed(context.Channel, "Timed out :(", "");
                return;
            }
            if (msgLanding.Result.Attachments.Count > 0) ticket.ThumbnailLanding = msgLanding.Result.Attachments[0].Url;
            // get thumbnail game
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with a screenshot from skribbl in-game.\nRespond without attachment to skip.");
            InteractivityResult<DiscordMessage> msgGame = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
            if (msgGame.TimedOut)
            {
                await Program.SendEmbed(context.Channel, "Timed out :(", "");
                return;
            }
            if (msgGame.Result.Attachments.Count > 0) ticket.ThumbnailGame = msgGame.Result.Attachments[0].Url;

            dbcontext = new PalantirDbContext();
            dbcontext.Themes.Update(ticket);
            dbcontext.SaveChanges();
            dbcontext.Dispose();
            await Program.SendEmbed(context.Channel, "Theme successfully added!", "You can now use following link to instantly share your theme:");
            await context.RespondAsync("https://typo.rip/t?ticket=" + ticket.Ticket);
        }

        [Description("Execute a bash command from the pi root")]
        [Command("bash")]
        [RequirePermissionFlag((byte)2)]
        public async Task Bash(CommandContext context, params string[] command)
        {
            string commandDelimited = command.ToDelimitedString(" ");
            string res = ("cd /home/pi/ && " + commandDelimited).Bash();
            await Program.SendEmbed(context.Channel, "**pi@raspberrypi: ~ $** " + commandDelimited, res != "" ? res : "Error.");
        }

        [Description("Execute a sql command in the palantir database")]
        [Command("sql")]
        [RequirePermissionFlag((byte)2)]
        public async Task Sql(CommandContext context, params string[] sql)
        {
            string sqlDelimited = sql.ToDelimitedString(" ");
            string res = ("sqlite3 /home/pi/Database/palantir.db \"" + sqlDelimited + "\"").Bash();
            await Program.SendEmbed(context.Channel, sqlDelimited, res != "" ? res : "Error.");
        }

        [Description("Set a member flag.")]
        [Command("flag")]
        public async Task Flag(CommandContext context, [Description("The id of the member to flag")] ulong id, [Description("The new flag")] int flag = -1)
        {
            if (flag == -1)
            {
                DiscordUser target = await Program.Client.GetUserAsync(id);
                PermissionFlag getperm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(target));
                string getDesc = "Flag[0] Bubble Farming - "
                    + getperm.BubbleFarming + "\nFlag[1] Bot Admin - "
                    + getperm.BotAdmin + "\nFlag[2] Moderator - "
                    + getperm.Moderator + "\nFlag[3] Unlimited Cloud - "
                    + getperm.CloudUnlimited + "\nFlag[4] Patron - "
                    + getperm.Patron + "\nFlag[5] Permanent Ban - "
                    + getperm.Permanban + "\nFlag[6] Drop Ban - "
                    + getperm.Dropban + "\nFlag[7] Patronizer - "
                    + getperm.Patronizer;
                await Program.SendEmbed(context.Channel, "The flags of " + target.Username, getDesc);
                return;
            }
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "o_o", "You can't set other's flags!");
                return;
            }
            Program.Feanor.SetFlagByID(id.ToString(), flag);
            string name = (await Program.Client.GetUserAsync(id)).Mention;
            PermissionFlag newFlag = new PermissionFlag((byte)flag);
            string desc = "Flag[0] Bubble Farming - "
                    + newFlag.BubbleFarming + "\nFlag[1] Bot Admin - "
                    + newFlag.BotAdmin + "\nFlag[2] Moderator - "
                    + newFlag.Moderator + "\nFlag[3] Unlimited Cloud - "
                    + newFlag.CloudUnlimited + "\nFlag[4] Patron - "
                    + newFlag.Patron + "\nFlag[5] Permanent Ban - "
                    + newFlag.Permanban + "\nFlag[6] Drop Ban - "
                    + newFlag.Dropban + "\nFlag[7] Patronizer - "
                    + newFlag.Patronizer;
            await Program.SendEmbed(context.Channel, "*magic happened*", "The flag of " + name + " was set to " + flag + "\n" + desc);
        }

        [Description("Reboots the bot & pulls from git.")]
        [Command("hardreboot")]
        [RequirePermissionFlag((byte)4)] // 4 -> mod
        public async Task Reboot(CommandContext context)
        {
            string upd = "git -C /home/pi/Palantir pull".Bash();
            upd += "\n\n Latest commit: " + ("git log --oneline -1".Bash());
            await Program.SendEmbed(context.Channel, "[literally dies...]", "You made me do this!!!\n\n**Update result:**\n" + upd);
            string op = "sudo service palantir restart".Bash();
            Environment.Exit(0);
        }

    }
}