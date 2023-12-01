﻿using System.Diagnostics;
using System.Text.RegularExpressions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Linq;
using MoreLinq.Extensions;
using System;
using System.Collections.Generic;
using Palantir.Model;
using System.IO;
using Palantir.PalantirCommandModule;

namespace Palantir.Commands
{
    public class EventCommands : PalantirCommandModule.PalantirCommandModule
    {
        [Description("Show event info")]
        [Command("event")]
        public async Task ShowEvent(CommandContext context, int eventID = 0)
        {
            List<Event> events = Events.GetEvents(false);
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            List<SpriteProperty> inv = BubbleWallet.GetInventory(login);
            Event evt;
            if (eventID < 1 || !events.Any(e => e.EventId == eventID)) evt = (Events.GetEvents().Count > 0 ? Events.GetEvents()[0] : null);
            else evt = events.FirstOrDefault(e => e.EventId == eventID);
            if (evt != null)
            {
                DateTime eventStart = Program.ParseDateAsUtc(evt.ValidFrom);
                DateTime eventEnd = eventStart.AddDays(evt.DayLength);
                embed.Title = ":champagne: " + evt.EventName;
                embed.Color = DiscordColor.Magenta;
                embed.WithDescription(evt.Description + 
                    "\nLasts from <t:" + new DateTimeOffset(eventStart).ToUnixTimeSeconds() + ":D> until <t:" 
                    + new DateTimeOffset(eventEnd).ToUnixTimeSeconds() + ":D>\n");

                string dropList = "";
                List<Model.Sprite> eventsprites = new List<Model.Sprite>();
                var progressiveDrops = Events.GetProgressiveEventDrops(evt);
                List<EventDrop> eventdrops = Events.GetEventDrops(new List<Event> { evt });
                eventdrops.ForEach(e =>
                {
                    var progressiveDropInfo = progressiveDrops.FirstOrDefault(d => d.drop.EventDropId == e.EventDropId);
                    List<Model.Sprite> sprites = Events.GetEventSprites(e.EventDropId);
                    eventsprites.AddRange(sprites);
                    dropList += "\n**" + e.Name + " Drop**  (" + BubbleWallet.GetEventCredit(login, e.EventDropId) + " caught) (`#" + e.EventDropId + "`)";

                    if(evt.Progressive == 1 && !progressiveDropInfo.isRevealed)
                    {
                        dropList += $"\n> Will be collectable from <t:{progressiveDropInfo.revealTimeStamp}:d> to <t:{progressiveDropInfo.endTimestamp}:d>\n";
                    }
                    else
                    {
                        int spent = inv.Where(spt => sprites.Any(eventsprite => eventsprite.Id == spt.ID)).Sum(spt => spt.Cost);
                        sprites.OrderBy(sprite => sprite.Id).ForEach(sprite =>
                           dropList += "\n> ‎ \n> ➜ **" + sprite.Name + "** (`#" + sprite.Id + "`)\n> "
                            + (inv.Any(s => s.ID == sprite.Id) ? ":package: " : (BubbleWallet.GetEventCredit(login, sprite.EventDropId) - spent + " / "))
                            + sprite.Cost + " " + e.Name + " Drops "
                        );
                        dropList += "\n";
                    }
                });
                embed.AddField("Event Sprites", dropList == "" ? "No drops added yet." : dropList);
                embed.AddField("\u200b", "Use `>sprite [id]` to see the event drop and sprite!");

                // progressive event info
                if (evt.Progressive == 1)
                {
                    embed.AddField("Progressive event", "The eventdrops unveil sequentially during the event - you can only collect them on certain days!");
                }

                // league stuff: for regular events
                if(evt.Progressive == 0)
                {
                    List<PastDrop> leaguedrops = new List<PastDrop>();
                    var credit = Events.GetAvailableLeagueTradeDrops(context.User.Id.ToString(), evt, out leaguedrops);
                    if (credit > 0) embed.AddField("\n\u200b \nLeague Event Drops", "> You have " + Math.Round(credit, 1, MidpointRounding.ToZero).ToString() + " League Drops to redeem! \n> You can swap them with the command `>redeem [amount] [event drop id]` to any of this event's Event Drops.");
                }

                // league stuff: for progressive events
                if (evt.Progressive == 1)
                {
                    var credits = Events.GetAvailableProgressiveLeagueTradeDrops(context.User.Id.ToString(), evt, out var leaguedrops);
                    //if (credit > 0) embed.AddField("\n\u200b \nLeague Event Drops", "> You have " + Math.Round(credit, 1, MidpointRounding.ToZero).ToString() + " League Drops to redeem! \n> You can swap them with the command `>redeem [amount] [event drop id]` to any of this event's Event Drops.");
                    if(credits.Values.Any(v => v > 0))
                    {
                        var creditInfo = string.Join("\n> ", credits.Keys.ToList().ConvertAll(key => $"`#{key}` {eventdrops.FirstOrDefault(d=>d.EventDropId==key).Name}: {Math.Round(credits[key], 1, MidpointRounding.ToZero)}"));
                        embed.AddField("\n\u200b \nLeague Event Drops", $"> You have following League Drops to redeem: \n{creditInfo}\n> \n> You can swap them with the command `>redeem [amount] [event drop id]`.");
                    }
                }

                Scene scene = BubbleWallet.GetAvailableScenes().FirstOrDefault(scene => scene.EventId == evt.EventId);
                if (scene != null)
                {
                    int collectedBubbles = BubbleWallet.GetCollectedBubblesInTimespan(eventStart, eventEnd.AddDays(-1), login);
                    bool hasScene = BubbleWallet.GetSceneInventory(login, false, false).Any(prop => prop.Id == scene.Id);
                    embed.WithImageUrl(scene.Url);
                    embed.AddField("\n\u200b \nEvent Scene: **" + scene.Name + "**", "> \n> " + (hasScene ? ":package:" : "") + collectedBubbles + " / " + (((evt.EventId == 15 ? -2 : 0) + evt.DayLength) * Events.eventSceneDayValue) + " Bubbles collected");
                }

                var collected = Events.GetCollectedEventDrops(context.Message.Author.Id.ToString(), evt);
                embed.WithFooter(Math.Round(collected) + " Drops total collected ~ Current gift loss rate: " + Math.Round(Events.CurrentGiftLossRate(eventsprites, collected), 3));

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
        [Synchronized]
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
            var drop = drops.Find(drop => drop.EventDropId == eventDropID);

            if (drop is null)
            {
                await Program.SendEmbed(context.Channel, "Watch out :o", eventDropID + " is no valid event drop");
                return;
            }

            var target = events.Find(evt => evt.EventId == drop.EventId);
            List<PastDrop> consumable;
            double credit = 0;
            if(target.Progressive == 1)
            {
                var credits = Events.GetAvailableProgressiveLeagueTradeDrops(context.User.Id.ToString(), target, out var consumables);
                credit = credits[drop.EventDropId];
                consumable = consumables[drop.EventDropId];
            }
            else
            {
                credit = Events.GetAvailableLeagueTradeDrops(context.User.Id.ToString(), target, out consumable);
            }

            if (credit < amount)
            {
                await Program.SendEmbed(context.Channel, "Sad times :c", "Your credit (" + Math.Round(credit, 1) + ") is too low!");
                return;
            }

            List<PastDrop> spent = new();
            //consumable = consumable.OrderByDescending(drop => Palantir.League.Weight(drop.LeagueWeight / 1000.0) / 100).ToList();
            double spent_count = 0;

            while (spent_count < amount)
            {
                spent.Add(consumable.First());
                spent_count += Palantir.League.Weight(consumable.First().LeagueWeight / 1000.0) / 100;
                consumable.RemoveAt(0);
            }

            int result = Events.TradeLeagueEventDrops(spent, eventDropID, BubbleWallet.GetLoginOfMember(context.User.Id.ToString()));
            if(result < 0)
            {
                await Program.SendEmbed(context.Channel, "Oops", "Something went wrong. Please try again.");
                return;
            }

            await Program.SendEmbed(context.Channel, "Congrats!", "You traded " + result + " of your Event League Credit to " + drop.Name);
            return;
        }

        [Description("Show passed events")]
        [Command("passed")]
        public async Task PassedEvents(CommandContext context)
        {
            List<Event> events = Events.GetEvents(false);
            string eventsList = "";
            events = events.Where(e => Program.ParseDateAsUtc(e.ValidFrom).AddDays(e.DayLength) < DateTime.Now).OrderByDescending(e => Program.ParseDateAsUtc(e.ValidFrom)).ToList();
            events.ForEach(e =>
            {
                eventsList += "➜ **" + e.EventName + "** [#" + e.EventId + "]: " + Program.DateTimeToStamp(e.ValidFrom,"d") + " to " + Program.DateTimeToStamp(Program.ParseDateAsUtc(e.ValidFrom).AddDays(e.DayLength),"d") + "\n";
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
            List<Event> events = Events.GetEvents(false);
            string eventsList = "";
            events = events.Where(e => Program.ParseDateAsUtc(e.ValidFrom) >= DateTime.Now).OrderByDescending(e => Program.ParseDateAsUtc(e.ValidFrom)).ToList();
            events.ForEach(e =>
            {
                eventsList += "➜ **" + e.EventName + "**: " + Program.DateTimeToStamp(e.ValidFrom, "d") + " to " + Program.DateTimeToStamp(Program.ParseDateAsUtc(e.ValidFrom).AddDays(e.DayLength), "d") + "\n";
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
        [Synchronized]
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
            //if (amount < 3 && total >= 3)
            //{
            //    await Program.SendEmbed(context.Channel, "That's all you got?", "With more than 3 drops collected, the minimal gift amount is 3 event drops.");
            //    return;
            //}

            List<EventDrop> drops = Events.GetEventDrops();
            var drop = drops.FirstOrDefault(d => d.EventDropId == eventDropID);
            var eventdrops = drops.Where(d => d.EventId == drop.EventId).ToList();
            var eventsprites = eventdrops.ConvertAll(d => Events.GetEventSprites(drop.EventDropId)).SelectMany(s => s).ToList();
            string name = drop.Name;
            if (credit - amount < 0)
            {
                await Program.SendEmbed(context.Channel, "You can't trick me!", "Your event credit is too few. You have only " + credit + " " + name + " left.");
                return;
            }
            var collected = Events.GetCollectedEventDrops(context.Message.Author.Id.ToString(), Events.GetEvents(false).FirstOrDefault(e => e.EventId == drop.EventId));

            double lossBase = Events.CurrentGiftLossRate(eventsprites, collected);
            int lossMin = Convert.ToInt16(Math.Round(lossBase * amount * 0.7));
            int lossMax = Convert.ToInt16(Math.Round(lossBase * amount * 1.1));
            if (lossMax < 1) lossMax = 1;
            int lost = new Random().Next(lossMin, lossMax);


            string targetLogin = BubbleWallet.GetLoginOfMember(target.Id.ToString());

            if (BubbleWallet.ChangeEventDropCredit(targetLogin, eventDropID, amount - lost))
                BubbleWallet.ChangeEventDropCredit(login, eventDropID, -amount);
            else
            {
                await Program.SendEmbed(context.Channel, "Oops", "Something went wrong. Please try again.");
                return;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne: Awww!";
            embed.WithDescription("You gifted " + target.DisplayName + " " + amount + " " + name + "!\nHowever, " + lost + " of them got lost in my pocket :(");
            embed.WithFooter(Math.Round(collected) + " Drops total collected ~ Current gift loss rate: " + Math.Round(lossBase, 3));
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Create a new seasonal event")]
        [Command("newevent")]
        [RequirePermissionFlag((byte)4)]
        public async Task CreateEvent(CommandContext context, [Description("The event name")] string name, [Description("The duration of the event in days")] int duration, [Description("The count of days when the event will start")] int validInDays, [Description("Indicator whether the event should be held progressive (0: false, other: true)")] int progressive, [Description("The event description")] params string[] description)
        {
            PalantirContext dbcontext = new PalantirContext();

            Event newEvent = new Event();
            newEvent.EventName = name.Replace("_", " ");
            newEvent.DayLength = duration;
            newEvent.Progressive = Convert.ToSByte(progressive == 0 ? 0 : 1);
            newEvent.ValidFrom = DateTime.Now.AddDays(validInDays).ToShortDateString();
            newEvent.Description = description.ToDelimitedString(" ");
            if (dbcontext.Events.Count() <= 0) newEvent.EventId = 0;
            else newEvent.EventId = dbcontext.Events.Max(e => e.EventId) + 1;

            if (dbcontext.Events.ToList().Any(otherEvent =>
                    !((Program.ParseDateAsUtc(newEvent.ValidFrom) > Program.ParseDateAsUtc(otherEvent.ValidFrom).AddDays(otherEvent.DayLength)) || // begin after end
                    (Program.ParseDateAsUtc(otherEvent.ValidFrom) > Program.ParseDateAsUtc(newEvent.ValidFrom).AddDays(newEvent.DayLength)))      // end before begin
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
            embed.WithDescription("The event lasts from  " + Program.DateTimeToStamp(newEvent.ValidFrom, "D") + " to " + Program.DateTimeToStamp(Program.ParseDateAsUtc(newEvent.ValidFrom).AddDays(newEvent.DayLength), "D"));
            embed.AddField("Make the event fancy!", "➜ `>eventdrop " + newEvent.EventId + " coolname` Send this command with an attached gif to add a event drop.");

            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Add a seasonal sprite to an event")]
        [Command("eventsprite")]
        [RequirePermissionFlag((byte)4)] // 4 -> mod
        public async Task CreateEventSprite(CommandContext context, [Description("The id of the event drop for the sprite")] int eventDropID, [Description("The name of the sprite")] string name, [Description("The event drop price")] int price, [Description("Any string except '-' if the sprite should replace the avatar")] string special = "", [Description("Any string except '-' if the sprite should be color-customizable")] string rainbow = "", [Description("Any string except '-' to set the sprite artist")] string artist = "")
        {
            PalantirContext dbcontext = new PalantirContext();

            if (!dbcontext.EventDrops.Any(e => e.EventDropId == eventDropID))
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

            System.Net.WebClient client = new System.Net.WebClient();

            // download sprite
            var id = dbcontext.Sprites.Where(s => s.Id < 1000).Max(s => s.Id) + 1;
            var spriteFileName = "evd" + eventDropID + "-" + name.Replace("'", "-").Replace(" ", "_") + "-" + id + ".gif";
            var tempSavePath = Path.Combine(Program.CacheDataPath, "sprite-sources", spriteFileName);
            client.DownloadFile(context.Message.Attachments[0].Url, tempSavePath);

            StaticData.AddFile(tempSavePath, "sprites/event", "add event sprite #" + id);

            Sprite eventsprite = new Sprite(
                name.Replace("_", " "),
                "https://static.typo.rip/sprites/event/" + spriteFileName,
                price,
                id,
                special != "-" && special != "",
                rainbow != "-" && rainbow != "",
                eventDropID,
                artist == "" || artist == "-" ? null : artist
            );
            BubbleWallet.AddSprite(eventsprite);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Sprite added to " + dbcontext.EventDrops.FirstOrDefault(e => e.EventDropId == eventDropID).Name + ": **" + eventsprite.Name + "**";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("ID: " + eventsprite.ID + "\nYou can buy and view the sprite with the usual comands.");
            embed.WithThumbnail(context.Message.Attachments[0].Url);

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Add a seasonal drop to an event")]
        [Command("eventdrop")]
        [RequirePermissionFlag((byte)4)]
        public async Task CreateEventDrop(CommandContext context, [Description("The id of the event for the event drop")] int eventID, [Description("The name of the event drop")] string name)
        {
            PalantirContext dbcontext = new PalantirContext();

            if (!dbcontext.Events.Any(e => e.EventId == eventID))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no event with that id.\nCheck `>upcoming`");
                return;
            }
            if (context.Message.Attachments.Count <= 0 || !context.Message.Attachments[0].FileName.EndsWith(".gif"))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no valid gif attached.");
                return;
            }

            EventDrop newDrop = new EventDrop();
            newDrop.EventId = eventID;
            newDrop.Name = name.Replace("_", " ");
            newDrop.Url = context.Message.Attachments[0].Url;
            if (dbcontext.EventDrops.Count() <= 0) newDrop.EventDropId = 0;
            else newDrop.EventDropId = dbcontext.EventDrops.Max(e => e.EventDropId) + 1;

            dbcontext.EventDrops.Add(newDrop);
            dbcontext.SaveChanges();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Drop added to " + dbcontext.Events.FirstOrDefault(e => e.EventId == eventID).EventName + ": **" + newDrop.Name + "**";
            embed.Color = DiscordColor.Magenta;
            embed.WithThumbnail(newDrop.Url);
            embed.WithDescription("The ID of the Drop is  " + newDrop.EventDropId + ".\nAdd a seasonal Sprite which can be bought with the event drops to make your event complete:\n" +
                "➜ `>eventsprite " + newDrop.EventDropId + " [name] [price]` with the sprite-gif attached.");

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Show the current Drop League season ranking")]
        [Command("league")]
        public async Task League(CommandContext context, int month = -1, int year = -1)
        {

            if (year == -1) year = DateTime.UtcNow.Year;
            if (month == -1) month = DateTime.UtcNow.Month;

            var season = new League(month.ToString(), year.ToString());
            var results = season.LeagueResults().OrderByDescending(l => l.Score).ToList();

            var embed = new DiscordEmbedBuilder()
                    .WithAuthor("Drop League")
                    .WithTitle("**" + season.seasonName + "** Season")
                    .WithColor(DiscordColor.Magenta)
                    .WithThumbnail("https://media.discordapp.net/attachments/910894527261327370/983025068214992948/challenge.gif")
                    .WithDescription("Drop Leagues are a monthly competition, where the very fastest catchers rank against each other.\n_ _\n" + results.Count + " participants in this season\n_ _ \n" + (season.IsActive() ? "Season ends <t:" + season.GetEndTimestamp() + ":R>\n_ _" : "Ended <t:" + season.GetEndTimestamp() + ">\n_ _"));

            void AddTop(MemberLeagueResult result, int rank, string emote)
            {
                var member = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(result.Login).Member1);
                embed.AddField(
                    emote + " _ _ " + member.UserName + "\n_ _  `#" + rank + " - " + result.Score + "dw`",
                    "> ***" + result.LeagueDrops.Count + "** League Drops*\n> ***" + result.AverageWeight + "%** avg.weight*\n> ***" + result.AverageTime + "ms** avg.time*\n> ***" + result.Streak.streakMax + "** max.streak*",
                    true
                );
            }

            if (results.Count() > 0) AddTop(results[0], 1, "<a:league_rnk1:987699431350632518>");
            if (results.Count() > 1) AddTop(results[1], 2, "<a:league_rnk2:987710613893566515>");
            if (results.Count() > 2) AddTop(results[2], 3, "<a:league_rnk3:987716889352470528>");

            string LowerText(List<MemberLeagueResult> results)
            {
                string content = "";
                foreach (var result in results)
                {
                    var member = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(result.Login).Member1);
                    content += "> " + member.UserName.Replace("`", "\\`") + " `" + result.Score + "dw / " + result.AverageWeight + "%`\n";
                }
                return content;
            }

            if (results.Count() > 3) embed.AddField("<a:league_rnk4:987723143982514207>_ _  _ _ `#4 - #7`", LowerText(results.GetRange(3, 4)));
            if (results.Count() > 7) embed.AddField("<a:league_rnk4:987723143982514207>_ _  _ _ `#8 - #10`", LowerText(results.GetRange(7, 3)));

            if (results.Count > 0)
            {
                var maxOverall = results.Max(r => r.Score);
                var overall = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(
                    Program.Feanor.GetMemberByLogin(results.Find(r => r.Score == maxOverall).Login).Member1
                );

                var maxWeight = results.Max(r => r.AverageWeight);
                var weight = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(
                    Program.Feanor.GetMemberByLogin(results.Find(r => r.AverageWeight == maxWeight).Login).Member1
                );

                var maxCount = results.Max(r => r.LeagueDrops.Count);
                var count = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(
                    Program.Feanor.GetMemberByLogin(results.Find(r => r.LeagueDrops.Count == maxCount).Login).Member1
                );

                var maxStreak = results.Max(r => r.Streak.streakMax);
                var streak = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(
                    Program.Feanor.GetMemberByLogin(results.Find(r => r.Streak.streakMax == maxStreak).Login).Member1
                );

                embed.AddField(
                    "_ _\n`⚔️` Category Leaders\n_ _  ",
                    "➜ **Overall:**\n> " + overall.UserName + ": `" + maxOverall + "dw`\n\n➜ **Average Weight:**\n> "
                        + weight.UserName + ": `" + maxWeight + "%`\n\n➜ **League Drops:**\n> " + count.UserName + ": `" + maxCount + " drops`"
                        + "\n\n➜ **League Drop Streak:**\n> " + streak.UserName + ": `" + maxStreak + " drops`",
                    true
                );
            }
            
            embed.AddField("\u200b ", "BTW: check your own rank with `>league rank`");
            await context.RespondAsync(embed);

        }

        [Description("Show the complete Drop League season ranking")]
        [Command("league-board")]
        public async Task Board(CommandContext context, [Description("Month of the league season, eg `11`")] int month = -1, [Description("Year of the league season, eg `2022`")] int year = -1)
        {

            if (year == -1) year = DateTime.UtcNow.Year;
            if (month == -1) month = DateTime.UtcNow.Month;

            var season = new League(month.ToString(), year.ToString());
            var results = season.LeagueResults().OrderByDescending(l => l.Score).ToList();

            string msg = "```\n";
            msg += " Leaderboard Drop League Season " + season.seasonName + "\n \n";
            msg += "｜Rank｜      Name     ｜ Score ｜Ø Weight｜Streak\n";

            string ranks = "";
            results.Batch(1).ForEach((batch, i) =>
            {
                var aBatch = batch.ToArray();
                var rank1 = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(aBatch[0].Login).Member1).UserName;
                ranks += $"｜#{ i + 1,3 }｜{ Regex.Replace(rank1, @"p{Cs}", ""),15 }｜{ aBatch[0].Score,6 } ｜{ aBatch[0].AverageWeight,6 }% ｜{ aBatch[0].Streak.streakMax,5 }";

                if (aBatch.Length > 1)
                {
                    var rank2 = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(aBatch[1].Login).Member1).UserName;
                    ranks += $"｜#{ i * 2 + 2,3 }｜{ Regex.Replace(rank1, @"p{Cs}", ""),15 }｜{ aBatch[1].Score,6 } ｜{ aBatch[1].AverageWeight,6 }% ｜{ aBatch[1].Streak.streakMax,5 } ｜\n";
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

            if (page.Length > 0) pages.Add(new DSharpPlus.Interactivity.Page(page + "\n```"));

            await Program.Interactivity.SendPaginatedMessageAsync(context.Channel, context.Message.Author, pages);
        }

        [Description("Show your current Drop League season ranking")]
        [Command("league-help")]
        public async Task LeagueHelp(CommandContext context)
        {
            var embed = new DiscordEmbedBuilder()
                   .WithAuthor("Drop League")
                   .WithTitle("**Help & Informations")
                   .WithColor(DiscordColor.Magenta)
                   .WithThumbnail("https://media.discordapp.net/attachments/910894527261327370/983025068214992948/challenge.gif")
                   .WithDescription("Drop Leagues are a monthly competition, where the very fastest catchers rank against each other.\n_ _\n");

            embed.AddField("_ _\n`📃` _ _ About ranking", "\n ➜ The **overall ranking leader** is the player with the most collected 'drop weight / `dw`'. Each League Drop you collect is weighted by how fast you catch it and adds to your score."
            + "\n_ _ \n ➜ The **average weight leader** is the player that has the highest average drop weight - this means, this player has the fastest average catch time!"
            + "\n_ _ \n ➜ The **league drops leader** is the player with the most total collected League Drops."
            + "\n_ _ \n ➜ The **maximum streak leader** is the player with the highest caught League Drop streak. Only League Drops count, otherwise the streak is broken."
            + "\n_ _ \n ➜ When you catch a League Drop, it is weighted (between 0.1 and 1) by how fast you were and added to your Drop/Bubble credit."
            + "\n_ _ \n ➜ When you catch a Event League Drop, it is also weighted. Using `>event` you can see your collected Event League Dropsfor an Event. You can trade them to any Eventdrop of this event!");


            embed.AddField("_ _ \n`🎖` _ _ Rewards", "➜ **Overall:**\n> Top 4: 5,4,3 Splits\n> Top 10: 2 Splits\n> Top 20: 1 Split\n\n➜ **League Drops Leaders: **\n> 3,2,1 Splits\n\n➜ **Streak Leaders: **\n> 3,2,1 Splits\n\n➜ **Overall Leader**: \n> Can't compete in categories, but gets 4 Splits if #1 in all categories");

            embed.AddField("_ _\n`🤖` _ _ Important commands", " ➜ `>league` To show the top ranking of the current season\n\n ➜ `>league-baord` to show the complete ranking of this season\n\n ➜ `>league-rank` to show your current statistics");

            await context.RespondAsync(embed);
        }


        [Description("Show your current Drop League season ranking")]
        [Command("league-rank")]
        public async Task Rank(CommandContext context, [Description("Month of the league season, eg `11`")] int month = -1, [Description("Year of the league season, eg `2022`")] int year = -1)
        {

            if (year == -1) year = DateTime.UtcNow.Year;
            if (month == -1) month = DateTime.UtcNow.Month;

            var season = new League(month.ToString(), year.ToString());
            var results = season.LeagueResults().OrderByDescending(l => l.Score).ToList();
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            int position = results.FindIndex(result => result.Login == login) + 1;

            if (position <= 0) await Program.SendEmbed(context.Channel, "Oopsie", "You aren't ranked in this season." + (season.IsActive() ? " Catch some drops faster than 1000ms to appear in the ranking!" : ""));
            else
            {
                var embed = new DiscordEmbedBuilder()
                .WithAuthor("Drop League")
                .WithTitle("**" + season.seasonName + "** Season")
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

                var maxStreak = results.Max(r => r.Streak.streakMax);
                var sortMaxStreak = results.OrderByDescending(results => results.Streak.streakMax);
                var selfMaxStreak = sortMaxStreak.ToList().IndexOf(results[position - 1]) + 1;
                if (results.Find(r => r.Streak.streakMax == maxStreak).Login == login)
                {
                    embed.AddField("<a:league_rnk1:987699431350632518>  _ _  Leader in the category `Maximum Streak`", "\u200b ");
                }

                embed.AddField(
                    "➜ _ _ Your Stats",
                    "> `" + results[position - 1].Score + "dw`\n> ***" + results[position - 1].LeagueDrops.Count
                    + "** League Drops (#" + position + ")*\n> ***" + results[position - 1].AverageWeight
                    + "%** avg.weight (#" + selfMaxAvg + ")*\n> ***" + results[position - 1].AverageTime
                    + "ms** avg.time *\n> ***" + results[position - 1].Streak.streakMax + "** max.streak (#" + selfMaxStreak + ", current streak: " + results[position - 1].Streak.streakEnd + ")*",
                    false
                );

                await context.RespondAsync(embed);
            }
        }


        [Description("Deprecated")]
        [Command("rnk")]
        public async Task RankWithoutHelp(CommandContext context)
        {
            await context.RespondAsync("The league commands got improved - probably you're looking for `>league-board`. \nAlso check out `>league`, `>league-rank` and `>league-help`");
        }

        [Description("Evaluates a league and rewards splits")]
        [Command("league-eval")]
        [RequirePermissionFlag(PermissionFlag.ADMIN)]
        public async Task EvalLeague(CommandContext context, int month, int year, bool apply = false)
        {
            var season = new League(month.ToString(), year.ToString());
            string text = "**Evaluation of League Season " + season.seasonName + "**\n\n\n";


            var rewards = season.Evaluate();

            
            List<SplitCredit> splits = new List<SplitCredit>();

            rewards.ForEach(reward =>
            {
                var name = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(reward.result.Login).Member1).UserName.Replace("`","");
                text += name + ": `" + reward.rewards.ToDelimitedString(", ") + " `=> " + reward.splits + " Splits\n";
            });

            await context.RespondAsync(text);

            if (apply)
            {
                PalantirContext db = new();
                int id = db.BoostSplits.Max(s => s.Id) + 1;

                BoostSplit leagueSplit = new BoostSplit()
                {
                    Date = "01/" + month.ToString().PadLeft(2, '0') + "/" + year.ToString(),
                    Id = id,
                    Description = "You have been ranked in the leaderboard of that season." ,
                    Value = 0,
                    Name = "<a:league_rnk1:987699431350632518> League " + season.seasonName
                };

                db.BoostSplits.Add(leagueSplit);

                rewards.ForEach((reward) =>
                {
                    db.SplitCredits.Add(new SplitCredit()
                    {
                        ValueOverride = reward.splits,
                        Login = Convert.ToInt32(reward.result.Login),
                        RewardDate = DateTime.UtcNow.ToShortDateString(),
                        Split = id,
                        Comment = reward.rewards.ToDelimitedString(", ")
                    });
                });

                db.SaveChanges();
                db.Dispose();

                await context.RespondAsync("Rewarded splits.");
            }
        }
    }
}

