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
using System.Net.Http;

namespace Palantir.Commands
{
    public class PatronCommands : BaseCommandModule
    {
        [Description("Generates a card of your profile")]
        [Command("card")]
        [RequirePermissionFlag(PermissionFlag.PATRON)]
        public async Task Card(CommandContext context)
        {
            DiscordMember dMember = context.Member;
            DiscordUser dUser = context.User;
            CustomCard cardsettings = new CustomCard
            {
                BackgroundImage = "-",
                BackgroundOpacity = 0.7,
                HeaderColor = "black",
                DarkTextColor = "white",
                LightTextColor = "white",
                HeaderOpacity = 1
            };
            // if other user is referenced for called card, set user
            if (context.Message.ReferencedMessage is not null)
            {
                dUser = context.Message.ReferencedMessage.Author;
                dMember = null;
            }

            PermissionFlag perm = new PermissionFlag(Program.Feanor.GetFlagByMember(dUser));
            string login = BubbleWallet.GetLoginOfMember(dUser.Id.ToString());

            // if target user is patron, load color scheme
            if (perm.Patron || perm.BotAdmin)
            {
                PalantirContext db = new PalantirContext();
                try
                {
                    CustomCard preferences = JsonConvert.DeserializeObject<CustomCard>(db.Members.FirstOrDefault(member => member.Login.ToString() == login).Customcard);
                    cardsettings = preferences;
                }
                catch { }
                db.Dispose();
            }

            DiscordMessage response = await context.RespondAsync(">  \n>  \n>   <a:working:857610439588053023> **Building your card afap!!**\n> _ _ \n> _ _ ");
            Model.Member member = Program.Feanor.GetMemberByLogin(login);
            Member memberDetail = JsonConvert.DeserializeObject<Member>(member.Member1);

            string content = Palantir.Properties.Resources.SVGcardBG;

            int[] sprites = BubbleWallet.GetInventory(login).Where(spt => spt.Activated).OrderBy(spt => spt.Slot).Select(spt => spt.ID).ToArray();

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Palantir#8352_by_tobeh#7437");
            string profilebase64 = Convert.ToBase64String(await client.GetByteArrayAsync(dUser.AvatarUrl));
            double bgheight = 0;
            string background64 = "";

            if (cardsettings.BackgroundImage != "-")
            {
                string bgPath = Program.CacheDataPath + "card-assets/imgur_" + cardsettings.BackgroundImage + ".bgb";
                if (!System.IO.File.Exists(bgPath))
                {
                    byte[] bgbytes = await client.GetByteArrayAsync("https://i.imgur.com/" + (cardsettings.BackgroundImage != "" && cardsettings.BackgroundImage != "-" ? cardsettings.BackgroundImage : "qFmcbT0.png"));
                    System.IO.File.WriteAllBytes(bgPath, bgbytes);
                }
                Image bg = Image.Load(Program.CacheDataPath + "card-assets/imgur_" + cardsettings.BackgroundImage + ".bgb");
                const double cardRatio = 489.98 / 328.09;
                SpriteComboImage.GetCropPosition(bg.Width, bg.Height, cardRatio, out double cropX, out double cropY, out double height, out double width);
                bg.Mutate(img => img.Crop(new Rectangle((int)cropX, (int)cropY, (int)width, (int)height)));
                background64 = bg.ToBase64String(SixLabors.ImageSharp.Formats.Png.PngFormat.Instance).Replace("data:image/png;base64,", "");
                bgheight = 328;
            }

            string combopath = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(sprites, BubbleWallet.GetMemberRainbowShifts(member.Login.ToString())));
            string spritebase64 = Convert.ToBase64String(System.IO.File.ReadAllBytes(combopath));
            System.IO.File.Delete(combopath);

