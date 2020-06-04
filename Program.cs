﻿using System;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using System.Linq;


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

        private static async Task OnDmCreated(DmChannelCreateEventArgs e)
        {
            await e.Channel.SendMessageAsync("hi");
        }

        private static async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            if (e.Channel.IsPrivate && e.Author != Client.CurrentUser)
            {
                DiscordDmChannel channel = (DiscordDmChannel)e.Channel;
                await channel.SendMessageAsync("hi");

                Member match = new Member{UserID = 0};
                try
                {
                    Feanor.PalantirMembers.ForEach((m) =>
                    {
                        if (m.UserID == e.Author.Id) match = m;
                    });


                    if (match.UserID > 0) await channel.SendMessageAsync("You are already a user.\nYou can login in the extension with following token: `" + match.UserLogin + "`");
                    else
                    {
                        Member member = new Member();
                        member.UserID = e.Author.Id;
                        do member.UserLogin = (new Random()).Next(99999999);
                        while (Feanor.MemberLoginExists(member.UserLogin));

                        Feanor.PalantirMembers.Add(member);

                        await channel.SendMessageAsync("Hey " + e.Author.Username + "!\nYou can now login to the bowser extension and use Palantir.\nClick the extension icon in your browser, enter your login and add you discord token! \nYour login is: `" + member.UserLogin + "` \nHave fun!");
                        Feanor.SavePalantirMember();
                    }
                }
                catch(Exception f) { Console.WriteLine(f.ToString()); }
               
                
            }


            // Is bot mentioned?
            else if(e.MentionedUsers[0] != null && e.MentionedUsers[0] == Bot)
            {
                string[] args = e.Message.Content.Split(" ");
                // Command "observe" with channel as argument?
                if(args[1] == "observe" && e.MentionedChannels.Count > 0 && e.MentionedChannels[0].Mention == args[2])
                {
                    // Create message in specified channel which later will be the static message to be continuously edited
                    DiscordMessage msg = await e.MentionedChannels[0].SendMessageAsync("Initializing...");
                    ObservedGuild guild = new ObservedGuild();
                    guild.GuildID = e.Guild.Id.ToString();
                    guild.ChannelID = e.MentionedChannels[0].Id.ToString();
                    guild.MessageID = msg.Id.ToString();
                    guild.GuildName = e.Guild.Name;

                    string token="";
                    do
                    {
                        token = (new Random()).Next(100000000 - 1).ToString("D8");
                        guild.ObserveToken = token;
                    }
                    while (Feanor.PalantirTokenExists(token));


                    string status = "";
                    if (args.Length > 3 && args[3] == "keep")
                    {
                        string oldToken = "";
                        Feanor.PalantiriTethers.ForEach((t) =>
                        {
                            if (t.PalantirEndpoint.GuildID == guild.GuildID) oldToken = t.PalantirEndpoint.ObserveToken;
                        });

                        if (oldToken == "") status = "\nThere was no existing token to keep.";
                        else token = oldToken;
                    }

                    await e.Message.RespondAsync("Active lobbies will now be observed in " + e.MentionedChannels[0].Mention + ".\nUsers need following token to connect the browser extension: ```fix\n" + token + "\n```Pin this message or save the token!" + status);

                    // save observed
                    Feanor.SavePalantiri(guild);  
                }
            }
        }
    }
}
