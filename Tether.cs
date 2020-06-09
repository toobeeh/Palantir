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

        private List<string> Emojis = (new string[]{
            "<a:l1:718816563750371358>",
            "<a:l2:718816563284803615>",
            "<a:l3:718816563217825845>",
            "<a:l4:718816562441879602>",
            "<a:l5:718816561993089056>",
            "<a:l6:718816561871192088>",
            "<a:l7:718816561116217395>",
            "<a:l8:718816560923410452>",
            "<a:l9:718816560915021884>",
            "<a:l10:718816560764157955>",
            "<a:l11:718816559421718598>",
            "<a:l12:718816559149350973>",
            "<a:l13:718817051828944926>",
            "<a:l14:718817049987776534>"
        }).ToList();


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
                Program.Feanor.RemovePalantiri(PalantirEndpoint);
                StopDataflow();
                Console.WriteLine("Removed guild " + PalantirEndpoint.GuildID);
                await guild.GetDefaultChannel().SendMessageAsync("The observed message couldn't be found. \nSet a channel using `@Palantir observe #channel`!");
            }
            catch
            {
                Program.Feanor.RemovePalantiri(PalantirEndpoint);
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
                Console.WriteLine("Exception: " + e.ToString() + "at Channel:" + PalantirEndpoint.ChannelID + ", Msg: "+PalantirEndpoint.MessageID + ",Client:" + Program.Client.CurrentUser.Username);
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
                Thread.Sleep(3000);
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
                lobby += "> **#" + l.ID + "**    " + Emojis[(new Random()).Next(Emojis.Count-1)] + "     " + l.Host + "   **|**   Language: " + l.Language + "   **|**   Round " + l.Round + "   **|**   " + (l.Private ? "Private " + "\n> <" + l.Link + ">" : "Public")  + "\n> " + l.Players.Count  + " Players \n";

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
                        players += (player.Drawing ? " 🖍, " : ", ");
                    }
                }
                if(players.Length > 0)players = players[0..^2];
                players += "";
                sender += "```";

                if (sender.Split("\n").Length > 2) lobby += sender;
                if (players.Length > 0) lobby += players;

                lobby += "\n\n\n";
                
                message += lobby;

                //Set lobby id to index (for displaying) and unique id (for searching)
                l.ID = l. ID + ":" + lobbyUniqueID;
            });

            
            

            string searching = "";
            foreach (PlayerStatus p in OnlinePlayers.Where(o => o.Status == "searching" && !GuildLobbies.Any(l => l.Players.Any(p => p.ID == o.PlayerMember.UserID)))){
                searching += p.PlayerMember.UserName + ", ";
            }

            if (searching.Length > 0) message += "<a:onmyway:718807079305084939>   " + searching[0..^2];

            if (GuildLobbies.Count == 0 && searching.Length == 0) message += "\n<a:alone:718807079434846238>\nSeems like no-one is playing :( \nAsk some friends to join or go solo!\n\n ";
            //Console.WriteLine(message);
            string guildLobbysStatus = JsonConvert.SerializeObject(GuildLobbies);
            File.WriteAllText(directory + "statusGuild" + PalantirEndpoint.GuildID + ".json", guildLobbysStatus);

            return message;
        }
    }
}
