using System;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System.Threading.Tasks;


namespace Palantir
{
    class Program
    {
        private static DiscordUser Bot;
        public static DiscordClient Client { get; private set; }
        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing Palantir...");
            //List<Lobby> lobbies = JsonConvert.DeserializeObject<List<Lobby>>(File.ReadAllText(@"C:\Users\Tobi\source\repos\toobeeh\Palantir\lobbies.json"));

            Feanor.LoadPalantiri();
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = "NzE1ODc0Mzk3MDI1NDY4NDE3.XtDksg.vbCY4jq50WGZthP2aZrIBIqzS7Q",
                TokenType = TokenType.Bot
            });
            Client.MessageCreated += OnMessageCreated;
            await Client.ConnectAsync();
            Bot = Client.CurrentUser;

            Console.WriteLine("Palantir ready. Do not uncover it.");
            Console.WriteLine("Stored guilds:");
            Feanor.PalantiriTethers.ForEach((t) => { Console.WriteLine("- " + t.PalantirEndpoint.GuildID); });

            Feanor.ActivatePalantiri();
            Console.WriteLine("Palantir activated. Fool of a Took!");

            await Task.Delay(-1);
        }
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
                    guild.ServerName = e.Guild.Name;

                    string token="";
                    do
                    {
                        token = (new Random()).Next(100000000 - 1).ToString("D8");
                        guild.OberserveToken = token;
                    }
                    while (Feanor.PalantirTokenExists(token));
                    

                    await e.Message.RespondAsync("Active lobbies will now be observed in " + e.MentionedChannels[0].Mention + ".\nUsers need following token to connect the browser extension: ```fix\n" + token + "\n``` Pin this message or save the token!");

                    // save observed
                    Feanor.SavePalantiri(guild);  
                }
            }
        }
    }
}
