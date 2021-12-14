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
using System.Globalization;
using System.Diagnostics;

namespace Palantir
{
    public class Tether
    {
        public ObservedGuild PalantirEndpoint { get; private set; }
        public GuildSettings PalantirSettings { get; private set; }
        private Thread Dataflow;
        private bool abort;
        private DiscordMessage TargetMessage;
        private DiscordChannel TargetChannel;
        private const int maxErrorCount = 5;
        private List<string> Emojis = (new string[]{
            "<a:l9:718816560915021884>",
            "<a:l8:718816560923410452>",
            "<a:l7:718816561116217395>",
            "<a:l6:718816561871192088>",
            "<a:l5:718816561993089056>",
            "<a:l4:718816562441879602>",
            "<a:l36:721872926411980820>",
            "<a:l35:721872926189551657>",
            "<a:l34:721872925661069312>",
            "<a:l33:721872925531308032>",
            "<a:l32:721872924570550352>",
            "<a:l31:721872924465954928>",
            "<a:l30:721872923182366730>",
            "<a:l3:718816563217825845>",
            "<a:l29:721872922452688916>",
            "<a:l28:721872921995378738>",
            "<a:l27:721872921844252705>",
            "<a:l26:721872921777274908>",
            "<a:l25:721872921152192513>",
            "<a:l24:721872920866979881>",
            "<a:l23:721872920347017216>",
            "<a:l22:721872920129044510>",
            "<a:l21:721872919990501468>",
            "<a:l20:721872919973855233>",
            "<a:l2:718816563284803615>",
            "<a:l1:718816563750371358>",
            "<a:l19:721872919919067247>",
            "<a:l18:721872918921084948>",
            "<a:l17:721872918480421014>",
            "<a:l16:721872918304522280>",
            "<a:l15:721872916257439745>",
            "<a:l14:718817049987776534>",
            "<a:l13:718817051828944926>",
            "<a:l12:718816559149350973>"
        }).ToList();

        public Tether(ObservedGuild guild)
        {
            abort = false;
            PalantirEndpoint = guild;
            PalantirSettings = new GuildSettings
            {
                Header = "```fix\nCurrently playing skribbl.io```", 
                IdleMessage = "Seems like no-one is playing :( \nAsk some friends to join or go solo!\n\n ", 
                Timezone = 0, 
                ShowAnimatedEmojis = true, 
                ShowRefreshed = false, 
                ShowToken = true, 
                WaitingMessages = { }
            };
            Dataflow = new Thread(new ThreadStart(ObserveLobbies));
            Dataflow.Name = "Dataflow GuildID " + guild.GuildID;
        }

        public Tether(ObservedGuild guild, GuildSettings settings)
        {
            abort = false;
            PalantirEndpoint = guild;
            PalantirSettings = settings;
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
            Console.WriteLine("Stopped Dataflow for " + PalantirEndpoint.GuildName);
            abort = true;
        }

        private async void RemoveTether()
        {
            Console.WriteLine("remove " + PalantirEndpoint.GuildName);
            try
            {
                await (await Program.Client.GetChannelAsync(Convert.ToUInt64(PalantirEndpoint.ChannelID))).SendMessageAsync("The observed message couldn't be found. \nSet a channel using `>observe #channel`!");
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Error sending status:" + e);
            }
            
            try
            {
                Program.Feanor.RemovePalantiri(PalantirEndpoint);
                StopDataflow();
                Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Removed guild " + PalantirEndpoint.GuildID);
            }
            catch
            {
                Program.Feanor.RemovePalantiri(PalantirEndpoint);
                StopDataflow();
                Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Removed guild " + PalantirEndpoint.GuildID + PalantirEndpoint.GuildName);
            }
        }

