using System.Diagnostics;
using System.Text.RegularExpressions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Linq;
using MoreLinq.Extensions;
using System;
using System.Collections.Generic;

namespace Palantir.Commands
{
    public class EventCommands : BaseCommandModule
    {
        [Description("Show event info")]
        [Command("event")]
        public async Task ShowEvent(CommandContext context, int eventID = 0)
        {
            List<EventEntity> events = Events.GetEvents(false);
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            List<SpriteProperty> inv = BubbleWallet.GetInventory(login);
            EventEntity evt;
            if (eventID < 1 || !events.Any(e => e.EventID == eventID)) evt = (Events.GetEvents().Count > 0 ? Events.GetEvents()[0] : null);
            else evt = events.FirstOrDefault(e => e.EventID == eventID);
            if (evt != null)
            {
                DateTime eventStart = Convert.ToDateTime(evt.ValidFrom);
                DateTime eventEnd = eventStart.AddDays(evt.DayLength);
                embed.Title = ":champagne: " + evt.EventName;
                embed.Color = DiscordColor.Magenta;
                embed.WithDescription(evt.Description + "\nLasts from " + eventStart.ToString("MMMM dd") + " until " + eventEnd.ToString("MMMM dd") + "\n");

                string dropList = "";
                List<SpritesEntity> eventsprites = new List<SpritesEntity>();
                List<EventDropEntity> eventdrops = Events.GetEventDrops(new List<EventEntity> { evt });
                eventdrops.ForEach(e =>
                {
                    List<SpritesEntity> sprites = Events.GetEventSprites(e.EventDropID);
                    dropList += "\n**" + e.Name + "**  (" + BubbleWallet.GetEventCredit(login, e.EventDropID) + " caught) (`#" + e.EventDropID + "`)";
                    int spent = inv.Where(spt => sprites.Any(eventsprite => eventsprite.ID == spt.ID)).Sum(spt => spt.Cost);
                    sprites.OrderBy(sprite => sprite.ID).ForEach(sprite =>
                       dropList += "\n> ‎ \n> ➜ **" + sprite.Name + "** (`#" + sprite.ID + "`)\n> "
                        + (inv.Any(s => s.ID == sprite.ID) ? ":package: " : (BubbleWallet.GetEventCredit(login, sprite.EventDropID) - spent + " / "))
                        + sprite.Cost + " " + e.Name + " Drops "
                    );
                    dropList += "\n";
                });
                embed.AddField("Event Sprites", dropList == "" ? "No drops added yet." : dropList);
                embed.AddField("\u200b", "Use `>sprite [id]` to see the event drop and sprite!");

                SceneEntity scene = BubbleWallet.GetAvailableScenes().FirstOrDefault(scene => scene.EventID == evt.EventID);
                if (scene != null)
                {
                    int collectedBubbles = BubbleWallet.GetCollectedBubblesInTimespan(eventStart, eventEnd.AddDays(-1), login);
                    bool hasScene = BubbleWallet.GetSceneInventory(login, false, false).Any(prop => prop.ID == scene.ID);
                    embed.WithImageUrl(scene.URL);
                    embed.AddField("\n\u200b \nEvent Scene: **" + scene.Name + "**", "> \n> " + (hasScene ? ":package:" : "") + collectedBubbles + " / " + (evt.DayLength * Events.eventSceneDayValue) + " Bubbles collected");
                }

                // league stuff
                List<PastDropEntity> leaguedrops = new List<PastDropEntity>();
                var credit = Events.GetAvailableLeagueTradeDrops(context.User.Id.ToString(), evt, out leaguedrops);
                if (credit > 0) embed.AddField("\n\u200b \nLeague Event Drops", "You have " + Math.Round(credit, 1).ToString() + " League Drops to redeem! \nYou can swap them with the command `>redeem [amount] [event drop id]` to any of this event's Event Drops.");
            }
            else
            {
                embed.Title = ":champagne: No Event active :(";
                embed.Color = DiscordColor.Magenta;
                embed.WithDescription("Check new events with `>upcoming`.\nSee all past events with `>passed` and a specific with `>event [id]`.\nGift event drops with `>gift [@person] [amount of drops] [id of the sprite]`.\nBtw - I keep up to 50% of the gift for myself! ;)");
            }

            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Swap League Event Drop credit to  Event Drops")]
        [Command("redeem")]
        public async Task Redeem(CommandContext context, int amount, int eventDropID)
        {
            if (amount < 0)
            {
                await Program.SendEmbed(context.Channel, "What's that supposed to mean?", "no comment");
                return;
            }
            if (eventDropID < 1)
            {
                await Program.SendEmbed(context.Channel, "CONGRATSASDJKAHKDJ!!!!", "You found the super duper secret easteregg! ||jk, read the manual||");
                return;
            }

            var events = Events.GetEvents(false);
            var drops = Events.GetEventDrops();
            var drop = drops.Find(drop => drop.EventDropID == eventDropID);

            if (drop is null)
            {
                await Program.SendEmbed(context.Channel, "Watch out :o", eventDropID + " is no valid event drop");
                return;
            }

            var target = events.Find(evt => evt.EventID == drop.EventID);
            List<PastDropEntity> consumable;
            double credit = Events.GetAvailableLeagueTradeDrops(context.User.Id.ToString(), target, out consumable);

            if (credit < amount)
            {
                await Program.SendEmbed(context.Channel, "Sad times :c", "Your credit (" + Math.Round(credit, 1) + ") is too low!");
                return;
            }

            List<PastDropEntity> spent = new();
            //consumable = consumable.OrderByDescending(drop => Palantir.League.Weight(drop.LeagueWeight / 1000.0) / 100).ToList();
            double spent_count = 0;

            while (spent_count < amount)
            {
                spent.Add(consumable.First());
                spent_count += Palantir.League.Weight(consumable.First().LeagueWeight / 1000.0) / 100;
                consumable.RemoveAt(0);
            }

            int result = Events.TradeLeagueEventDrops(spent, eventDropID, BubbleWallet.GetLoginOfMember(context.User.Id.ToString()));

            await Program.SendEmbed(context.Channel, "Congrats!", "You traded " + result + " of your Event League Credit to " + drop.Name);
            return;
        }

        [Description("Show passed events")]
        [Command("passed")]
        public async Task PassedEvents(CommandContext context)
        {
            List<EventEntity> events = Events.GetEvents(false);
            string eventsList = "";
            events = events.Where(e => Convert.ToDateTime(e.ValidFrom).AddDays(e.DayLength) < DateTime.Now).OrderByDescending(e => Convert.ToDateTime(e.ValidFrom)).ToList();
            events.ForEach(e =>
            {
                eventsList += "➜ **" + e.EventName + "** [#" + e.EventID + "]: " + e.ValidFrom + " to " + Convert.ToDateTime(e.ValidFrom).AddDays(e.DayLength).ToShortDateString() + "\n";
                //eventsList += e.Description + "\n\n";
            });
            if (eventsList == "") eventsList = "There have no events passed.";

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Passed Events:";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription(eventsList);
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Show upcoming events")]
        [Command("upcoming")]
        public async Task UpcomingEvents(CommandContext context)
        {
            List<EventEntity> events = Events.GetEvents(false);
            string eventsList = "";
            events = events.Where(e => Convert.ToDateTime(e.ValidFrom) >= DateTime.Now).OrderByDescending(e => Convert.ToDateTime(e.ValidFrom)).ToList();
            events.ForEach(e =>
            {
                eventsList += "➜ **" + e.EventName + "**: " + e.ValidFrom + " to " + Convert.ToDateTime(e.ValidFrom).AddDays(e.DayLength).ToShortDateString() + "\n";
                eventsList += e.Description + "\n\n";
            });
            if (eventsList == "") eventsList = "There are no upcoming events :( \nAsk a responsible person to create one!";

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Upcoming Events:";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription(eventsList);
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Gift event drops")]
        [Command("gift")]
        public async Task Gift(CommandContext context, [Description("The gift receiver (@member)")] DiscordMember target, [Description("The amount of gifted event drops")] int amount, [Description("The id of the sprite which can be bought with the gifted event drops")] int eventSpriteID)
        {
            if (amount < 0)
            {
                await Program.SendEmbed(context.Channel, "LOL!", "Your'e tryna steal some stuff, huh?");
                return;
            }
            List<Sprite> sprites = BubbleWallet.GetAvailableSprites();
            if (!sprites.Any(s => s.ID == eventSpriteID && s.EventDropID != 0))
            {
                await Program.SendEmbed(context.Channel, "Hmmm...", "That sprite doesn't exist or is no event sprite.");
                return;
            }
            int eventDropID = sprites.FirstOrDefault(s => s.ID == eventSpriteID).EventDropID;
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            int credit = BubbleWallet.GetRemainingEventDrops(login, eventDropID);
            int total = BubbleWallet.GetEventCredit(login, eventDropID);
            if (amount < 3 && total >= 3)
            {
                await Program.SendEmbed(context.Channel, "That's all you got?", "With more than 3 drops collected, the minimal gift amount is 3 event drops.");
                return;
            }
            List<SpriteProperty> inv = BubbleWallet.GetInventory(login);

            List<EventDropEntity> drops = Events.GetEventDrops();
            string name = drops.FirstOrDefault(d => d.EventDropID == eventDropID).Name;
            if (credit - amount < 0)
            {
                await Program.SendEmbed(context.Channel, "You can't trick me!", "Your event credit is too few. You have only " + credit + " " + name + " left.");
                return;
            }
            int lost = amount >= 3 ? (new Random()).Next(0, amount / 3 + 1) : (new Random()).Next(0, 2);
            string targetLogin = BubbleWallet.GetLoginOfMember(target.Id.ToString());

            if (BubbleWallet.ChangeEventDropCredit(targetLogin, eventDropID, amount - lost))
                BubbleWallet.ChangeEventDropCredit(login, eventDropID, -amount);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne: Awww!";
            embed.WithDescription("You gifted " + target.DisplayName + " " + amount + " " + name + "!\nHowever, " + lost + " of them got lost in my pocket :(");
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Create a new seasonal event")]
        [Command("newevent")]
        [RequirePermissionFlag((byte)4)]
        public async Task CreateEvent(CommandContext context, [Description("The event name")] string name, [Description("The duration of the event in days")] int duration, [Description("The count of days when the event will start")] int validInDays, [Description("The event description")] params string[] description)
        {
            PalantirDbContext dbcontext = new PalantirDbContext();

            EventEntity newEvent = new EventEntity();
            newEvent.EventName = name.Replace("_", " ");
            newEvent.DayLength = duration;
            newEvent.ValidFrom = DateTime.Now.AddDays(validInDays).ToShortDateString();
            newEvent.Description = description.ToDelimitedString(" ");
            if (dbcontext.Events.Count() <= 0) newEvent.EventID = 0;
            else newEvent.EventID = dbcontext.Events.Max(e => e.EventID) + 1;

            if (dbcontext.Events.ToList().Any(otherEvent =>
                    !((Convert.ToDateTime(newEvent.ValidFrom) > Convert.ToDateTime(otherEvent.ValidFrom).AddDays(otherEvent.DayLength)) || // begin after end
                    (Convert.ToDateTime(otherEvent.ValidFrom) > Convert.ToDateTime(newEvent.ValidFrom).AddDays(newEvent.DayLength)))      // end before begin
                )
             )
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's already an event running in that timespan.\nCheck '>event'");
                return;
            }

            dbcontext.Events.Add(newEvent);
            dbcontext.SaveChanges();
            dbcontext.Dispose();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Event created: **" + newEvent.EventName + "**";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("The event lasts from  " + newEvent.ValidFrom + " to " + Convert.ToDateTime(newEvent.ValidFrom).AddDays(newEvent.DayLength).ToShortDateString());
            embed.AddField("Make the event fancy!", "➜ `>eventdrop " + newEvent.EventID + " coolname` Send this command with an attached gif to add a event drop.");

            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Add a seasonal sprite to an event")]
        [Command("eventsprite")]
        [RequirePermissionFlag((byte)4)] // 4 -> mod
        public async Task CreateEventSprite(CommandContext context, [Description("The id of the event drop for the sprite")] int eventDropID, [Description("The name of the sprite")] string name, [Description("The event drop price")] int price, [Description("Any string except '-' if the sprite should replace the avatar")] string special = "", [Description("Any string except '-' to set the sprite artist")] string artist = "")
        {
            PalantirDbContext dbcontext = new PalantirDbContext();

            if (!dbcontext.EventDrops.Any(e => e.EventDropID == eventDropID))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no event drop with that id.\nCheck `>upevent`");
                return;
            }
            if (context.Message.Attachments.Count <= 0 || !context.Message.Attachments[0].FileName.EndsWith(".gif"))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no valid gif attached.");
                return;
            }
            if (price < 5)
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "We don't gift sprites. The price is too low.");
                return;
            }
            if (String.IsNullOrWhiteSpace(name))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "Something went wrong with the name.");
                return;
            }

