using System.Diagnostics;
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
using Palantir.Model;
using Palantir.PalantirCommandModule;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Palantir.Commands
{
    public class ManagementCommands : PalantirCommandModule.PalantirCommandModule
    {
        //[Description("Creates a new theme ticket which can be used by anyone to add a new theme to typo.")]
        //[Command("themeticket")]
        //[RequirePermissionFlag(PermissionFlag.MOD)]
        //public async Task CreateThemeTicket(CommandContext context)
        //{
        //    Theme empty = new Theme();
        //    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        //    const int length = 6;
        //    Random random = new Random();
        //    PalantirContext dbcontext = new PalantirContext();
        //    do
        //    {
        //        empty.Ticket = new string(Enumerable.Repeat(chars, length)
        //        .Select(s => s[random.Next(s.Length)]).ToArray());
        //    }
        //    while (dbcontext.Themes.Any(theme => theme.Ticket == empty.Ticket));
        //    dbcontext.Themes.Add(empty);
        //    dbcontext.SaveChanges();
        //    dbcontext.Dispose();
        //    string response = "Successfully created theme ticket!\nUse it with the command `>addtheme`.\nTicket: `" + empty.Ticket + "`";
        //    if (context.Channel.IsPrivate) await context.RespondAsync(response);
        //    else await context.Member.SendMessageAsync(response);
        //}

        //[Description("Adds a new theme. You'll need to have a valid theme ticket.")]
        //[Command("addtheme")]
        //public async Task AddTheme(CommandContext context)
        //{
        //    var interactivity = Program.Client.GetInteractivity();
        //    await Program.SendEmbed(context.Channel, "Add a theme", "Respond within one minute with your theme ticket.\nThe ticket is a 6-digit code which Palantir moderators can generate.");
        //    InteractivityResult<DiscordMessage> msgTicket = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(1));
        //    Theme ticket = new Theme();
        //    ticket.Ticket = msgTicket.TimedOut ? "0" : msgTicket.Result.Content;
        //    PalantirContext dbcontext = new PalantirContext();
        //    ticket = dbcontext.Themes.FirstOrDefault(theme => theme.Ticket == ticket.Ticket && theme.Theme1 != " ");
        //    dbcontext.Dispose();
        //    if (ticket is null)
        //    {
        //        await Program.SendEmbed(context.Channel, "Invalid theme ticket :(", "");
        //        return;
        //    }
        //    ticket.Author = context.User.Username;
        //    //get name
        //    await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with the theme Name.");
        //    InteractivityResult<DiscordMessage> msgName = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
        //    if (msgName.TimedOut)
        //    {
        //        await Program.SendEmbed(context.Channel, "Timed out :(", "");
        //        return;
        //    }
        //    ticket.Name = msgName.Result.Content;
        //    // get theme
        //    await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with the theme text.");
        //    InteractivityResult<DiscordMessage> msgTheme = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
        //    if (msgTheme.TimedOut)
        //    {
        //        await Program.SendEmbed(context.Channel, "Timed out :(", "");
        //        return;
        //    }
        //    ticket.Theme1 = msgTheme.Result.Content;
        //    // get description
        //    await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with the theme description.");
        //    InteractivityResult<DiscordMessage> msgDesc = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
        //    if (msgDesc.TimedOut)
        //    {
        //        await Program.SendEmbed(context.Channel, "Timed out :(", "");
        //        return;
        //    }
        //    ticket.Description = msgDesc.Result.Content;
        //    // get thumbnail landing
        //    await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with a screenshot from the skribbl landing page.\nRespond without attachment to skip.");
        //    InteractivityResult<DiscordMessage> msgLanding = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
        //    if (msgLanding.TimedOut)
        //    {
        //        await Program.SendEmbed(context.Channel, "Timed out :(", "");
        //        return;
        //    }
        //    if (msgLanding.Result.Attachments.Count > 0) ticket.ThumbnailLanding = msgLanding.Result.Attachments[0].Url;
        //    // get thumbnail game
        //    await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with a screenshot from skribbl in-game.\nRespond without attachment to skip.");
        //    InteractivityResult<DiscordMessage> msgGame = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
        //    if (msgGame.TimedOut)
        //    {
        //        await Program.SendEmbed(context.Channel, "Timed out :(", "");
        //        return;
        //    }
        //    if (msgGame.Result.Attachments.Count > 0) ticket.ThumbnailGame = msgGame.Result.Attachments[0].Url;

        //    dbcontext = new PalantirContext();
        //    dbcontext.Themes.Update(ticket);
        //    dbcontext.SaveChanges();
        //    dbcontext.Dispose();
        //    await Program.SendEmbed(context.Channel, "Theme successfully added!", "You can now use following link to instantly share your theme:");
        //    await context.RespondAsync("https://typo.rip/t?ticket=" + ticket.Ticket);
        //}

        //[Description("Execute a bash command from the pi root")]
        //[Command("bash")]
        //[RequirePermissionFlag(PermissionFlag.ADMIN)]
        //public async Task Bash(CommandContext context, params string[] command)
        //{
        //    string commandDelimited = command.ToDelimitedString(" ");
        //    string res = ("cd /home/pi/ && " + commandDelimited).Bash();
        //    await Program.SendEmbed(context.Channel, "**pi@raspberrypi: ~ $** " + commandDelimited, res != "" ? res : "Error.");
        //}

        //[Description("Execute a sql command in the palantir database")]
        //[Command("sql")]
        //[RequirePermissionFlag(PermissionFlag.ADMIN)]
        //public async Task Sql(CommandContext context, params string[] sql)
        //{
        //    string sqlDelimited = sql.ToDelimitedString(" ");
        //    string res = ("sqlite3 /home/pi/Database/palantir.db \"" + sqlDelimited + "\"").Bash();
        //    await Program.SendEmbed(context.Channel, sqlDelimited, res != "" ? res : "Error.");
        //}

        [Description("Set a member flag.")]
        [Command("flag")]
        public async Task Flag(CommandContext context, [Description("The id of the member to flag")] ulong id, [Description("The new flag")] int flag = -1)
        {
            if (flag == -1)
            {
                DiscordUser target = await Program.Client.GetUserAsync(id);
                PermissionFlag getperm = new PermissionFlag(Program.Feanor.GetFlagByMember(target));
                string getDesc = "Flag[0] Bubble Farming - "
                    + getperm.BubbleFarming + "\nFlag[1] Bot Admin - "
                    + getperm.BotAdmin + "\nFlag[2] Moderator - "
                    + getperm.Moderator + "\nFlag[3] Unlimited Cloud - "
                    + getperm.CloudUnlimited + "\nFlag[4] Patron - "
                    + getperm.Patron + "\nFlag[5] Permanent Ban - "
                    + getperm.Permanban + "\nFlag[6] Drop Ban - "
                    + getperm.Dropban + "\nFlag[7] Patronizer - "
                    + getperm.Patronizer + "\nFlag[8] Booster - "
                    + getperm.Booster; ;
                await Program.SendEmbed(context.Channel, "The flags of " + target.Username, getDesc);
                return;
            }
            PermissionFlag perm = new PermissionFlag(Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin && !perm.Moderator)
            {
                await Program.SendEmbed(context.Channel, "o_o", "You can't set other's flags!");
                return;
            }

            PermissionFlag newFlag = new PermissionFlag(Convert.ToInt16(flag));
            newFlag.BotAdmin = newFlag.BotAdmin && perm.BotAdmin;
            newFlag.Patronizer = newFlag.Patronizer && perm.BotAdmin;
            newFlag.Patron = newFlag.Patron && perm.BotAdmin;

            Program.Feanor.SetFlagByID(id.ToString(), newFlag.CalculateFlag());
            string name = (await Program.Client.GetUserAsync(id)).Mention;
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

        [Description("Tests concurrency locks.")]
        [Command("concurrency")]
        [Synchronized]
        public async Task Concurrency(CommandContext context)
        {
            var a = await context.RespondAsync("hello there");
            throw new Exception("test");
            await Task.Delay(1000 * 10);
            await a.RespondAsync("general kenobi!");
        }

        //[Description("Evaluate C# code in the command context.")]
        //[Command("evaluate")]
        //[RequirePermissionFlag(PermissionFlag.ADMIN)]
        //public async Task EvaluateCs(CommandContext context, string code)
        //{
        //    if (code.StartsWith("```cs") && code.EndsWith("```")) code = code.Substring(5, code.Length - 8);

        //    // Define any necessary references and using directives
        //    var references = AppDomain.CurrentDomain.GetAssemblies();
        //    var options = ScriptOptions.Default
        //        .WithReferences(references)
        //        .WithImports("System", "System.Threading.Tasks");

        //    // Evaluate and run the code
        //    var globals = new { context };
        //    await CSharpScript.EvaluateAsync(code, options, globals);
        //}


        [Description("List servers with palantir and their stats.")]
        [Command("serverlist")]
        [RequirePermissionFlag(PermissionFlag.ADMIN)]
        public async Task Serverlist(CommandContext context, int membersBelow)
        {
            string guildlist = "";
            int count = 0;
            foreach (var guild in Program.Client.Guilds)
            {
                if (guild.Value.MemberCount >= membersBelow) continue;
                count++;
                int connectedMembers = Program.Feanor.GetGuildMembers(guild.Key.ToString()).Count();
                guildlist += "**" + guild.Value.Name + "**: " + guild.Value.MemberCount + " "
                    + (Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == guild.Key.ToString()) ? " `Palantir active | " + connectedMembers + "`" : "") + "\n";
                if (guildlist.Length > 1800)
                {
                    await context.RespondAsync(guildlist);
                    guildlist = "";
                }
            }
            guildlist += "\n Count: " + count;
            if (guildlist.Length > 0) await context.Channel.SendMessageAsync(guildlist);
        }

        [Description("Remove palantir from servers meeting certain criteria.")]
        [Command("serverpurge")]
        [RequirePermissionFlag(PermissionFlag.ADMIN)]
        public async Task PurgeServers(CommandContext context, int membersBelow, int connectedMembersBelow = 1)
        {
            int count = 0;
            string humanCriteria = "\n> - Minimum member count: " + membersBelow + "\n> - Minimum members connected to palantir: " + connectedMembersBelow;
            await context.Channel.SendMessageAsync("Starting purge with criteria:" + humanCriteria);
            foreach (var guild in Program.Client.Guilds)
            {
                if (guild.Value.MemberCount < membersBelow)
                {
                    int connectedMembers = Program.Feanor.GetGuildMembers(guild.Key.ToString()).Count();
                    if (connectedMembers < connectedMembersBelow)
                    {
                        try
                        {
                            await guild.Value.GetDefaultChannel().SendMessageAsync("Hi there!\n\nThis server does not meet one of following criteria:" + humanCriteria + "\n\nDue to a server limit, Palantir leaves all servers below that.\nYou can try inviting Palantir again or feel free to use the bot on the Typo server:\nhttps://discord.link/typo");
                        }
                        catch (Exception ex)
                        {
                            await context.Channel.SendMessageAsync("Could not send leave message: " + ex.ToString());
                        }
                        try
                        {
                            await guild.Value.LeaveAsync();
                            count++;
                        }
                        catch (Exception ex)
                        {
                            await context.Channel.SendMessageAsync("Could not leave server: " + ex.ToString());
                        }
                    }
                }
            }
            await context.Channel.SendMessageAsync("\n Purge complete, left guilds: " + count);
        }

        [Description("Start a reaction giveaway.")]
        [Command("giveaway")]
        [RequirePermissionFlag(PermissionFlag.MOD)]
        public async Task StartGiveaway(CommandContext context, ulong channelID, ulong messageID, DiscordEmoji reactionEmoji, int timeoutMilliSec, int winners, string giveawayname)
        {
            await Program.Servant.SendMessageAsync(context.Channel,
                "**Starting the " + giveawayname + "!**\n\nPeople will be eliminated once in " + (timeoutMilliSec / 1000 / 60) + " minutes, the last " + winners + " participants are the winners.");

            var msg = await(await Program.Client.GetChannelAsync(channelID)).GetMessageAsync(messageID);
            var reactions = await msg.GetReactionsAsync(reactionEmoji, 100);

            while(reactions.Count > winners)
            {
                reactions = reactions.Shuffle().ToList();
                var eliminate = reactions.First();
                reactions = reactions.Skip(1).ToList();

                var mentions = new System.Collections.Generic.List<IMention>();
                var eliminateState = new DiscordMessageBuilder()
                    .WithAllowedMentions(mentions)
                    .WithContent(eliminate.Mention + "** was eliminated :(** " + reactions.Count + " people left.");

                await Program.Servant.SendMessageAsync(context.Channel, eliminateState);

                if(reactions.Count == 10)
                {
                    var pool = new DiscordMessageBuilder()
                        .WithAllowedMentions(mentions)
                        .WithContent("\nThe remaining participant pool is: \n" + reactions.Select(r => r.Mention).ToDelimitedString(";"));
                    await Program.Servant.SendMessageAsync(context.Channel, pool);
                }

                await Task.Delay(timeoutMilliSec);
            }

            await Program.Servant.SendMessageAsync(context.Channel, "**The winners of the " + giveawayname + " are " + reactions.Select(rc => rc.Mention).ToDelimitedString(" and ") + "!**");
        }
    }
}
