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

namespace Palantir.Commands
{
    public class SpriteCommands : BaseCommandModule
    {

        [Description("Get a list of all sprites in the store.")]
        [Command("sprites")]
        [Aliases("spt", "sprite")]
        public async Task Sprites(CommandContext context, [Description("The id of the sprite (eg '15')")] int sprite = 0)
        {
            List<Sprite> sprites = BubbleWallet.GetAvailableSprites();

            if (sprites.Any(s => s.ID == sprite))
            {
                Sprite s = BubbleWallet.GetSpriteByID(sprite);
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Color = DiscordColor.Magenta;
                embed.Title = s.Name + (s.EventDropID > 0 ? " (Event Sprite)" : "");
                embed.ImageUrl = s.URL;
                embed.Description = "";
                if (!string.IsNullOrEmpty(s.Artist))
                {
                    embed.Description += "**Artist:** " + s.Artist + " \n";
                }
                if (s.EventDropID == 0)
                {
                    embed.Description += "**Costs:** " + s.Cost + " Bubbles\n\n**ID**: " + s.ID + (s.Special ? " :sparkles: " : "");
                }
                else
                {
                    EventDropEntity drop = Events.GetEventDrops().FirstOrDefault(d => d.EventDropID == s.EventDropID);
                    embed.Description += "**Event Drop Price:** " + s.Cost + " " + drop.Name + "\n**ID**: " + s.ID + (s.Special ? " :sparkles: " : "");
                    embed.WithThumbnail(drop.URL);
                }
                int[] score = BubbleWallet.SpriteScoreboard().FirstOrDefault(score => score.Key == s.ID).Value;
                embed.WithFooter("Bought: " + (score[2] + score[1]) + " | Active: " + score[1]);
                embed.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)\n[Try out the sprite](https://tobeh.host/Orthanc/sprites/cabin/?sprite=" + sprite + ")");
                await context.Channel.SendMessageAsync(embed: embed);
            }
            else
            {
                DiscordEmbedBuilder list = new DiscordEmbedBuilder();
                list.Color = DiscordColor.Magenta;
                list.Title = "🔮 Top 10 Popular Sprites";
                list.Description = "Show one of the available Sprites with `>sprites [id]`";
                Dictionary<int, int[]> spriteScores = BubbleWallet.SpriteScoreboard().Slice(0, 10).ToDictionary();
                int rank = 1;
                spriteScores.ForEach(score =>
                {
                    Sprite spt = sprites.First(sprite => sprite.ID == score.Key);
                    list.AddField("**#" + rank + ": " + spt.Name + "** ", "ID: " + spt.ID + (spt.Special ? " :sparkles: " : "") + " - Active: " + score.Value[1] + ", Bought: " + (score.Value[2] + score.Value[1]));
                    rank++;
                });
                list.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)");
                await context.Channel.SendMessageAsync(embed: list);
            }
        }

        [Description("Buy a sprite.")]
        [Command("buy")]
        public async Task Buy(CommandContext context, [Description("The id of the sprite (eg '15')")] int sprite)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            MemberEntity member = Program.Feanor.GetMemberByLogin(login);
            List<SpriteProperty> inventory;
            try
            {
                inventory = BubbleWallet.GetInventory(login);
            }
            catch (Exception e)
            {
                await Program.SendEmbed(context.Channel, "Error executing command", e.ToString());
                return;
            }
            List<Sprite> available = BubbleWallet.GetAvailableSprites();

            if (inventory.Any(s => s.ID == sprite))
            {
                await Program.SendEmbed(context.Channel, "Woah!!", "Bubbles are precious. \nDon't pay for something you already own!");
                return;
            }

            if (!available.Any(s => s.ID == sprite))
            {
                await Program.SendEmbed(context.Channel, "Eh...?", "Can't find that sprite. \nChoose another one or keep your bubbles.");
                return;
            }