            // download sprite
            System.Net.WebClient client = new System.Net.WebClient();
            client.DownloadFile(context.Message.Attachments[0].Url, "/home/pi/Webroot/eventsprites/evd" + eventDropID + name.Replace("'", "-") + ".gif");

            Sprite eventsprite = new Sprite(
                name.Replace("_", " "),
                "https://tobeh.host/eventsprites/evd" + eventDropID + name.Replace("'", "-") + ".gif",
                price,
                dbcontext.Sprites.Where(s => s.ID < 1000).Max(s => s.ID) + 1,
                special != "-" && special != "",
                eventDropID,
                artist == "" || artist == "-" ? null : artist
            );
            BubbleWallet.AddSprite(eventsprite);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Sprite added to " + dbcontext.EventDrops.FirstOrDefault(e => e.EventDropID == eventDropID).Name + ": **" + eventsprite.Name + "**";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("ID: " + eventsprite.ID + "\nYou can buy and view the sprite with the usual comands.");
            embed.WithThumbnail(eventsprite.URL);

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Add a seasonal drop to an event")]
        [Command("eventdrop")]
        [RequirePermissionFlag((byte)4)]
        public async Task CreateEventDrop(CommandContext context, [Description("The id of the event for the event drop")] int eventID, [Description("The name of the event drop")] string name)
        {
            PalantirDbContext dbcontext = new PalantirDbContext();

            if (!dbcontext.Events.Any(e => e.EventID == eventID))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no event with that id.\nCheck `>upcoming`");
                return;
            }
            if (context.Message.Attachments.Count <= 0 || !context.Message.Attachments[0].FileName.EndsWith(".gif"))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no valid gif attached.");
                return;
            }

            EventDropEntity newDrop = new EventDropEntity();
            newDrop.EventID = eventID;
            newDrop.Name = name.Replace("_", " ");
            newDrop.URL = context.Message.Attachments[0].Url;
            if (dbcontext.EventDrops.Count() <= 0) newDrop.EventDropID = 0;
            else newDrop.EventDropID = dbcontext.EventDrops.Max(e => e.EventDropID) + 1;

            dbcontext.EventDrops.Add(newDrop);
            dbcontext.SaveChanges();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Drop added to " + dbcontext.Events.FirstOrDefault(e => e.EventID == eventID).EventName + ": **" + newDrop.Name + "**";
            embed.Color = DiscordColor.Magenta;
            embed.WithThumbnail(newDrop.URL);
            embed.WithDescription("The ID of the Drop is  " + newDrop.EventDropID + ".\nAdd a seasonal Sprite which can be bought with the event drops to make your event complete:\n" +
                "➜ `>eventsprite " + newDrop.EventDropID + " [name] [price]` with the sprite-gif attached.");

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Show the current Drop League season ranking")]
        [Command("league")]
        public async Task League(CommandContext context, int month = -1, int year = -1)
        {

            if (year == -1) year = DateTime.Now.Year;
            if (month == -1) month = DateTime.Now.Month;

            var season = new League(month.ToString(), year.ToString());
            var results = season.LeagueResults().OrderByDescending(l => l.Score).ToList();

            var embed = new DiscordEmbedBuilder()
                    .WithAuthor("Drop League")
                    .WithTitle("**" + DateTime.Now.ToString("MMMM yyyy") + "** Season")
                    .WithColor(DiscordColor.Magenta)
                    .WithThumbnail("https://media.discordapp.net/attachments/910894527261327370/983025068214992948/challenge.gif")
                    .WithDescription("Drop Leagues are a monthly competition, where the very fastest catchers rank against each other.\n_ _\n" + results.Count + " participants in this season\n_ _ \n" + (season.IsActive() ? "Season ends <t:" + season.GetEndTimestamp() + ":R>\n_ _" : "Ended <t:" + season.GetEndTimestamp() + ">\n_ _"));

            void AddTop(League.MemberLeagueResult result, int rank, string emote)
            {
                var member = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(result.Login).Member);
                embed.AddField(
                    emote + "_ _  _ _ `#" + rank + "`  **" + member.UserName + "**",
                    "> `" + result.Score + "dw`\n> ***" + result.LeagueDrops.Count + "** League Drops*\n> ***" + result.AverageWeight + "%** avg.weight*\n> ***" + result.AverageTime + "ms** avg.time*\n> ***" + result.Streak + "** max.streak*",
                    true
                );
            }

            if (results.Count() > 0) AddTop(results[0], 1, "<a:league_rnk1:987699431350632518>");
            if (results.Count() > 1) AddTop(results[1], 2, "<a:league_rnk2:987710613893566515>");
            if (results.Count() > 2) AddTop(results[2], 3, "<a:league_rnk3:987716889352470528>");

            string LowerText(List<League.MemberLeagueResult> results)
            {
                string content = "";
                foreach (var result in results)
                {
                    var member = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(result.Login).Member);
                    content += "> " + member.UserName + " `" + result.Score + "dw / " + result.AverageWeight + "%`\n";
                }
                return content;
            }

            if (results.Count() > 3) embed.AddField("<a:league_rnk4:987723143982514207>_ _  _ _ `#4 - #7`", LowerText(results.GetRange(3, 4)));
            if (results.Count() > 7) embed.AddField("<a:league_rnk4:987723143982514207>_ _  _ _ `#8 - #10`", LowerText(results.GetRange(7, 3)));

            if (results.Count > 0)
            {
                var maxOverall = results.Max(r => r.Score);
                var overall = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(
                    Program.Feanor.GetMemberByLogin(results.Find(r => r.Score == maxOverall).Login).Member
                );

                var maxWeight = results.Max(r => r.AverageWeight);
                var weight = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(
                    Program.Feanor.GetMemberByLogin(results.Find(r => r.AverageWeight == maxWeight).Login).Member
                );

                var maxCount = results.Max(r => r.LeagueDrops.Count);
                var count = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(
                    Program.Feanor.GetMemberByLogin(results.Find(r => r.LeagueDrops.Count == maxCount).Login).Member
                );

                var maxStreak = results.Max(r => r.Streak);
                var streak = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(
                    Program.Feanor.GetMemberByLogin(results.Find(r => r.Streak == maxStreak).Login).Member
                );

                embed.AddField(
                    "_ _\n`⚔️` Category Leaders",
                    "➜ **Overall**: " + overall.UserName + " (`" + maxOverall + "dw`)\n➜ **Average Weight**: "
                        + weight.UserName + " (`" + maxWeight + "%`)\n➜ **League Drops**: " + count.UserName + " (`" + maxCount + " drops`)"
                        + "\n➜ **League Drop Streak**: " + streak.UserName + " (`" + maxStreak + " drops`)",
                    true
                );
            }

            embed.AddField("_ _ \n`🎖️` Rewards", "➜ **Overall:** #1 : 4 Splits, #2-3: 3 Splits,  #4-10: 2 Splits\n➜ **Weight Leader: **3 Splits\n➜ **League Drops Leader: **3 Splits\n➜ **Streak Leader: **3 Splits");
            embed.AddField("\u200b ", "BTW: check your own rank with `>rank`");
            await context.RespondAsync(embed);

        }

        [Description("Show your current Drop League season ranking")]
        [Command("rank")]
        public async Task Rank(CommandContext context, [Description("Month of the league season, eg `11`")] int month = -1, [Description("Year of the league season, eg `2022`")] int year = -1, [Description("Command options: `all` to see the total ranking, `help` to see with explaination")] string modifier = "help")
        {

            if (year == -1) year = DateTime.Now.Year;
            if (month == -1) month = DateTime.Now.Month;

            var season = new League(month.ToString(), year.ToString());
            var results = season.LeagueResults().OrderByDescending(l => l.Score).ToList();
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            int position = results.FindIndex(result => result.Login == login) + 1;


            if (modifier == "all")
            {
                string msg = "```\n";
                msg += "                                Leaderboard Drop League Season " + month.ToString().PadLeft(2, ' ') + "/" + year.ToString().PadLeft(2, ' ') + "\n \n";
                msg += "｜Rank｜      Name     ｜ Score ｜Ø Weight｜Streak｜ ｜Rank｜     Name     ｜ Score ｜Ø Weight｜Streak｜\n";

                string ranks = "";
                results.Batch(2).ForEach((batch, i) =>
                {
                    var aBatch = batch.ToArray();
                    var rank1 = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(aBatch[0].Login).Member).UserName;
                    ranks += $"｜#{ i * 2 + 1,3 }｜{ rank1,15 }｜{ aBatch[0].Score,6 } ｜{ aBatch[0].AverageWeight,6 }% ｜{ aBatch[0].Streak,5 } ｜ ";

                    if (aBatch.Length > 1)
                    {
                        var rank2 = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(aBatch[1].Login).Member).UserName;
                        ranks += $"｜#{ i * 2 + 2,3 }｜{ rank2,15 }｜{ aBatch[1].Score,6 } ｜{ aBatch[1].AverageWeight,6 }% ｜{ aBatch[1].Streak,5 } ｜\n";
                    }
                    else ranks += "\n";
                });

                string page = "" + msg;
                List<DSharpPlus.Interactivity.Page> pages = new();
                ranks.Split("\n").ForEach(line =>
                {
                    if (page.Length + line.Length < 1800) page += line + "\n";
                    else
                    {
                        pages.Add(new DSharpPlus.Interactivity.Page(page + "\n```"));
                        page = msg + line + "\n";
                    }
                });

                if(page.Length > 0) pages.Add(new DSharpPlus.Interactivity.Page(page + "\n```"));

                await Program.Interactivity.SendPaginatedMessageAsync(context.Channel, context.Message.Author, pages);
                //results.Batch(5).ForEach((batch, i) =>
                //{
                //    var aBatch = batch.ToArray();
                //    embed.AddField("\u200b ", "" +
                //        "#" + (i * 5 + 1) + " - " + Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(aBatch[0].Login).Member).UserName + "\n`" + aBatch[0].Score + "dw / " + aBatch[0].AverageWeight + "% / " + aBatch[0].Streak + "`\n_ _\n" +
                //        (aBatch.Length > 1 ? "#" + (i * 5 + 2) + " - " + Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(aBatch[1].Login).Member).UserName + "\n`" + aBatch[1].Score + "dw / " + aBatch[1].AverageWeight + "% / " + aBatch[1].Streak + "`\n_ _\n" : "") +
                //        (aBatch.Length > 2 ? "#" + (i * 5 + 3) + " - " + Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(aBatch[2].Login).Member).UserName + "\n`" + aBatch[2].Score + "dw / " + aBatch[2].AverageWeight + "% / " + aBatch[2].Streak + "`\n_ _\n" : "") +
                //        (aBatch.Length > 3 ? "#" + (i * 5 + 4) + " - " + Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(aBatch[3].Login).Member).UserName + "\n`" + aBatch[3].Score + "dw / " + aBatch[3].AverageWeight + "% / " + aBatch[3].Streak + "`\n_ _\n" : "") +
                //        (aBatch.Length > 4 ? "#" + (i * 5 + 5) + " - " + Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(aBatch[4].Login).Member).UserName + "\n`" + aBatch[4].Score + "dw / " + aBatch[4].AverageWeight + "% / " + aBatch[4].Streak + "`\n" : ""),
                //        true
                //    );
                //});
            }

            else if (modifier == "help" || modifier == "")
            {
                if (position <= 0) await Program.SendEmbed(context.Channel, "Oopsie", "You aren't ranked in this season." + (season.IsActive() ? " Catch some drops faster than 1000ms to appear in the ranking!" : ""));
                else
                {

                    var embed = new DiscordEmbedBuilder()
                    .WithAuthor("Drop League")
                    .WithTitle("**" + DateTime.Now.ToString("MMMM yyyy") + "** Season")
                    .WithColor(DiscordColor.Magenta)
                    .WithThumbnail("https://media.discordapp.net/attachments/910894527261327370/983025068214992948/challenge.gif")
                    .WithDescription("Drop Leagues are a monthly competition, where the very fastest catchers rank against each other.\n_ _\n" + results.Count + " participants in this season\n_ _ \n" + (season.IsActive() ? "Season ends <t:" + season.GetEndTimestamp() + ":R>\n_ _" : "Ended <t:" + season.GetEndTimestamp() + ">\n_ _"));

                    if (position == 1)
                    {
                        embed.AddField("_ _\n<a:league_rnk1:987699431350632518>  _ _ You are ranked as #1!", "\u200b "); // \u200b 
                    }
                    else if (position == 2)
                    {
                        embed.AddField("_ _\n<a:league_rnk2:987710613893566515>  _ _ You are ranked as #2!", "\u200b ");
                    }
                    else if (position == 3)
                    {
                        embed.AddField("_ _\n<a:league_rnk3:987716889352470528>  _ _ You are ranked as #3!", "\u200b ");
                    }
                    else if (position <= 10)
                    {
                        embed.AddField("_ _\n<a:league_rnk4:987723143982514207>  _ _ You are ranked as #" + position, "You are below the top 10 ranked players this season.");
                    }
                    else
                    {
                        embed.AddField("_ _\n<a:league_rnk4:987723143982514207>  _ _ You are ranked as #" + position, "Catch more League Drops to be ranked below the top 10 players.");
                    }

                    var maxAvg = results.Max(r => r.AverageWeight);
                    var sortMaxAvg = results.OrderByDescending(r => r.AverageWeight);
                    var selfMaxAvg = sortMaxAvg.ToList().IndexOf(results[position - 1]) + 1;
                    if (results.Find(r => r.AverageWeight == maxAvg).Login == login)
                    {
                        embed.AddField("<a:league_rnk1:987699431350632518>  _ _ Leader in the category `Average Weight`", "\u200b ");
                    }

                    var maxCount = results.Max(r => r.LeagueDrops.Count);
                    var sortMaxCount = results.OrderByDescending(results => results.LeagueDrops.Count);
                    var selfMaxCount = sortMaxCount.ToList().IndexOf(results[position - 1]) + 1;
                    if (results.Find(r => r.LeagueDrops.Count == maxCount).Login == login)
                    {
                        embed.AddField("<a:league_rnk1:987699431350632518>  _ _  Leader in the category `League Drops`", "\u200b ");
                    }

                    var maxStreak = results.Max(r => r.Streak);
                    var sortMaxStreak = results.OrderByDescending(results => results.Streak);
                    var selfMaxStreak = sortMaxStreak.ToList().IndexOf(results[position - 1]) + 1;
                    if (results.Find(r => r.Streak == maxStreak).Login == login)
                    {
                        embed.AddField("<a:league_rnk1:987699431350632518>  _ _  Leader in the category `Maximum Streak`", "\u200b ");
                    }

                    embed.AddField(
                       "➜ _ _ Your Stats",
                       "> `" + results[position - 1].Score + "dw`\n> ***" + results[position - 1].LeagueDrops.Count
                        + "** League Drops (#" + position + ")*\n> ***" + results[position - 1].AverageWeight
                        + "%** avg.weight (#" + selfMaxAvg + ")*\n> ***" + results[position - 1].AverageTime
                        + "ms** avg.time *\n> ***" + results[position - 1].Streak + "** max.streak (#" + selfMaxStreak + ")*",
                       false
                    );

                    embed.AddField("_ _\n➜ _ _ About ranking", "\n> ➜ The **overall ranking leader** is the player with the most collected 'drop weight / `dw`'. Each League Drop you collect is weighted by how fast you catch it and adds to your score."
                    + "\n_ _ \n> ➜ The **average weight leader** is the player that has the highest average drop weight - this means, this player has the fastest average catch time!"
                    + "\n_ _ \n> ➜ The **league drops leader** is the player with the most total collected League Drops."
                    + "\n_ _ \n> ➜ The **maximum streak leader** is the player with the highest caught League Drop streak. Only League Drops count, otherwise the streak is broken."
                    + "\n_ _ \n> ➜ When you catch a League Drop, it is weighted (between 0.1 and 1) by how fast you were and added to your Drop/Bubble credit."
                    + "\n_ _ \n> ➜ When you catch a Event League Drop, it is also weighted. Using `>event` you can see your collected Event League Dropsfor an Event. You can trade them to any Eventdrop of this event!");

                    await context.RespondAsync(embed);
                }
            }
        }


        [Description("Show your current Drop League season ranking - cleaner without help ;)")]
        [Command("rnk")]
        public async Task RankWithoutHelp(CommandContext context, string modifier = "", int month = -1, int year = -1)
        {
            await Rank(context, month, year, modifier);
        }
    }
}

