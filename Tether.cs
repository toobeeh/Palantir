using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace Palantir
{
    public class Tether
    {
        public ObservedGuild PalantirEndpoint { get; private set; }
        private Thread Dataflow;
        private bool abort;
        private DiscordMessage TargetMessage;
        private DiscordChannel TargetChannel;
        private const int maxErrorCount = 5;
        //private const string directory = @"C:\Users\Tobi\source\repos\toobeeh\Palantir\";
        private const string directory = @"/home/pi/JsonShared/";


        public Tether(ObservedGuild guild)
        {
            abort = false;
            PalantirEndpoint = guild;
            Dataflow = new Thread(new ThreadStart(ObserveLobbies));
            Dataflow.Name = "Dataflow GuildID " + guild.GuildID;
        }

        public void SetNewPalantirEndpoint(ObservedGuild guild)
        {
            abort = false;
            PalantirEndpoint = guild;
            Dataflow = new Thread(new ThreadStart(ObserveLobbies));
            Dataflow.Name = "Dataflow GuildID " + guild.GuildID;
        }

        public void EstablishDataflow()
        {
            Dataflow.Start();
        }

        public void StopDataflow()
        {
            abort = true;
        }

        private async void RemoveTether()
        {
            try
            {
                DiscordGuild guild = await Program.Client.GetGuildAsync(PalantirEndpoint.GuildID);
                Feanor.RemovePalantiri(PalantirEndpoint);
                StopDataflow();
                Console.WriteLine("Removed guild " + PalantirEndpoint.GuildID);
                await guild.GetDefaultChannel().SendMessageAsync("The observed message couldn't be found. \nSet a channel using `@Palantir observe #channel`!");
            }
            catch
            {
                Feanor.RemovePalantiri(PalantirEndpoint);
                StopDataflow();
                Console.WriteLine("Removed guild " + PalantirEndpoint.GuildID);
            }
        }

        private async void ObserveLobbies()
        {
            try
            {
                TargetChannel = await Program.Client.GetChannelAsync(PalantirEndpoint.ChannelID);
                TargetMessage = await TargetChannel.GetMessageAsync(PalantirEndpoint.MessageID);
            }
            catch
            {
                RemoveTether();
                return;
            }

            int notFound = 0;

            while (!abort)
            {
                TargetMessage = await TargetMessage.ModifyAsync(BuildLobbyContent());
                try
                {
                    
                    notFound = 0;
                }
                catch { 
                    notFound++;
                    if(notFound > maxErrorCount)
                    {
                        RemoveTether();
                        return;
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private string BuildLobbyContent()
        {
            string message = "";
            List<string> reports =new List<string>(Directory.GetFiles(directory, "*report*"));
            List<Lobby> Lobbies = new List<Lobby>();

            reports.ForEach((r) =>
            {
                Lobbies.Add(JsonConvert.DeserializeObject<Lobby>(File.ReadAllText(r)));
            });

            List<Lobby> GuildLobbies = new List<Lobby>();
            Lobbies.ForEach((l) =>
            {
                if (l.ServerID == PalantirEndpoint.GuildID && l.ObserveToken == PalantirEndpoint.ObserveToken) GuildLobbies.Add(l);
            });

            message += "\n\n";
            message += "```ini\n";
            message += "[    Currently playing skribbl.io or sketchful.io    ]";
            message += "```";
            message += "Refreshed: " + DateTime.Now.ToShortTimeString() + " (GMT)\nServer token: `"+ PalantirEndpoint.ObserveToken + "`\n\n\n";
            

            GuildLobbies.ForEach((l) =>
            {
                string lobby = "";


                lobby += "> **#" + l.ID + "**    :crystal_ball:     " + l.Host + "   **|**   Round " + l.Round + "   **|**   " + (l.Private ? "Private `" + l.Link + "`" : "Public")  + "\n> \n";

                string players = "`";
                string sender = "```ini\n";
                foreach(Player player in l.Players)
                {
                    if (player.Sender)
                    {
                        sender += "[" + player.Name + "]";
                        for (int i = player.Name.Length; i < 15; i++) sender += " ";
                        sender += player.Score + " pts";
                        sender += player.Drawing ? " 🖍 \n" : "\n";
                    }
                    else 
                    {
                        players += player.Name ;
                        players += (player.Drawing ? " 🖍 " : "") + (l.Players.IndexOf(player) < l.Players.Count - 1 ? ", " : "");
                    }
                }
                players += "`";
                sender += "```";

                if (sender.Split("\n").Length > 2) lobby += sender;
                if (players.Length > 0) lobby += players;

                lobby += "\n\n";
                message += lobby;
            });

            if (GuildLobbies.Count == 0) message += "\nATM, noone is drawing :( \nAsk some friends to join or go solo!\n\n ";

            return message;
        }

    }
}