            Sprite target = available.FirstOrDefault(s => s.ID == sprite);
            int credit = BubbleWallet.CalculateCredit(login, context.User.Id.ToString());
            PermissionFlag perm = new PermissionFlag((byte)member.Flag);
            if (target.ID == 1003)
            {
                if (!perm.Patron)
                {
                    await Program.SendEmbed(context.Channel, "Haha, nice try -.-", "This sprite is exclusive for patrons!");
                    return;
                }
            }
            else if (target.EventDropID == 0)
            {
                if (credit < target.Cost && !perm.BotAdmin)
                {
                    await Program.SendEmbed(context.Channel, "Haha, nice try -.-", "That stuff is too expensive for you. \nSpend few more hours on skribbl.");
                    return;
                }
            }
            else
            {
                if (BubbleWallet.GetRemainingEventDrops(login, target.EventDropID) < target.Cost && !perm.BotAdmin)
                {
                    await Program.SendEmbed(context.Channel, "Haha, nice try -.-", "That stuff is too expensive for you. \nSpend few more hours on skribbl.");
                    return;
                }
            }

            inventory.Add(new SpriteProperty(target.Name, target.URL, target.Cost, target.ID, target.Special, target.Rainbow, target.EventDropID, target.Artist, false, -1));
            BubbleWallet.SetInventory(inventory, login);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Whee!";
            embed.Description = "You unlocked **" + target.Name + "**!\nActivate it with `>use " + target.ID + "`";
            embed.Color = DiscordColor.Magenta;
            embed.ImageUrl = target.URL;
            await context.Channel.SendMessageAsync(embed: embed);