            int caughtEventdrops = BubbleWallet.CaughtEventdrops(dUser.Id.ToString());
            int caughtleagueEventdrops = Convert.ToInt32(League.GetLeagueEventDropWeights(dUser.Id.ToString()).Sum());
            int caughtleagueDrops = League.CalcLeagueDropsValue(League.GetLeagueDropWeights(dUser.Id.ToString()));
            double ratio = Math.Round(((double)member.Drops + caughtEventdrops + caughtleagueEventdrops + caughtleagueDrops) / ((double)member.Bubbles / 1000), 1);
            if (!double.IsFinite(ratio)) ratio = 0;

            SpriteComboImage.FillPlaceholdersBG(ref content, profilebase64, spritebase64, background64, cardsettings.BackgroundOpacity, cardsettings.HeaderOpacity, bgheight.ToString(), cardsettings.HeaderColor, cardsettings.LightTextColor, cardsettings.DarkTextColor, dMember is not null ? dMember.DisplayName : dUser.Username, member.Bubbles.ToString(), (member.Drops + caughtleagueDrops).ToString(), ratio,
                BubbleWallet.FirstTrace(login), BubbleWallet.GetInventory(login).Count.ToString(), BubbleWallet.ParticipatedEvents(login).Count.ToString() + " (" + caughtEventdrops + " Drops)", Math.Round((double)member.Bubbles * 10 / 3600).ToString(),
                BubbleWallet.GlobalRanking(login).ToString(), BubbleWallet.GlobalRanking(login, true).ToString(), memberDetail.Guilds.Count.ToString(), perm.Patron, BubbleWallet.IsEarlyUser(login), perm.Moderator);

            string path = SpriteComboImage.SVGtoPNG(content);
            var s3 = await Program.S3.UploadPng(path, context.Message.Author.Id + "/card-" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString());
            await response.ModifyAsync(content: s3);

            //System.IO.File.WriteAllText("/home/pi/graph.svg", content);
            //var msg = new DiscordMessageBuilder().WithFile(System.IO.File.OpenRead("/home/pi/graph.svg"));
            //await context.RespondAsync(msg);
            //System.IO.File.Delete("/home/pi/graph.svg");
        }

        [Description("Generates a card of your profile")]
        [Command("customcard")]
        [RequirePermissionFlag((byte)16)]
        public async Task Customcard(CommandContext context, [Description("The color theme (color name or color code)")] string color = "black", [Description("Primary information color")] string lightcolor = "white", [Description("Secondary information color")] string darkcolor = "white", [Description("The URL of the background - only the filename on imgur, eg: '7pnIfgB.png'")] string backgroundUrl = "", [Description("The opacity of the background (0-1)")] double backgroundOpacity = 0.7, [Description("The opacity of the background (0-1)")] double headerOpacity = 1)
        {
            DiscordMessage response = await context.RespondAsync("\n>   <a:working:857610439588053023> **Updating your settings...**\n");
            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());

