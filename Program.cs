using System;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace Palantir
{
    class Program
    {
        public static DataManager Feanor;
        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing Palantir...");

            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = File.ReadAllText("/home/pi/palantirToken.txt"),
                TokenType = TokenType.Bot
            });
            Commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { ">" },
                DmHelp = false,
                IgnoreExtraArguments = true
            });
            Client.GuildCreated += onjoin;
            Commands.RegisterCommands<Commands>();
            await Client.ConnectAsync();
            Feanor = new DataManager();

            Console.WriteLine("Palantir ready. Do not uncover it.");
            Console.WriteLine("Stored guilds:");
            Feanor.PalantirTethers.ForEach((t) => { Console.WriteLine("- " + t.PalantirEndpoint.GuildID + " / " + t.PalantirEndpoint.GuildName); });
            Console.WriteLine("Stored members:");
            Feanor.PalantirMembers.ForEach((m) => { Console.WriteLine("- " + m.UserName); });
            Feanor.ActivatePalantiri();

            Console.WriteLine("Palantir activated. Fool of a Took!");

            await Task.Delay(-1);
        }

        private static async Task onjoin(GuildCreateEventArgs e)
        {
            try
            {
                await e.Guild.SystemChannel.SendMessageAsync("Hello there! <a:l33:721872925531308032>\nMy prefix is `>`.\nCheck out `>manual` or `>help`.\nhttps://gph.is/2s4rv0N");
            }
            catch
            {
                foreach(KeyValuePair<ulong, DiscordChannel> p in e.Guild.Channels)
                {
                    try {
                        await e.Guild.GetChannel(p.Key).SendMessageAsync("Hello there! <a:l33:721872925531308032>\nMy prefix is `>`.\nCheck out `>manual` or `>help`.\nhttps://gph.is/2s4rv0N");
                        return;
                    }
                    catch { }
                }
            }
        }

    }
}