            return;
        }

        [Description("Choose your sprite.")]
        [Command("use")]
        public async Task Use(CommandContext context, [Description("The id of the sprite (eg '15')")] int sprite, [Description("The sprite-slot which will be set. Starts at slot 1.")] int slot = 1, [Description("A timeout in seconds when the action will be performed")] int timeoutSeconds = 0)
        {
            if (timeoutSeconds > 0)
            {
                await Program.SendEmbed(context.Channel, "Tick tock...", "The command will be executed in " + timeoutSeconds + "s.", "", DiscordColor.Green.Value);
                await Task.Delay(timeoutSeconds * 1000);
            }
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            List<SpriteProperty> inventory;
            inventory = BubbleWallet.GetInventory(login);
            if (sprite != 0 && !inventory.Any(s => s.ID == sprite))
            {
                await Program.SendEmbed(context.Channel, "Hold on!", "You don't own that. \nGet it first with `>buy " + sprite + "`.");
                return;
            }

            MemberEntity member = Program.Feanor.GetMemberByLogin(login);
            PermissionFlag perm = new PermissionFlag((byte)member.Flag);

            if (!perm.BotAdmin && (slot < 1 || slot > BubbleWallet.GetDrops(login, context.User.Id.ToString()) / 1000 + 1 + (perm.Patron ? 1 : 0)))
            {
                await Program.SendEmbed(context.Channel, "Out of your league.", "You can't use that sprite slot!\nFor each thousand collected drops, you get one extra slot.");
                return;
            }

            if (sprite == 0)
            {
                await Program.SendEmbed(context.Channel, "Minimalist, huh? Your sprite was disabled.", "");
                inventory.ForEach(i =>
                {
                    if (i.Slot == slot) i.Activated = false;
                });
                BubbleWallet.SetInventory(inventory, login);
                return;
            }

            if (BubbleWallet.GetSpriteByID(sprite).Special && inventory.Any(active => active.Activated && active.Special && active.Slot != slot))
            {
                await Program.SendEmbed(context.Channel, "Too overpowered!!", "Only one of your sprite slots may have a special sprite.");
                return;
            }

            inventory.ForEach(i =>
            {
                if (i.ID == sprite && i.Activated) i.Slot = slot; // if sprite is already activated, activate on other slot
                else if (i.ID == sprite && !i.Activated) { i.Activated = true; i.Slot = slot; } // if sprite is not activated, activate on slot
                else if (!(i.Activated && i.ID != sprite && i.Slot != slot)) { i.Activated = false; i.Slot = -1; }
                // if sprite ist not desired not activated on slot deactivate
            });
            BubbleWallet.SetInventory(inventory, login);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Your fancy sprite on slot " + slot + " was set to **`" + BubbleWallet.GetSpriteByID(sprite).Name + "`**";
            embed.ImageUrl = BubbleWallet.GetSpriteByID(sprite).URL;
            embed.Color = DiscordColor.Magenta;
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Activate sprite slot combo.")]
        [Command("combo")]
        public async Task Combo(CommandContext context, [Description("The id of the sprites (eg '15 0 16 17')")] params int[] sprites)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            List<SpriteProperty> inventory = BubbleWallet.GetInventory(login);
            if (sprites.Any(sprite => sprite != 0 && !inventory.Any(item => item.ID == sprite)))
            {
                await Program.SendEmbed(context.Channel, "Gonna stop you right there.", "You don't own all sprites from this combo.");
                return;
            }

            MemberEntity member = Program.Feanor.GetMemberByLogin(login);
            PermissionFlag perm = new PermissionFlag((byte)member.Flag);

            if (!perm.BotAdmin && (sprites.Length < 1 || sprites.Length > BubbleWallet.GetDrops(login, context.User.Id.ToString()) / 1000 + 1 + (perm.Patron ? 1 : 0)))
            {
                await Program.SendEmbed(context.Channel, "Gotcha!", "You can't use that many sprite slots!\nFor each thousand collected drops, you get one extra slot.");
                return;
            }
            if (sprites.Where(sprite => !(BubbleWallet.GetSpriteByID(sprite) is null) && BubbleWallet.GetSpriteByID(sprite).Special).Count() > 1)
            {
                await Program.SendEmbed(context.Channel, "Too overpowered!!", "Only one of your sprite slots may have a special sprite.");
                return;
            }

            inventory.ForEach(item =>
            {
                item.Activated = false;
                item.Slot = 0;
            });
            List<int> slots = sprites.ToList();
            slots.ForEach(slot =>
            {
                if (slot > 0)
                {
                    inventory.Find(item => item.ID == slot).Activated = true;
                    inventory.Find(item => item.ID == slot).Slot = slots.IndexOf(slot) + 1;
                }
            });
            BubbleWallet.SetInventory(inventory, login);

            sprites = sprites.Where(id => id > 0).ToArray();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Your epic sprite combo was activated!";
            embed.Color = DiscordColor.Magenta;
            if (sprites.Length > 0)
            {
                string path = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(sprites), "/home/pi/Webroot/files/combos/")
                    .Replace(@"/home/pi/Webroot/", "https://tobeh.host/");
                embed.ImageUrl = path;
            }
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("View a scene")]
        [Command("scene")]
        public async Task ViewScene(CommandContext context, [Description("The ID of the scene")] int id)
        {
            PalantirDbContext db = new PalantirDbContext();
            SceneEntity scene = db.Scenes.FirstOrDefault(scene => scene.ID == id);
            if (scene is not null)
            {
                List<SceneProperty> inventory = BubbleWallet.GetSceneInventory(BubbleWallet.GetLoginOfMember(context.User.Id.ToString()), false, false);
                int sceneCost = BubbleWallet.SceneStartPrice;
                inventory.Where(s => s.EventID == 0).ForEach(scene => sceneCost *= BubbleWallet.ScenePriceFactor);

                if (scene.Color.IndexOf("!") > 0) scene.Color = scene.Color.Substring(0, scene.Color.IndexOf("!"));
                if (scene.GuessedColor.IndexOf("!") > 0) scene.GuessedColor = scene.GuessedColor.Substring(0, scene.GuessedColor.IndexOf("!"));

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Title = "**" + scene.Name + "**";
                embed.Color = DiscordColor.Magenta;
                embed.AddField("Costs:", scene.EventID > 0 ? "This is an event scene - check `>event " + scene.EventID + "`" : "Your current scene price is **" + sceneCost + "** bubbles.");
                embed.WithDescription("**ID:** " + scene.ID + "\n" + (scene.Artist != "" ? "**Artist:** " + scene.Artist + "\n" : "") + "**Font color: **" + scene.Color + " / " + scene.GuessedColor + "\n\nBuy the scene: `>paint " + id + "`\nUse the scene: `>show " + id + "`");
                embed.WithImageUrl(scene.URL);
                await context.Channel.SendMessageAsync(embed: embed);
            }
            else
            {
                await Program.SendEmbed(context.Channel, "That's no scene :(", "Nothing found for the scene ID " + id);
            }
            db.Dispose();
        }

        [Description("Buy a scene")]
        [Command("paint")]
        public async Task BuyScene(CommandContext context, [Description("The ID of the scene")] int id)
        {
            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
            PermissionFlag flags = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            List<SceneEntity> available = BubbleWallet.GetAvailableScenes();
            List<SceneProperty> inventory = BubbleWallet.GetSceneInventory(login, false, false);
            int credit = flags.BotAdmin ? int.MaxValue : BubbleWallet.CalculateCredit(login, context.User.Id.ToString());
            int sceneCost = BubbleWallet.SceneStartPrice;
            inventory.Where(s => s.EventID == 0).ForEach(scene => sceneCost *= BubbleWallet.ScenePriceFactor);
            int eventID = available.FirstOrDefault(scene => scene.ID == id).EventID;

            if (!available.Any(scene => scene.ID == id))
            {
                await Program.SendEmbed(context.Channel, "Uhm..", "I don't know that scene :(\nCheck the ID!");
            }
            else if (inventory.Any(scene => scene.ID == id))
            {
                await Program.SendEmbed(context.Channel, "Waiiit :o", "You already own this scene!");
            }
            else if (eventID == 0 && credit < sceneCost)
            {
                await Program.SendEmbed(context.Channel, "I see you 👀", "You need at least " + sceneCost + " to buy a scene!\nScene cost increases by *2 with every scene you buy.");
            }
            else if (eventID > 0 && !Events.EligibleForEventScene(login, eventID))
            {
                await Program.SendEmbed(context.Channel, "Sorryyy,", "You have not (yet) collected enough bubbles during that event to buy this scene.\nCheck `>event " + eventID + "` to view your event bubbles.");
            }
            else
            {
                BubbleWallet.BuyScene(login, id);
                SceneEntity scene = available.FirstOrDefault(scene => scene.ID == id);

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Title = "Hypeeee!";
                embed.Color = DiscordColor.Magenta;
                embed.WithDescription("You unlocked ** " + scene.Name + "**!\n" + "Use it with: `>show " + id + "`");
                embed.WithImageUrl(scene.URL);
                await context.Channel.SendMessageAsync(embed: embed);
            }
        }

        [Description("Use a scene")]
        [Command("show")]
        public async Task UseScene(CommandContext context, [Description("The ID of the scene")] int id)
        {
            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
            PermissionFlag flags = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            List<SceneProperty> inventory = BubbleWallet.GetSceneInventory(login, false, false);

            if (!inventory.Any(scene => scene.ID == id) && id != 0)
            {
                await Program.SendEmbed(context.Channel, "Yeet!", "You don't own that scene - yet!");
            }
            else
            {
                inventory.ForEach(scene => scene.Activated = scene.ID == id);
                BubbleWallet.SetSceneInventory(login, inventory);

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                SceneProperty scene = inventory.FirstOrDefault(scene => scene.ID == id);
                embed.Title = id == 0 ? "Ok then" : "So pretty!";
                embed.Color = DiscordColor.Magenta;
                embed.WithDescription(id == 0 ? "Unset your skribbl scene." : "Your skribbl scene is now ** " + scene.Name + "**!");
                if (id != 0) embed.WithImageUrl(scene.URL);
                await context.Channel.SendMessageAsync(embed: embed);
            }
        }

        [Description("Add a sprite")]
        [Command("addsprite")]
        [RequirePermissionFlag((byte)4)] // 4 -> mod
        public async Task AddSprite(CommandContext context, [Description("The name of the sprite")] string name, [Description("The bubble price")] int price, [Description("Any string except '-' if the sprite should replace the avatar")] string special = "", [Description("Any string except '-' if the sprite should be color-customizable")] string rainbow = "", [Description("Any string except '-' to set the sprite artist")] string artist = "")
        {
            PalantirDbContext dbcontext = new PalantirDbContext();
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
            client.DownloadFile(context.Message.Attachments[0].Url, "/home/pi/Webroot/regsprites/spt" + name.Replace("'", "-") + ".gif");

            Sprite sprite = new Sprite(
                name.Replace("_", " "),
                "https://tobeh.host/regsprites/spt" + name.Replace("'", "-") + ".gif",
                price,
                dbcontext.Sprites.Where(s => s.ID < 1000).Max(s => s.ID) + 1,
                special != "-" && special != "",
                rainbow != "-" && rainbow != "",
                0,
                (artist == "" || artist == "-") ? null : artist
            );
            BubbleWallet.AddSprite(sprite);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Sprite **" + name + "** with ID " + sprite.ID + " was added!";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("ID: " + sprite.ID + "\nYou can buy and view the sprite with the usual comands.");
            embed.WithThumbnail(sprite.URL);

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Add a scene")]
        [Command("addscene")]
        [RequirePermissionFlag((byte)2)] // 2 -> admin
        public async Task AddScene(CommandContext context, [Description("The name of the scene")] string name, [Description("A color string (hex, rgb, name..)")] string color, [Description("A color when the player has guessed the word")] string guessedColor, [Description("Any string except '-' to set the sprite artist")] string artist = "", [Description("Event ID or '0' to associate to no event")] int eventID = 0)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.Moderator && !perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Ts ts...", "This command is only available for higher beings.\n||Some call them Bot-Moderators ;))||");
                return;
            }

            PalantirDbContext dbcontext = new PalantirDbContext();
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
            if (eventID != 0 && !Events.GetEvents(false).Any(evt => evt.EventID == eventID))
            {
                eventID = 0;
            }

            // download scene
            System.Net.WebClient client = new System.Net.WebClient();
            client.DownloadFile(context.Message.Attachments[0].Url, "/home/pi/Webroot/scenes/scene" + name.Replace("'", "-") + ".gif");

            string url = "https://tobeh.host/scenes/scene" + name.Replace("'", "-") + ".gif";
            if (artist == "-") artist = "";
            SceneEntity scene = BubbleWallet.AddScene(name.Replace("_", " "), color, guessedColor, artist, url, eventID);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Scene **" + name + "** with ID " + scene.ID + " was added" + (eventID > 0 ? " to event #" + eventID : "") + "!";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("ID: " + scene.ID + "\nView the scene with `>scene [scene id]`\nBuy the scene with `>paint [scene id]`\nUse a scene with `>show [scene id]`");
            embed.WithThumbnail(scene.URL);

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Set the color of a rainbow sprite.")]
        [Command("rainbow")]
        [RequireBeta()]
        public async Task Rainbow(CommandContext context, [Description("The id of the sprite (eg '15')")] int sprite, [Description("The rainbow shift from 0-360. 0 to remove it.")] int shift = 0)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            List<SpriteProperty> inventory;
            inventory = BubbleWallet.GetInventory(login);
            if (sprite != 0 && !inventory.Any(s => s.ID == sprite))
            {
                await Program.SendEmbed(context.Channel, "Hold on!", "You don't own that sprite. \nGet it first with `>buy " + sprite + "`.");
                return;
            }

            if(!inventory.Find(s => s.ID == sprite).Rainbow)
            {
                await Program.SendEmbed(context.Channel, "Hold on!", "That's not a rainbow sprite :(");
                return;
            }

            MemberEntity member = Program.Feanor.GetMemberByLogin(login);
            PermissionFlag perm = new PermissionFlag((byte)member.Flag);

            var shifts = BubbleWallet.GetMemberRainbowShifts(login);

            if (!(perm.BotAdmin || perm.Patron))
            {
                shifts = new();
            }

            shifts[sprite] = shift;

            string desc = shifts.Keys.ToList().Select(key => "**#" + key.ToString() + "*: " + shifts[key].ToString()).ToDelimitedString("\n");

            BubbleWallet.SetMemberRainbowShifts(login, shifts);

            await Program.SendEmbed(context.Channel, "Nice choice :}", "Your sprite will now be color-customized. Try it on!\n\nYour current choices are:\n" + desc + (perm.BotAdmin || perm.Patron ? "" : "\n\nYour previous color customizations were cleared. Become a Patron to set multiple at once!"));
        }
    }
}
