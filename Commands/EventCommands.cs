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

