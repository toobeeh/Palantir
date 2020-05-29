using System;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Palantir
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing Palanir...");
            //List<Lobby> lobbies = JsonConvert.DeserializeObject<List<Lobby>>(File.ReadAllText(@"C:\Users\Tobi\source\repos\toobeeh\Palantir\lobbies.json"));

            Feanor.LoadPalantiri();

            var discordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = "NzE1ODc0Mzk3MDI1NDY4NDE3.XtDksg.vbCY4jq50WGZthP2aZrIBIqzS7Q",
                TokenType = TokenType.Bot
            });
            discordClient.MessageCreated += OnMessageCreated;
            await discordClient.ConnectAsync();
            Bot = discordClient.CurrentUser;

            Console.WriteLine("Palantir connected. Fool of a Took!");

            Console.WriteLine("Stored guilds:");
            Feanor.Palantiri.ForEach((p) => { Console.WriteLine("- " + p.GuildID); });

            await Task.Delay(-1);
        }

        private static DiscordUser Bot;

        private static async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            // Is bot mentioned?
            if(e.MentionedUsers[0] != null && e.MentionedUsers[0] == Bot)
            {
                string[] args = e.Message.Content.Split(" ");
                // Command "observe" with channel as argument?
                if(args[1] == "observe" && e.MentionedChannels.Count > 0 && e.MentionedChannels[0].Mention == args[2])
                {
                    // Create message in specified channel which later will be the static message to be continuously edited
                    DiscordMessage msg = await e.MentionedChannels[0].SendMessageAsync("Initializing...");
                    ObservedGuild guild = new ObservedGuild();
                    guild.GuildID = e.Guild.Id;
                    guild.ChannelID = e.MentionedChannels[0].Id;
                    guild.MessageID = msg.Id;

                    // save observed
                    Feanor.SavePalantiri(guild);  
                }
            }
        }
    }
}