        private async void ObserveLobbies()
        {
            try
            {
                TargetChannel = await Program.Client.GetChannelAsync(Convert.ToUInt64(PalantirEndpoint.ChannelID));
                TargetMessage = await TargetChannel.GetMessageAsync(Convert.ToUInt64(PalantirEndpoint.MessageID));
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException e)
            {
                Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Exception: " + e.ToString() + "at Channel:" + PalantirEndpoint.ChannelID + ", Msg: " + PalantirEndpoint.MessageID + ", Guild:" + PalantirEndpoint.GuildName);
                RemoveTether();
            }
            catch(Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Exception: " + e.ToString() + "at Channel:" + PalantirEndpoint.ChannelID + ", Msg: "+PalantirEndpoint.MessageID + ", Guild:" + PalantirEndpoint.GuildName);
                return;
            }

            int notFound = 0;
            string lastContent = "";
            while (!abort)
            {
                try
                {
                    // try to build lobby message
                    string content = BuildLobbyContent();
                    if (content == lastContent)
                    {
                        await TargetChannel.TriggerTypingAsync();
                        Thread.Sleep(4000);
                        continue;
                    }
                    else lastContent = content;
                    DiscordMessage split;
                    try
                    {
                        do split = (await TargetChannel.GetMessagesAfterAsync(TargetMessage.Id, 1)).First();
                        while (split.Author.Id != Program.Client.CurrentUser.Id);
                    }
                    catch
                    {
                        // no split message found -> not required yet
                        split = null;
                    }
                    if (content.Length <= 1900) {
                        TargetMessage = await TargetMessage.ModifyAsync(content.Replace(" ", ""));
                        if(!(split is null)) await split.ModifyAsync("_ _");
                    }
                    else
                    {
                        int lastLobbyBreak = content.Length > 1900 ? 1900 : content.Length;
                        while (content[lastLobbyBreak] != ' ' || lastLobbyBreak < 1000) lastLobbyBreak--;
                        TargetMessage = await TargetMessage.ModifyAsync(content.Substring(0, lastLobbyBreak - 1).Replace(" ", ""));
                        if(split is null) split = await TargetChannel.SendMessageAsync("_ _"); 
                        split = await split.ModifyAsync(content.Substring(lastLobbyBreak + 1, content.Length - lastLobbyBreak - 1).Replace(" ", ""));
                    }
                    await TargetChannel.TriggerTypingAsync();
                    notFound = 0;
                }
                catch (Microsoft.Data.Sqlite.SqliteException e) // catch sql exceptions
                {
                    if(e.SqliteErrorCode == 8)
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Locked DB. Skipped writing lobby data for this cycle.");
                    else
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > DB Error: " + e.SqliteErrorCode + ". Skipped writing lobby data for this cycle.\n" + e.ToString());
                }
                catch(DSharpPlus.Exceptions.NotFoundException e) // catch Discord api axceptions
                {
                    notFound++;
                    if (notFound > maxErrorCount)
                    {
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") 
                            + " > Target Message couldnt be edited. Not found incremented to " + notFound + " / " + maxErrorCount
                            + " Error: " + e.ToString());
                        RemoveTether();
                        return;
                    }
                }
                catch (DSharpPlus.Exceptions.UnauthorizedException e) // catch Discord api axceptions
                {
                    notFound++;
                    if (notFound > maxErrorCount)
                    {
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                            + " > Target Message couldnt be edited. Not found incremented to " + notFound + " / " + maxErrorCount
                            + " Error: " + e.ToString());
                        RemoveTether();
                        return;
                    }
                }
                catch (Exception e) // catch other exceptions
                {
                    int line = new StackTrace(e, true).GetFrame(0).GetFileLineNumber();
                    Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Unhandled exception on line " + line + " - Target message couldnt be edited. No removal of tether, just 15s timeout. Error: " + e.ToString());
                    Thread.Sleep(15000);
                }
                Thread.Sleep(4000);
            }
        }

        private string BuildLobbyContent()
        {
            //return "```Discord lobbies are currently under maintenance. \nThis can take up to 48 hours.\nSorry & see ya!```";
            string message = "";
            PalantirDbContext database = new PalantirDbContext();

            List<Lobby> Lobbies = new List<Lobby>();
            List<ReportEntity> reports = database.Reports.Distinct().Where(r=>r.ObserveToken == PalantirEndpoint.ObserveToken).ToList();

            List<PlayerStatus> OnlinePlayers = new List<PlayerStatus>();
            List<StatusEntity> playerstatus = database.Status.Distinct().ToList();

            reports.ForEach((r) =>
            {
                if (DateTime.ParseExact(r.Date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) > DateTime.UtcNow.AddSeconds(-8)) 
                {
                    try
                    {
                        //Console.WriteLine("Found report: " + r.LobbyID);
                        Lobbies.Add(JsonConvert.DeserializeObject<Lobby>(r.Report));
                    }
                    catch (Exception e) { Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Couldnt read lobby entry: " + e); };
                }
            });

            playerstatus.ForEach((p) =>
            {
                if ( DateTime.ParseExact(p.Date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) > DateTime.UtcNow.AddSeconds(-8))
                {
                    try
                    {
                        OnlinePlayers.Add(JsonConvert.DeserializeObject<PlayerStatus>(p.Status));
                    }
                    catch (Exception e) { Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Couldnt read status file: " + e); };
                }
            });

            List<Lobby> guildLobbies = new List<Lobby>();
            Lobbies.ForEach((l) =>
            {
                if (l.GuildID.ToString() == PalantirEndpoint.GuildID && l.ObserveToken == PalantirEndpoint.ObserveToken)
                {
                    guildLobbies.Add(l);
                }
            });

            List<EventEntity> events = Events.GetEvents(true);
            if (events.Count <= 0) message += "```arm\nNo event active :( Check upcoming events with '>upcoming'.\n‎As soon as an event is live, you can see details using '>event'.```\n\n";
            else message += "```arm\n" + events[0].EventName + ": \n"+ events[0].Description + "\nView details with '>event'!```\n";

            message += PalantirSettings.Header + "\n";
            if (PalantirSettings.ShowRefreshed) message += "Refreshed: <t:" + (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 20) + ":R> \n";//+ DateTime.UtcNow.AddHours(PalantirSettings.Timezone).ToShortTimeString() + " (UTC " + PalantirSettings.Timezone.ToString("+0;-#") + ")\n"; 
            if(PalantirSettings.ShowToken) message += "Click to connect: <https://typo.rip/i?invite="+ PalantirEndpoint.ObserveToken + ">\n";

            message += "\n";
            
            guildLobbies.ForEach((l) =>
            {
                string lobby = "";
                string lobbyUniqueID = l.ID;

                List<short> scores = new List<short>();
                foreach(Player p in l.Players) { if (!scores.Contains(p.Score)) scores.Add(p.Score); }
                scores.Sort((a, b) => b.CompareTo(a));

                // get description if private
                string lobbyDescription = "";
                if (l.Private)
                {
                    string d;
                    string json = "";
                    try
                    {
                        json = database.Lobbies.FirstOrDefault(lobbyEntity => lobbyEntity.LobbyID == l.ID).Lobby;
                        ProvidedLobby lobbyraw = JsonConvert.DeserializeObject<ProvidedLobby>(json);
                        d = lobbyraw.Description;
                        if (d.Length > 100) d = d.Substring(0, 100);
                        if (d.Contains("#nojoin")) { l.Link = "Closed Private Game"; l.Key = PalantirEndpoint.GuildName + l.Key[^5..]; }
                        if (lobbyraw.Restriction == "restricted") { l.Link = "Restricted Private Game"; l.Key = PalantirEndpoint.GuildName + l.Key[^5..]; }
                        else if (lobbyraw.Restriction != "unrestricted" && PalantirEndpoint.GuildID != lobbyraw.Restriction) { l.Link = "Server-Restricted Private Game"; l.Key = PalantirEndpoint.GuildName + l.Key[^5..]; }
                    }
                    catch { 
                        d = "";
                    };

                    if (d != "") lobbyDescription = "> `" + DSharpPlus.Formatter.Sanitize(d).Replace("`","").Replace("\n","") + "`\n";
                }
                string key = l.Key;
                // set id to index
                l.ID = Convert.ToString(guildLobbies.IndexOf(l) + 1);
                lobby += "> **#" + l.ID + "**    " + (PalantirSettings.ShowAnimatedEmojis ? Emojis[(new Random()).Next(Emojis.Count-1)] : "") + "     " + l.Host + "   **|**  " + l.Language + "   **|**   Round " + l.Round + "   **|**   " + (l.Host == "skribbl.io" ? (l.Private ? "Private " + "\n> <" + l.Link + ">" : "Public") : "<" + l.Link + ">")  + "\n> " + l.Players.Count  + " Players \n";
                
                if (lobbyDescription != "") lobby += lobbyDescription.Replace("\\", "");

                string players = "";
                string sender = "```fix\n";
                foreach(Player player in l.Players)
                {
                    string login = "";
                    PlayerStatus match = OnlinePlayers.FirstOrDefault(p => p.Status == "playing" && p.LobbyID == lobbyUniqueID && p.LobbyPlayerID == player.LobbyPlayerID && p.PlayerMember.Guilds.Count(g => g.GuildID == l.GuildID) > 0);
                    if(match != null)
                    {
                        player.Sender = true;
                        player.ID = match.PlayerMember.UserID;
                        login = match.PlayerMember.UserLogin;
                        if (l.Host == "skribbl.io")
                        {
                            try
                            {
                                BubbleWallet.AddBubble(login);
                            }
                            catch (Microsoft.Data.Sqlite.SqliteException e) // catch sql exceptions
                            {
                                if (e.SqliteErrorCode == 8)
                                    Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Locked DB. Skipped adding bubble for " + login);
                                else
                                    Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > DB Error: " + e.SqliteErrorCode + ". Skipped adding bubble for " + login);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Unhandled error adding Bubble for login " + login + " : \n" + e.ToString());
                            }
                            try
                            {
                                BubbleWallet.SetOnlineSprite(login, l.Key, player.LobbyPlayerID);
                            }
                            catch (Microsoft.Data.Sqlite.SqliteException e) // catch sql exceptions
                            {
                                if (e.SqliteErrorCode == 8)
                                    Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Locked DB. Skipped setting sprite for " + login);
                                else
                                    Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > DB Error: " + e.SqliteErrorCode + ". Skipped setting sprite for " + login);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Unhandled error setting Sprite / Scene for login " + login);
                            }
                        }
                            
                    }

                    if (player.Sender)
                    {
                        string line = "";
                        line += Formatter.Sanitize(player.Name).Replace("\\","").Replace("´","");
                        line += new string(' ', (20 - player.Name.Length) < 0 ? 0 : (20 - player.Name.Length));
                        line += player.Score + " pts";
                        if (player.Score != 0)
                        {
                            if (scores.IndexOf(player.Score) == 0) line += " 🏆 ";
                            if (scores.IndexOf(player.Score) == 1) line += " 🥈 ";
                            if (scores.IndexOf(player.Score) == 2) line += " 🥉 ";
                        }
                        line += new string(' ', (32 - line.Length) < 0 ? 0 : (32 - line.Length));
                        string patronEmoji = "";
                        if (Program.Feanor.PatronEmojis.Count > 0 && Program.Feanor.PatronEmojis.Keys.Any(key => key == login)) patronEmoji = Program.Feanor.PatronEmojis[login];
                        if (patronEmoji.Length > 0)
                        {
                            line += "  " + patronEmoji + " ";
                        }
                        else if (l.Host == "skribbl.io") line += "  🔮 " + BubbleWallet.GetBubbles(login) + " Bubbles";
                        line += player.Drawing ? " 🖍 \n" : "\n";
                        sender += line;
                    }
                    else 
                    {
                        if(player.Score != 0)
                        {
                            if (scores.IndexOf(player.Score) == 0) players += " `🏆` ";
                            if (scores.IndexOf(player.Score) == 1) players += " `🥈` ";
                            if (scores.IndexOf(player.Score) == 2) players += " `🥉` ";
                        }
                        players += Formatter.Sanitize(player.Name);
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
                message += " "; // lobby break indicator em space

                //Set lobby id to index (for displaying) and unique id (for searching)
                l.ID = l. ID + ":" + lobbyUniqueID;
            });

            string searching = "";
            foreach (PlayerStatus p in OnlinePlayers.Where(o => o.Status == "searching" && o.PlayerMember.Guilds.Any(g=>g.GuildID == PalantirEndpoint.GuildID) && !guildLobbies.Any(l => l.Players.Any(p => p.ID == o.PlayerMember.UserID)))){
                try { searching += Formatter.Sanitize(p.PlayerMember.UserName) + ", "; } catch {}
            }

            string waiting = "";
            foreach (PlayerStatus p in OnlinePlayers.Where(o => o.Status == "waiting" && o.PlayerMember.Guilds.Any(g => g.GuildID == PalantirEndpoint.GuildID) && !guildLobbies.Any(l => l.Players.Any(p => p.ID == o.PlayerMember.UserID))))
            {
                try { waiting += Formatter.Sanitize(p.PlayerMember.UserName) + ", "; } catch {}
            }

            if (searching.Length > 0) message += "<a:onmyway:718807079305084939>   " + searching[0..^2];
            if (waiting.Length > 0 && guildLobbies.Count > 0) message += "\n\n:octagonal_sign:   " + waiting[0..^2];
            if (PalantirSettings.ShowAnimatedEmojis && guildLobbies.Count == 0 && searching.Length == 0) message += "\n <a:alone:718807079434846238>\n";
            if (guildLobbies.Count == 0 && searching.Length == 0) message += PalantirSettings.IdleMessage;

            GuildLobbiesEntity entity = database.GuildLobbies.FirstOrDefault(g => g.GuildID == PalantirEndpoint.GuildID);

            if (entity != null)
            {
                entity.Lobbies = JsonConvert.SerializeObject(guildLobbies);
                database.SaveChanges();
            }
            else
            {
                entity = new GuildLobbiesEntity();
                entity.GuildID = PalantirEndpoint.GuildID;
                entity.Lobbies = JsonConvert.SerializeObject(guildLobbies);
                database.GuildLobbies.Add(entity);
                database.SaveChanges();
            }
            database.Dispose();
            return message;
        }
    }
}
