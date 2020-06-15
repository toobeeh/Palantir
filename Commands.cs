using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Linq;

namespace Palantir
{
    public class Commands : BaseCommandModule
    {
        [Command("header")]
        [Description("Set the header text of the bot message.")]
        public async Task Header(CommandContext context, [Description("Header text of the message")] params string[] header)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel befor configuring the settings!");
                return;
            }
            string text = "";
            foreach (string s in header) text += s + " ";
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.Header = text;
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }

        [Command("idle")]
        [Description("Set the idle text of the bot message.")]
        public async Task Idle(CommandContext context, [Description("Idle text of the message")] params string[] idle)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel befor configuring the settings!");
                return;
            }
            string text = "";
            foreach (string s in idle) text += s + " ";
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.IdleMessage = text;
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }

        [Command("timezone")]
        [Description("Set the timezone UTC offset of the bot message.")]
        public async Task Timezone(CommandContext context, [Description("Timezone offset (eg -5)")] int offset)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel befor configuring the settings!");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.Timezone = offset;
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }

        [Command("token")]
        [Description("Set whether the token should be displayed or not.")]
        public async Task Token(CommandContext context, [Description("State (on/off)")] string state)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel befor configuring the settings!");
                return;
            }
            if (state != "on" && state != "off")
            {
                await context.Message.RespondAsync("Invalid state.");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.ShowToken = state == "on";
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }

        [Command("refreshed")]
        [Description("Set whether the refreshed time should be displayed or not.")]
        public async Task Refreshed(CommandContext context, [Description("State (on/off)")] string state)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel befor configuring the settings!");
                return;
            }
            if (state != "on" && state != "off")
            {
                await context.Message.RespondAsync("Invalid state.");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.ShowRefreshed = state == "on";
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }

        [Command("animated")]
        [Description("Set whether the animated emojis should be displayed or not.")]
        public async Task Animated(CommandContext context, [Description("State (on/off)")] string state)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel befor configuring the settings!");
                return;
            }
            if (state != "on" && state != "off")
            {
                await context.Message.RespondAsync("Invalid state.");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.ShowAnimatedEmojis = state == "on";
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }


        [Command("observe")]
        [Description("Set a channel where lobbies will be observed.")]
        public async Task Observe(CommandContext context, [Description("Target channel (eg #channel)")] string channel)
        {
            if (context.Channel.IsPrivate) { await context.Message.RespondAsync("This command is only available in server channels."); return; }
            if (context.Message.MentionedChannels.Count <1) { await context.Message.RespondAsync("Invalid channel!"); return; }

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
            await context.Message.RespondAsync("Active lobbies will now be observed in " + context.Message.MentionedChannels[0].Mention + ".\nUsers need following token to connect the browser extension: ```fix\n" + token + "\n```Pin this message or save the token!");
            // save observed
            Program.Feanor.SavePalantiri(guild);
            
        }

        [Command("observe")]
        public async Task Observe(CommandContext context, [Description("Target channel (#channel)")]string channel, [Description("Indicator to keep existing token (keep)")]  string keep)
        {
            if (context.Channel.IsPrivate) await context.Message.RespondAsync("This command is only available in server channels.");
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
            if (keep == "keep")
            {
                string oldToken = "";
                Program.Feanor.PalantirTethers.ForEach((t) => {if (t.PalantirEndpoint.GuildID == guild.GuildID) oldToken = t.PalantirEndpoint.ObserveToken;});
                if (oldToken == "") valid = false;
                else token = oldToken;

                if (valid)
                {
                    await context.Message.RespondAsync("The channel is now set to  " + context.Message.MentionedChannels[0].Mention + ".\nUsers won't need to re-enter their token." );
                    // save observed
                    Program.Feanor.SavePalantiri(guild);
                }
                else await context.Message.RespondAsync("There is no existing token.\nCheck >help for help.");
            }
            else await context.Message.RespondAsync("That's no valid command.\nCheck >help for help.");
        }

        [Description("Get your login data to connect the extension.")]
        [Command("login")]
        public async Task Login(CommandContext context)
        {
            // if DM
            if (context.Channel.IsPrivate)
            {
                DiscordDmChannel channel = (DiscordDmChannel)context.Channel;
                Member match = new Member { UserID = "0" };

                Program.Feanor.PalantirMembers.ForEach((m) =>
                {
                    if (Convert.ToUInt64(m.UserID) == context.Message.Author.Id) match = m;
                });

                if (match.UserID != "0") await channel.SendMessageAsync("Forgot your login? \nHere it is: `" + match.UserLogin + "`");
                else
                {
                    Member member = new Member();
                    member.UserID = context.Message.Author.Id.ToString();
                    member.UserName = context.Message.Author.Username;
                    member.Guilds = new List<ObservedGuild>();
                    do member.UserLogin = (new Random()).Next(99999999).ToString();
                    while (Program.Feanor.PalantirMembers.Where(mem => mem.UserLogin == member.UserLogin).ToList().Count > 0);

                    Program.Feanor.AddMember(member);

                    await channel.SendMessageAsync("Hey " + context.Message.Author.Username + "!\nYou can now login to the bowser extension and use Palantir.\nClick the extension icon in your browser, enter your login and add you discord server's token! \nYour login is: `" + member.UserLogin + "`");
                }
            }
            else await context.Message.RespondAsync("Use this command only in DM!\nRevealing private stuff is always a bad idea.");
        }
    }
}
