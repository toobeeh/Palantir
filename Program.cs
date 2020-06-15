using System;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using System.Threading.Tasks;
using System.Linq;


namespace Palantir
{
    class Program
    {
        private static DiscordUser Bot;
        public static DataManager Feanor;
        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing Palantir...");

            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = "NzE1ODc0Mzk3MDI1NDY4NDE3.XtDksg.vbCY4jq50WGZthP2aZrIBIqzS7Q",
                TokenType = TokenType.Bot
            });
            Commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { ">" },
                DmHelp = false
            });
            Commands.RegisterCommands<Commands>();
            await Client.ConnectAsync();
            Bot = Client.CurrentUser;
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

    }
}