            var client = new HttpClient();
            byte[] bgbytes = await client.GetByteArrayAsync("https://i.imgur.com/" + (backgroundUrl != "" && backgroundUrl != "-" ? backgroundUrl : "qFmcbT0.png"));
            System.IO.File.WriteAllBytes(Program.CacheDataPath + "card-assets/imgur_" + backgroundUrl + ".bgb", bgbytes);
            PalantirContext db = new PalantirContext();
            CustomCard settings = new CustomCard
            {
                BackgroundImage = backgroundUrl,
                BackgroundOpacity = backgroundOpacity,
                HeaderOpacity = headerOpacity,
                HeaderColor = color,
                LightTextColor = lightcolor,
                DarkTextColor = darkcolor
            };
            db.Members.FirstOrDefault(member => member.Login.ToString() == login).Customcard = JsonConvert.SerializeObject(settings);
            db.SaveChanges();
            db.Dispose();
            string properties = "";
            foreach (System.ComponentModel.PropertyDescriptor p in System.ComponentModel.TypeDescriptor.GetProperties(settings))
            {
                properties += p.Name + ": `" + p.GetValue(settings).ToString() + "`\n";
            }
            await response.ModifyAsync(content: "**Updated your card settings!**\n\n" + properties);
            if (context.Message.ReferencedMessage is null) await Card(context);
        }

        [Description("Gift patronage to a friend")]
        [Command("patronize")]
        public async Task Patronize(CommandContext context, string gift_id = "")
        {
            PermissionFlag perm = new PermissionFlag(Program.Feanor.GetFlagByMember(context.User));
            if (!perm.Patronizer)
            {
                await Program.SendEmbed(context.Channel, "Gifts are beautiful!", "Looking to get Palantir & Typo Patron perks, but however can't pay a Patreon patronage?\n\nAsk a friend with the `Patronizer Package` subscription on Patreon to patronize you!\nYour friend just has to use the command `>patronize " + context.User.Id + "`.");
            }
            else if (gift_id == "")
            {
                await Program.SendEmbed(context.Channel, "Oh, a patronizer! :o", "To gift Patreon perks to a friend, use the command `>patronize id`, where id is the User-ID of your friend.\nYour friend can use `>patronize` to get their id!");
            }
            else if (gift_id == "none")
            {
                PalantirContext db = new PalantirContext();
                string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
                Model.Member patronizer = db.Members.FirstOrDefault(member => member.Login.ToString() == login);
                if (patronizer.Patronize is not null && DateTime.Now - DateTime.ParseExact(patronizer.Patronize.Split("#")[1], "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture) < TimeSpan.FromDays(5))
                    await Program.SendEmbed(context.Channel, "Sorry...", "You'll have to wait five days from the date of the gift (" + patronizer.Patronize.Split("#")[1] + ") to remove it!");
                else
                {
                    patronizer.Patronize = null;
                    await Program.SendEmbed(context.Channel, "Well, okay", "The gift was removed.\nMaybe choose someone else? <3");
                }
                db.SaveChanges();
                db.Dispose();
            }
            else
            {
                DiscordUser patronized = await Program.Client.GetUserAsync(Convert.ToUInt64(gift_id));
                if (patronized is null)
                    await Program.SendEmbed(context.Channel, "Sorry...", "I don't know this user ID :(\nYour friend has to use the `>patronize` command and tell you his ID!");
                else
                {
                    PalantirContext db = new PalantirContext();
                    string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
                    Model.Member patronizer = db.Members.FirstOrDefault(member => member.Login.ToString() == login);
                    if (patronizer.Patronize is not null && DateTime.Now - DateTime.ParseExact(patronizer.Patronize.Split("#")[1], "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture) < TimeSpan.FromDays(5))
                        await Program.SendEmbed(context.Channel, "Sorry...", "You'll have to wait five days from the date of the gift (" + patronizer.Patronize.Split("#")[1] + ") to change the receiver!");
                    else
                    {
                        patronizer.Patronize = gift_id + "#" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                        await Program.SendEmbed(context.Channel, "You're awesome!!", "You just gifted " + patronized.Username + " patron perks as long as you have the patronizer subscription!\nAfter a cooldown of five days, you can change the receiver with the same command or revoke it with `>patronize none`.");
                    }
                    db.SaveChanges();
                    db.Dispose();
                }
            }
        }

        [Description("Set your patron emoji")]
        [Command("patronemoji")]
        [RequirePermissionFlag((byte)16)]
        public async Task Patronemoji(CommandContext context, string emoji)
        {
            PalantirContext db = new PalantirContext();
            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
            string regexEmoji = "(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])";
            List<List<int>> cpByEmojis = Program.SplitCodepointsToEmojis(
                Program.StringToGlyphs(emoji).Where(e => Regex.Match(e, regexEmoji).Success).ToList()
                .ConvertAll(glyph => glyph.ToCharArray().ToList().ConvertAll(character => (int)character).ToList()));
            string matchedEmoji = cpByEmojis.Count == 0 ? ""
                : cpByEmojis[0].ConvertAll(point => Convert.ToChar(point)).ToDelimitedString("");
            db.Members.FirstOrDefault(member => member.Login.ToString() == login).Emoji = matchedEmoji;
            db.SaveChanges();
            db.Dispose();
            await Program.SendEmbed(context.Channel, "Emoji set to: `" + matchedEmoji + "`", "Disable it with the same command without emoji.");
        }

    }
}
