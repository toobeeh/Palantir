using System.Diagnostics;
using System.Text.RegularExpressions;
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
using System;
using System.Collections.Generic;
using Palantir.Model;

namespace Palantir.Commands
{
    public class SetupCommands : BaseCommandModule
    {
        [Command("observe")]
        [Description("Set a channel where lobbies will be observed.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Observe(CommandContext context, [Description("Target channel (eg #channel)")] string channel)
        {
            if (context.Message.MentionedChannels.Count < 1) { await context.Message.RespondAsync("Invalid channel!"); return; }

            // Create message in specified channel which later will be the static message to be continuously edited
            DiscordMessage msg = await context.Message.MentionedChannels[0].SendMessageAsync("Initializing...");
            ObservedGuild guild = new ObservedGuild();
            guild.GuildID = context.Guild.Id.ToString();
            guild.ChannelID = context.Message.MentionedChannels[0].Id.ToString();
            guild.MessageID = msg.Id.ToString();
            guild.GuildName = context.Guild.Name;

            string token;
            do
            {
                token = (new Random()).Next(100000000 - 1).ToString("D8");
                guild.ObserveToken = token;
            }
            while (Program.Feanor.PalantirTokenExists(token));
            await context.Message.RespondAsync("Active lobbies will now be observed in " + context.Message.MentionedChannels[0].Mention + ".\nUsers need following token to connect the browser extension: ```fix\n" + token + "\n```Pin this message or save the token!\n\nFor further instructions, users can visit the website https://typo.rip.\nMaybe include the link in the bot message!");
            // save observed
            Program.Feanor.SavePalantiri(guild);
        }

        [Command("switch")]
        [Description("Set a channel where lobbies will be observed.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Switch(CommandContext context, [Description("Target channel (#channel)")] string channel)
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());
            if (context.Message.MentionedChannels.Count < 1) { await context.Message.RespondAsync("Invalid channel!"); return; }

            // Create message in specified channel which later will be the static message to be continuously edited
            DiscordMessage msg = await context.Message.MentionedChannels[0].SendMessageAsync("Initializing...");
            ObservedGuild guild = new ObservedGuild();
            guild.GuildID = context.Guild.Id.ToString();
            guild.ChannelID = context.Message.MentionedChannels[0].Id.ToString();
            guild.MessageID = msg.Id.ToString();
            guild.GuildName = context.Guild.Name;

            string token = "";
            do
            {
                token = (new Random()).Next(100000000 - 1).ToString("D8");
                guild.ObserveToken = token;
            }
            while (Program.Feanor.PalantirTokenExists(token));

            bool valid = true;
            string oldToken = "";

            Program.Feanor.PalantirTethers.ForEach((t) => { if (t.PalantirEndpoint.GuildID == guild.GuildID) oldToken = t.PalantirEndpoint.ObserveToken; });
            if (oldToken == "") valid = false;
            else
            {
                token = oldToken;
                guild.ObserveToken = token;
            }

            if (valid)
            {
                await context.Message.RespondAsync("The channel is now set to  " + context.Message.MentionedChannels[0].Mention + ".\nUsers won't need to re-enter their token.");
                // save observed
                Program.Feanor.SavePalantiri(guild);
            }
            else await context.Message.RespondAsync("There is no existing token.\nCheck >help for help.");
        }

        [Command("animated")]
        [Description("Set whether the animated emojis should be displayed or not.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Animated(CommandContext context, [Description("State (on/off)")] string state)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            if (state != "on" && state != "off")
            {
                await context.Message.RespondAsync("Invalid state.");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.ShowAnimatedEmojis = state == "on";
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
            await context.RespondAsync("Updated animated emoji setting.");
        }

        [Command("header")]
        [Description("Set the header text of the bot message.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Header(CommandContext context, [Description("Header text of the message")] params string[] header)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            string text = "";
            foreach (string s in header) text += s + " ";
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.Header = text;
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
            await context.RespondAsync("Updated header setting.");
        }

        [Command("idle")]
        [Description("Set the idle text of the bot message.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Idle(CommandContext context, [Description("Idle text of the message")] params string[] idle)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            string text = "";
            foreach (string s in idle) text += s + " ";
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.IdleMessage = text;
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
            await context.RespondAsync("Updated idle setting.");
        }

        [Command("addwebhook")]
        [Description("Add a new webhook")]
        [RequireUserPermissions(DSharpPlus.Permissions.ManageWebhooks)]
        [RequireGuild()]
        public async Task AddWebhook(CommandContext context, [Description("Name of the webhook")] string name, [Description("URL of the webhook")] string url)
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());

            PalantirContext db = new PalantirContext();
            db.Webhooks.Add(new Model.Webhook
            {
                Name = name,
                WebhookUrl = url,
                ServerId = context.Guild.Id.ToString()
            });
            db.SaveChanges();
            db.Dispose();

            await context.RespondAsync("Webhook added. See all webhooks with >webhooks");
        }

        [Command("webhooks")]
        [Description("Show all webhooks for this server - warning, the webhook url is sensitive data and can be abused!")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Webhooks(CommandContext context, [Description("True if all webhooks should be removed.")] bool clearAll = false)
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());

            PalantirContext db = new PalantirContext();
            string hooks = "";
            db.Webhooks.Where(w => w.ServerId == context.Guild.Id.ToString()).ForEach(h =>
            {
                hooks += "- " + h.Name + ": " + h.WebhookUrl + "\n";
            });
            await context.RespondAsync(hooks);

            if (clearAll)
            {
                db.Webhooks.RemoveRange(db.Webhooks.Where(w => w.ServerId == context.Guild.Id.ToString()));
                await context.RespondAsync("Those webhooks were removed.");
            }
            db.SaveChanges();
            db.Dispose();
        }

        [Command("timezone")]
        [Description("Set the timezone UTC offset of the bot message.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Timezone(CommandContext context, [Description("Timezone offset (eg -5)")] int offset)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.Timezone = offset;
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
            await context.RespondAsync("Updated timezone setting.");
        }

        [Command("token")]
        [Description("Set whether the token should be displayed or not.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Token(CommandContext context, [Description("State (on/off)")] string state)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            if (state != "on" && state != "off")
            {
                await context.Message.RespondAsync("Invalid state.");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.ShowToken = state == "on";
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
            await context.RespondAsync("Updated token visibility setting.");
        }

        [Command("refreshed")]
        [Description("Set whether the refreshed time should be displayed or not.")]
        [RequireGuild()]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        public async Task Refreshed(CommandContext context, [Description("State (on/off)")] string state)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            if (state != "on" && state != "off")
            {
                await context.Message.RespondAsync("Invalid state.");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.ShowRefreshed = state == "on";
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
            await context.RespondAsync("Updated refreshed visibility setting.");
        }
    }
}
