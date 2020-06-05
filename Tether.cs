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
using System.Linq;

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
                DiscordGuild guild = await Program.Client.GetGuildAsync(Convert.ToUInt64(PalantirEndpoint.GuildID));
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
                TargetChannel = await Program.Client.GetChannelAsync(Convert.ToUInt64(PalantirEndpoint.ChannelID));
                TargetMessage = await TargetChannel.GetMessageAsync(Convert.ToUInt64(PalantirEndpoint.MessageID));
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString() + "at Channel:" + PalantirEndpoint.ChannelID + ", Msg: "+PalantirEndpoint.MessageID + ",Client:" + Program.Client.ToString());
                //RemoveTether();
                return;
            }

            int notFound = 0;

            while (!abort)
            {
                try
                {
                    TargetMessage = await TargetMessage.ModifyAsync(BuildLobbyContent());
                    notFound = 0;
                }
                catch(Exception e) { 
                    notFound++;
                    if(notFound > maxErrorCount)
                    {
                        Console.WriteLine("Target Message couldnt be edited. Error: " + e.ToString());
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
            List<string> reports =new List<string>(Directory.GetFiles(directory, "*reportID*"));
            List<Lobby> Lobbies = new List<Lobby>();
            List<string> playerstatus = new List<string>(Directory.GetFiles(directory + "OnlinePlayers/", "statusMember*"));
            List<PlayerStatus> OnlinePlayers = new List<PlayerStatus>();

            reports.ForEach((r) =>
            {
                if (File.GetCreationTime(r) < DateTime.Now.AddSeconds(-7)) File.Delete(r);
                else
                {
                    try
                    {
                        Console.WriteLine("Found report: " + r);
                        Lobbies.Add(JsonConvert.DeserializeObject<Lobby>(File.ReadAllText(r)));
                    }
                    catch (Exception e) { Console.WriteLine("Couldnt read lobby file: " + e); };
                }
            });

            playerstatus.ForEach((p) =>
            {
                if (File.GetCreationTime(p) < DateTime.Now.AddSeconds(-7)) File.Delete(p);
                else
                {
                    try
                    {
                        Console.WriteLine("Found status: " + p);
                        OnlinePlayers.Add(JsonConvert.DeserializeObject<PlayerStatus>(File.ReadAllText(p)));
                    }
                    catch (Exception e) { Console.WriteLine("Couldnt read status file: " + e); };
                }
            });

            List<Lobby> GuildLobbies = new List<Lobby>();
            Lobbies.ForEach((l) =>
            {
                if (l.GuildID.ToString() == PalantirEndpoint.GuildID && l.ObserveToken == PalantirEndpoint.ObserveToken)
                {
                    GuildLobbies.Add(l);
                    Console.WriteLine("Lobby for guild " + PalantirEndpoint.GuildName + " was found: " + l.ID);
                }
            });

            message += "\n\n";
            message += "```fix\n";
            message += "Currently playing skribbl.io or sketchful.io";
            message += "```";
            message += "Refreshed: " + DateTime.Now.ToShortTimeString() + " (GMT)\nServer token: `"+ PalantirEndpoint.ObserveToken + "`\n\n\n";
            

            GuildLobbies.ForEach((l) =>
            {
                string lobby = "";
                string lobbyUniqueID = l.ID;

                // set id to index
                l.ID = Convert.ToString(GuildLobbies.IndexOf(l)+1);
                lobby += "> **#" + l.ID + "**    :crystal_ball:     " + l.Host + "   **|**   Language: " + l.Language + "   **|**   Round " + l.Round + "   **|**   " + (l.Private ? "Private " + "\n> <" + l.Link + ">" : "Public")  + "\n> \n";

                string players = "";
                string sender = "```fix\n";
                foreach(Player player in l.Players)
                {
                    PlayerStatus match = OnlinePlayers.FirstOrDefault(p => p.Status == "playing" && p.LobbyID == lobbyUniqueID && p.LobbyPlayerID == player.LobbyPlayerID && p.PlayerMember.Guilds.Count(g => g.GuildID == l.GuildID) > 0);
                    if(match != null)
                    {
                        player.Sender = true;
                        player.ID = match.PlayerMember.UserID;
                    }

                    if (player.Sender)
                    {

                        sender += player.Name;
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
                players += "";
                sender += "```";

                if (sender.Split("\n").Length > 2) lobby += sender;
                if (players.Length > 0) lobby += players;

                lobby += "\n\n\n";
                
                message += lobby;
            });

            string searching = "";
            foreach (PlayerStatus p in OnlinePlayers.Where(o => !GuildLobbies.Any(l => l.Players.Any(p => p.ID != o.PlayerMember.UserID)))){
                searching += p.PlayerMember.UserName + ", ";
            }

            if (searching.Length > 0) message += ":mag:  " + searching;

            if (GuildLobbies.Count == 0) message += "\nAtm, noone is playing :( \nAsk some friends to join or go solo!\n\n ";

            string guildLobbysStatus = JsonConvert.SerializeObject(GuildLobbies);
            File.WriteAllText(directory + "statusGuild" + PalantirEndpoint.GuildID + ".json", guildLobbysStatus);

            return message;
        }

    }
}
