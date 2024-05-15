using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Linq;
using MoreLinq.Extensions;
using Newtonsoft.Json;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System.Globalization;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using System.Collections.Generic;
using Palantir.Model;
using System.IO;
using Palantir.PalantirCommandModule;

namespace Palantir.Commands
{
    public class SpriteCommands : PalantirCommandModule.PalantirCommandModule
    {

        

        [Description("Add a sprite")]
        [Command("addsprite")]
        [RequirePermissionFlag((byte)4)] // 4 -> mod
        public async Task AddSprite(CommandContext context, [Description("The name of the sprite")] string name, [Description("The bubble price")] int price, [Description("Any string except '-' if the sprite should replace the avatar")] string special = "", [Description("Any string except '-' if the sprite should be color-customizable")] string rainbow = "", [Description("Any string except '-' to set the sprite artist")] string artist = "")
        {
            PalantirContext dbcontext = new PalantirContext();
            if (context.Message.Attachments.Count <= 0 || !context.Message.Attachments[0].FileName.EndsWith(".gif"))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no valid gif attached.");
                return;
            }
            if (price < 500)
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
            var id = dbcontext.Sprites.Where(s => s.Id < 1000).Max(s => s.Id) + 1;
            var spriteFileName = "spt" + name.Replace("'", "-").Replace(" ", "_") + "-" + id + ".gif";
            var tempSavePath = Path.Combine(Program.CacheDataPath, "sprite-sources", spriteFileName);
            client.DownloadFile(context.Message.Attachments[0].Url, tempSavePath);

            StaticData.AddFile(tempSavePath, "sprites/regular", "add sprite #" + id);

            Sprite sprite = new Sprite(
                name.Replace("_", " "),
                "https://static.typo.rip/sprites/regular/" + spriteFileName,
                price,
                id,
                special != "-" && special != "",
                rainbow != "-" && rainbow != "",
                0,
                (artist == "" || artist == "-") ? null : artist
            );
            BubbleWallet.AddSprite(sprite);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Sprite **" + sprite.Name + "** with ID " + sprite.ID + " was added!";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("ID: " + sprite.ID + "\nYou can buy and view the sprite with the usual comands.");
            embed.WithThumbnail(context.Message.Attachments[0].Url);

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Add a scene")]
        [Command("addscene")]
        [RequirePermissionFlag((byte)2)] // 2 -> admin
        public async Task AddScene(CommandContext context, [Description("The name of the scene")] string name, [Description("A color string (hex, rgb, name..)")] string color, [Description("A color when the player has guessed the word")] string guessedColor, [Description("Any string except '-' to set the sprite artist")] string artist = "", [Description("Event ID or '0' to associate to no event")] int eventID = 0, [Description("If the scene can be bought or only obtained by another way")] bool exclusive = false)
        {
            PermissionFlag perm = new PermissionFlag(Program.Feanor.GetFlagByMemberId(context.User.Id.ToString()));
            if (!perm.Moderator && !perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Ts ts...", "This command is only available for higher beings.\n||Some call them Bot-Moderators ;))||");
                return;
            }

            PalantirContext dbcontext = new PalantirContext();
            if (context.Message.Attachments.Count <= 0 || !context.Message.Attachments[0].FileName.EndsWith(".gif"))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no valid scene image attached. Scenes need to be a gif.");
                return;
            }
            if (String.IsNullOrWhiteSpace(name))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "Something went wrong with the scene name.");
                return;
            }
            if (eventID != 0 && !Events.GetEvents(false).Any(evt => evt.EventId == eventID))
            {
                eventID = 0;
            }

            // download scene
            var id = BubbleWallet.NextSceneId();
            System.Net.WebClient client = new System.Net.WebClient();
            var sceneFileName = "scene" + name.Replace("'", "-") + "-" + id + ".gif";
            var tempSavePath = Path.Combine(Program.CacheDataPath, "scene-sources", sceneFileName);
            client.DownloadFile(context.Message.Attachments[0].Url, tempSavePath);

            StaticData.AddFile(tempSavePath, "scenes", "add scene #" + id);

            string url = "https://static.typo.rip/scenes/" + sceneFileName;
            if (artist == "-") artist = "";
            Scene scene = BubbleWallet.AddScene(name.Replace("_", " "), color, guessedColor, artist, url, eventID, exclusive);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Scene **" + name + "** with ID " + scene.Id + " was added" + (eventID > 0 ? " to event #" + eventID : "") + "!";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("ID: " + scene.Id + "\nView the scene with `>scene [scene id]`\nBuy the scene with `>paint [scene id]`\nUse a scene with `>show [scene id]`");
            embed.WithThumbnail(context.Message.Attachments[0].Url);

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }
    }
}
