using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Palantir
{
    public class Commands : BaseCommandModule
    {
        [Command("observe")]
        public async Task Observe(CommandContext context, string channel, string keep = "")
        {
           
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


            string status = "";
            if (keep == "keep")
            {
                string oldToken = "";
                Program.Feanor.PalantirTethers.ForEach((t) =>
                {
                    if (t.PalantirEndpoint.GuildID == guild.GuildID) oldToken = t.PalantirEndpoint.ObserveToken;
                });

                if (oldToken == "") status = "\nThere was no existing token to keep.";
                else token = oldToken;
            }

            await context.Message.RespondAsync("Active lobbies will now be observed in " + context.Message.MentionedChannels[0].Mention + ".\nUsers need following token to connect the browser extension: ```fix\n" + token + "\n```Pin this message or save the token!" + status);

            // save observed
            Program.Feanor.SavePalantiri(guild);
            
        }



    }
}
