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
using Palantir.Model;
using System.IO;
using Palantir.PalantirCommandModule;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;

namespace Palantir.Commands
{
    public class EventCommands : PalantirCommandModule.PalantirCommandModule
    {
        [Description("Show event info")]
        [Command("event")]
        public async Task ShowEvent(CommandContext context, int eventID = 0)
        {
            await Program.SendNewPalantirInformation(context, ">event [id]");
            List<Event> events = Events.GetEvents(false);
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            List<SpriteProperty> inv = BubbleWallet.GetInventory(login);
            Event evt;
            if (eventID < 1 || !events.Any(e => e.EventId == eventID)) evt = (Events.GetEvents().Count > 0 ? Events.GetEvents()[0] : null);
            else evt = events.FirstOrDefault(e => e.EventId == eventID);
            if (evt != null)
            {
                DateTime eventStart = Program.ParseDateAsUtc(evt.ValidFrom);
                DateTime eventEnd = eventStart.AddDays(evt.DayLength);
                List<Model.Sprite> eventsprites = new List<Model.Sprite>();
                var progressiveDrops = Events.GetProgressiveEventDrops(evt);
                List<EventDrop> eventdrops = Events.GetEventDrops(new List<Event> { evt });
                Scene scene = BubbleWallet.GetAvailableScenes().FirstOrDefault(scene => scene.EventId == evt.EventId);
                var collected = Events.GetCollectedEventDrops(context.Message.Author.Id.ToString(), evt);

                // build pages of drop info
                List<string> dropInfoPages= new List<string>() { ""};
                eventdrops.ForEach(e =>
                {
                    string dropInfo = "";
                    var progressiveDropInfo = progressiveDrops.FirstOrDefault(d => d.drop.EventDropId == e.EventDropId);
                    List<Model.Sprite> sprites = Events.GetEventSprites(e.EventDropId);
                    eventsprites.AddRange(sprites);
                    dropInfo += "\n**" + e.Name + " Drop**  (" + BubbleWallet.GetEventCredit(login, e.EventDropId) + " caught) (`#" + e.EventDropId + "`)";

                    if(evt.Progressive == 1 && !progressiveDropInfo.isRevealed)
                    {
                        dropInfo += $"\n> Will be collectable from <t:{progressiveDropInfo.revealTimeStamp}:d> to <t:{progressiveDropInfo.endTimestamp}:d>\n";
                    }
                    else
                    {
                        int spent = inv.Where(spt => sprites.Any(eventsprite => eventsprite.Id == spt.ID)).Sum(spt => spt.Cost);
                        sprites.OrderBy(sprite => sprite.Id).ForEach(sprite =>
                           dropInfo += "\n> ‎ \n> ➜ **" + sprite.Name + "** (`#" + sprite.Id + "`)\n> "
                            + (inv.Any(s => s.ID == sprite.Id) ? ":package: " : (BubbleWallet.GetEventCredit(login, sprite.EventDropId) - spent + " / "))
                            + sprite.Cost + " " + e.Name + " Drops "
                        );
                        dropInfo += "\n";
                    }

                    if(dropInfoPages[dropInfoPages.Count - 1].Length + dropInfo.Length > 500) dropInfoPages.Add(dropInfo);
                    else dropInfoPages[dropInfoPages.Count - 1] +=  dropInfo;
                });

                var rate = Events.CurrentGiftLossRate(eventsprites, collected);
                Console.WriteLine($"Loss Collection Debug [event]: userid: {context.Message.Author.Id}, collected: {collected}, loss: {rate}, event: {evt.EventId}, eventsprites: {String.Join("", eventsprites.ConvertAll(s=>s.Id))}");

                // build embed pages
                var pages = dropInfoPages.ConvertAll(page =>
                {
                    var pageEmbed = new DiscordEmbedBuilder()
                        .WithTitle(":champagne: " + evt.EventName)
                        .WithDescription(evt.Description +
                            "\nLasts from <t:" + new DateTimeOffset(eventStart).ToUnixTimeSeconds() + ":D> until <t:"
                            + new DateTimeOffset(eventEnd).ToUnixTimeSeconds() + ":D>\n")
                        .WithColor(DiscordColor.Magenta)
                        .WithFooter(Math.Round(collected) + " Drops total collected ~ Current gift loss rate: " + Math.Round(rate, 3))
                        .AddField("Event Sprites", page.Length == 0 ? "No drops added yet." : page)
                        .AddField("\u200b", "Use `>sprite [id]` to see the event drop and sprite!");

                    return pageEmbed;
                });

                // progressive event info
                if (evt.Progressive == 1)
                {
                    pages.ForEach(page =>
                    {
                        page.AddField("Progressive event", "The eventdrops unveil sequentially during the event - you can only collect them on certain days!");
                    });
                }

                // league stuff: for regular events
                if(evt.Progressive == 0)
                {
                    List<PastDrop> leaguedrops = new List<PastDrop>();
                    var credit = Events.GetAvailableLeagueTradeDrops(context.User.Id.ToString(), evt, out leaguedrops);
                    if (credit > 0)
                    {
                        pages.ForEach(page =>
                        {
                            page.AddField("\n\u200b \nLeague Event Drops", "> You have " + Math.Round(credit, 1, MidpointRounding.ToZero).ToString() + " League Drops to redeem! \n> You can swap them with the command `>redeem [amount] [event drop id]` to any of this event's Event Drops.");
                        });
                    }
                }

                // league stuff: for progressive events
                if (evt.Progressive == 1)
                {
                    var credits = Events.GetAvailableProgressiveLeagueTradeDrops(context.User.Id.ToString(), evt, out var leaguedrops);
                    //if (credit > 0) embed.AddField("\n\u200b \nLeague Event Drops", "> You have " + Math.Round(credit, 1, MidpointRounding.ToZero).ToString() + " League Drops to redeem! \n> You can swap them with the command `>redeem [amount] [event drop id]` to any of this event's Event Drops.");
                    if(credits.Values.Any(v => v > 0))
                    {
                        var creditInfo = string.Join("\n", credits.Keys.ToList().ConvertAll(key => $"> - `#{key}` {eventdrops.FirstOrDefault(d=>d.EventDropId==key).Name}: {Math.Round(credits[key], 1, MidpointRounding.ToZero)}"));
                        pages.ForEach(page =>
                        {
                            page.AddField("\n\u200b \nLeague Event Drops", $"> You have following League Drops to redeem: \n{creditInfo}\n> \n> You can swap them with the command `>redeem [amount] [event drop id]`.");
                        });    
                    }
                }

                if (scene != null)
                {
                    int collectedBubbles = BubbleWallet.GetCollectedBubblesInTimespan(eventStart, eventEnd.AddDays(-1), login);
                    bool hasScene = BubbleWallet.GetSceneInventory(login, false, false).Any(prop => prop.Id == scene.Id);

                    pages.ForEach(page =>
                    {
                        page.WithImageUrl(scene.Url);
                        page.AddField("\n\u200b \nEvent Scene: **" + scene.Name + "**", "> \n> " + (hasScene ? ":package:" : "") + collectedBubbles + " / " + (((evt.EventId == 15 ? -2 : 0) + evt.DayLength) * Events.eventSceneDayValue) + " Bubbles collected");
                    });
                }

                var paginatablePages = pages.ConvertAll(page => new Page() { Embed = page });

                if (pages.Count > 1) await context.Client.GetInteractivity().SendPaginatedMessageAsync(context.Channel, context.User, paginatablePages);
                else await context.Channel.SendMessageAsync(embed: pages[0]);
            }
            else
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Title = ":champagne: No Event active :(";
                embed.Color = DiscordColor.Magenta;
                embed.WithDescription("Check new events with `>upcoming`.\nSee all past events with `>passed` and a specific with `>event [id]`.\nGift event drops with `>gift [@person] [amount of drops] [id of the sprite]`.\nBtw - I keep up to 50% of the gift for myself! ;)");

                await context.Channel.SendMessageAsync(embed: embed);
            }
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

    }
}

