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
                    dropList += "\n**" + e.Name + "**  (" + BubbleWallet.GetEventCredit(login, e.EventDropID) + " caught)";
                    int spent = inv.Where(spt => sprites.Any(eventsprite => eventsprite.ID == spt.ID)).Sum(spt => spt.Cost);
                    sprites.OrderBy(sprite => sprite.ID).ForEach(sprite =>
                       dropList += "\n> ‎ \n> ➜ **" + sprite.Name + "** (#" + sprite.ID + ")\n> "
                        + (inv.Any(s => s.ID == sprite.ID) ? ":package: " : (BubbleWallet.GetEventCredit(login, sprite.EventDropID) - spent + " / "))
                        + sprite.Cost + " " + e.Name + " Drops "
                    );
                    dropList += "\n";
                });
                embed.AddField("Event Sprites", dropList == "" ? "No drops added yet." : dropList);
                embed.AddField("\u200b", "Use `>sprite [id]` to see the event drop and sprite!");

                SceneEntity scene = BubbleWallet.GetAvailableScenes().FirstOrDefault(scene => scene.EventID == evt.EventID);
                if(scene != null)
                {
                    int collectedBubbles = BubbleWallet.GetCollectedBubblesInTimespan(eventStart, eventEnd.AddDays(-1), login);
                    bool hasScene = BubbleWallet.GetSceneInventory(login, false, false).Any(prop => prop.ID == scene.ID);
                    embed.WithImageUrl(scene.URL);
                    embed.AddField("\n\u200b \nEvent Scene: **" + scene.Name + "**", "> \n> " + (hasScene ? ":package:" : "") + collectedBubbles + " / " + (evt.DayLength * Events.eventSceneDayValue) + " Bubbles collected");
                }
            }
            else
            {
                embed.Title = ":champagne: No Event active :(";
                embed.Color = DiscordColor.Magenta;
                embed.WithDescription("Check new events with `>upcoming`.\nSee all past events with `>passed` and a specific with `>event [id]`.\nGift event drops with `>gift [@person] [amount of drops] [id of the sprite]`.\nBtw - I keep up to 50% of the gift for myself! ;)");
            }

            await context.Channel.SendMessageAsync(embed: embed);
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
                eventsList += e.Description + "\n\n";
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
        [RequireBeta()]
        public async Task League(CommandContext context)
        {

           var season = new League(DateTime.Now.Month.ToString(), DateTime.Now.Year.ToString());
           var results = season.LeagueResults().OrderByDescending(l=>l.Score).ToList();

            var embed = new DiscordEmbedBuilder()
                 .WithAuthor("Drop League")
                 .WithTitle("**" + DateTime.Now.ToString("MMMM yyyy") + "** Season")
                 .WithColor(DiscordColor.Magenta)
                 .WithThumbnail("https://media.discordapp.net/attachments/910894527261327370/983025068214992948/challenge.gif")
                 .WithDescription("Drop Leagues are a monthly competition, where the very fastest catchers rank against each other.\n_ _\nSeason ends `in 20 days`\n_ _");

            void AddTop(League.MemberLeagueResult result, int rank)
            {
                var member = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(result.Login).Member);
                embed.AddField(
                    "`#"+rank+"`  **" + member.UserName + "**",
                    "> `" + result.Score + "dw`\n> ***" + result.LeagueDrops.Count + "** League Drops*\n> ***" + result.AverageWeight + "%** avg.weight*\n> ***" + result.AverageTime + "ms** avg.time*",
                    true
                );
            }

            if (results.Count() > 0) AddTop(results[0],1);
            if (results.Count() > 1) AddTop(results[1],2);
            if (results.Count() > 2) AddTop(results[2],3);

            string LowerText(List<League.MemberLeagueResult> results)
            {
                string content = "";
                foreach(var result in results)
                {
                    var member = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(result.Login).Member);
                    content += "> " + member.UserName + " `" + result.Score + "dw / " + result.AverageWeight + "%`\n";
                }
                return content;
            }

            if (results.Count() > 3) embed.AddField("`#4 - #7`", LowerText(results.GetRange(3,4)));
            if (results.Count() > 7) embed.AddField("`#8 - #10`", LowerText(results.GetRange(7, 3)));

            if(results.Count > 0)
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

                embed.AddField(
                    "_ _\n`⚔️` Category Leaders",
                    "➜ **Overall**: " + overall.UserName + " (`" + maxOverall + "dw`)\n➜ **Average Weight**: " 
                        + weight.UserName + " (`" + maxWeight + "%`)\n➜ **League Drops**: " + count.UserName + " (`" + maxCount + " drops`)",
                    true
                );
            }

            embed.AddField("_ _ \n`🎖️` Rewards", "➜ **Overall:** #1 : 4 Splits, #2-3: 3 Splits,  #4-10: 2 Splits\n➜ **Weight Leader: **3 Splits\n➜ **League Drops Leader: **3 Splits");

            await context.RespondAsync(embed);

        }
    }
}
