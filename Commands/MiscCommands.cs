using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
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
using SixLabors.ImageSharp.Formats;
using Microsoft.EntityFrameworkCore;

namespace Palantir.Commands
{
    public class MiscCommands : BaseCommandModule
    {

        [Command("manual")]
        [Description("Show the manual for bot usage")]
        public async Task Manual(CommandContext context)
        {
            string msg = "";
            msg += "**Visit the website:**\n";
            msg += "https://typo.rip\n";
            msg += "**Connect to the bot**\n";
            msg += " - Message the bot in DM `>login`\n";
            msg += " - Copy the login number\n";
            msg += " - Enter the login number in the browser extension popup\n";
            msg += " - Copy the server token (from the bot message or ask your admin)\n";
            msg += " - Enter the server token in the browser extension popup\n\n";
            msg += " Now all your added servers will display when you're online. \n";
            msg += "https://media.giphy.com/media/UuviRqel88ryGaL8cg/giphy.gif";

            await context.RespondAsync(msg);
        }

        [Description("Get your login data to connect the extension.")]
        [Command("login")]
        public async Task Login(CommandContext context)
        {
            await context.RespondAsync("Check out the new way to create & connect your Palantir account: \nhttps://youtu.be/Th1sanNw-EY");
        }

        [Description("Get a overview of your inventory. (old layout)")]
        [Command("oldinventory")]
        [Aliases("oldinv")]
        public async Task OldInventory(CommandContext context)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
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

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Color = DiscordColor.Magenta;
            embed.Title = "🔮  " + context.Message.Author.Username + "s Inventory";

            List<SpriteProperty> active = new List<SpriteProperty>();

            if (inventory.Count > 20)
            {
                string invList = "";
                inventory.OrderBy(s => s.ID).ToList().ForEach(s =>
                {
                    invList += "**#" + s.ID + "** - " + s.Name + " " + (s.Special ? ":sparkles:" : "") + "\n";
                    if (s.Activated) active.Add(s);
                });
                if (invList.Length < 1024) embed.AddField("All Sprites:", invList);
                else
                {
                    List<string> lines = invList.Split("\n").ToList();
                    lines.Batch(5).ForEach(b =>
                    {
                        string batch = b.ToDelimitedString("\n");
                        if (!String.IsNullOrWhiteSpace(batch)) embed.AddField("\u200b ", batch, true);
                    });
                }
            }
            else inventory.OrderBy(s => s.ID).ToList().ForEach(s =>
            {
                embed.AddField("**" + s.Name + "** ", "#" + s.ID + " |  Worth " + s.Cost + (s.EventDropID > 0 ? " Event Drops" : " Bubbles") + (s.Special ? " :sparkles: " : ""), true);
                if (s.Activated) active.Add(s);
            });

            string desc = "";

            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (perm.BubbleFarming) desc += "`🚩 Flagged as 'bubble farming'.`\n";
            if (perm.BotAdmin) desc += "`✔️ Verified cool guy aka Admin.`\n";
            if (perm.Moderator) desc += "`🛠️ Palantir Moderator.`\n";
            if (perm.CloudUnlimited || perm.Patron) desc += "`📦 Unlimited cloud storage.`\n";
            if (BubbleWallet.IsEarlyUser(login)) desc += "`💎 Early User.`\n";
            if (perm.Patron) desc += "`🎖️  Patron 💖`\n";

            List<int> spriteIDs = new List<int>();
            active.OrderBy(slot => slot.Slot).ForEach(sprite =>
            {
                spriteIDs.Add(sprite.ID);
                if (active.Count <= 1)
                {
                    desc += "\n**Selected sprite:** " + sprite.Name;
                }
                else
                {
                    desc += "\n**Slot " + sprite.Slot + ":** " + sprite.Name;
                }
            });
            if (spriteIDs.Count > 0)
            {
                string path = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(spriteIDs.ToArray()), "/home/pi/Webroot/files/combos/")
                    .Replace(@"/home/pi/Webroot/", "https://tobeh.host/");
                embed.ImageUrl = path;
            }

            int drops = BubbleWallet.GetDrops(login);
            if (inventory.Count <= 0) desc = "You haven't unlocked any sprites yet!";
            desc += "\n\n🔮 **" + BubbleWallet.CalculateCredit(login) + "** of " + BubbleWallet.GetBubbles(login) + " collected Bubbles available.";
            desc += "\n\n💧 **" + drops + "** Drops collected.";
            if (drops >= 1000 || perm.BotAdmin || perm.Patron) desc += "\n\n<a:chest:810521425156636682> **" + (perm.BotAdmin ? "Infinite" : (drops / 1000 + 1 + (perm.Patron ? 1 : 0)).ToString()) + " ** Sprite slots available.";

            embed.AddField("\u200b ", desc);

            if (inventory.Count < 5) embed.AddField("\u200b ", "Use `>use [id]` to select your Sprite!\n`>use 0` will set no Sprite.\nBuy a Sprite with `>buy [id]`.\nSpecial Sprites :sparkles: replace your whole avatar! ");
            embed.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)");
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Get a overview of your inventory.")]
        [Command("inventory")]
        [Aliases("inv")]
        public async Task Inventory(CommandContext context, int batchsize = 7)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            int drops = BubbleWallet.GetDrops(login);
            int bubbles = BubbleWallet.GetBubbles(login);
            int credit = BubbleWallet.CalculateCredit(login);
            List<SpriteProperty> inventory = BubbleWallet.GetInventory(login).OrderBy(s => s.ID).ToList();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Magenta)
                .WithTitle("🔮  " + context.Message.Author.Username + "s Inventory");

            List<string> sprites = new List<string>();
            inventory.OrderBy(s => s.ID).ToList().ForEach(s =>
            {
                sprites.Add(
                    "**#" + s.ID + "** -" + s.Name
                    + (s.Special ? " :sparkles: " : ""));
            });

            TimeSpan boostCooldown = Drops.BoostCooldown(Convert.ToInt32(login));
            embed.AddField("\u200b ",
                "`🔮` **" + credit + " ** / " + bubbles + " Bubbles\n"
                + "`💧` **" + drops + "** Drops caught\n"
                + "`🔥` " + (boostCooldown.TotalSeconds > 0 ? "Next `>dropboost` in " + boostCooldown.ToString(@"dd\d\ hh\h\ mm\m\ ss\s") : "`>dropboost` available!"));

            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            string flags = "";
            if (perm.BubbleFarming) flags += "`🚩 Bubble Farming`\n";
            if (perm.BotAdmin) flags += "`✔️ Verified cool guy aka Admin`\n";
            if (perm.Moderator) flags += "`🛠️ Palantir Moderator`\n";
            if (perm.CloudUnlimited || perm.Patron) flags += "`📦 Unlimited cloud storage`\n";
            if (BubbleWallet.IsEarlyUser(login)) flags += "`💎 Early User`\n";
            if (perm.Patron) flags += "`💖️ Patreon Subscriber`\n";
            if (perm.Patronizer) flags += "`🎁 Patronizer`\n";
            if (flags.Length > 0) embed.AddField("Flags:", flags);

            List<SceneProperty> sceneInv = BubbleWallet.GetSceneInventory(login, false, false);
            if(sceneInv.Count > 0)
            {
                embed.AddField("Scenes:", sceneInv.ConvertAll(scene => "#" + scene.ID + " - " + scene.Name + (scene.Activated ? " (active)" : "")).ToDelimitedString("\n"));
            }

            string selected = "";
            inventory.Where(spt => spt.Activated).OrderBy(slot => slot.Slot).ForEach(sprite =>
            {
                selected += "Slot " + sprite.Slot + ": " + sprite.Name + " (#" + sprite.ID + ")\n";
            });
            if (drops >= 1000 || perm.BotAdmin || perm.Patron) selected += "\n<a:chest:810521425156636682> **" + (perm.BotAdmin ? "Infinite" : (drops / 1000 + 1 + (perm.Patron ? 1 : 0)).ToString()) + " ** Sprite slots available.";
            embed.AddField("Selected Sprites:", selected.Length > 0 ? selected : "None");

            if (inventory.Where(spt => spt.Activated).Count() == 1)
                embed.ImageUrl = inventory.FirstOrDefault(s => s.Activated).URL;
            if (inventory.Where(spt => spt.Activated).Count() > 1)
                embed.ImageUrl = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(
                    inventory.Where(s => s.Activated).OrderBy(s => s.Slot).Select(s => s.ID).ToArray()), "/home/pi/Webroot/files/combos/")
                    .Replace(@"/home/pi/Webroot/", "https://tobeh.host/");

            DiscordEmbedField sleft = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            DiscordEmbedField smiddle = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            DiscordEmbedField sright = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            var spritebatches = sprites.Batch(batchsize * 3);

            if (inventory.Count < 5) embed.AddField("Command help: ", "Use `>use [id]` to select your Sprite!\n`>use 0` will set no Sprite.\nBuy a Sprite with `>buy [id]`.\nSpecial Sprites :sparkles: replace your whole avatar! ");
            embed.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)");
            DiscordMessageBuilder response = new DiscordMessageBuilder();

            Action<string, bool> setComponents = (string navText, bool disabled) =>
            {
                response.ClearComponents();
                response.AddComponents(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "last", "Previous", disabled),
                    new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "nav", navText, true),
                    new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "next", "Next", disabled));
            };
            setComponents("Navigate Sprites", false);
            DiscordMessage sent = null;
            DSharpPlus.Interactivity.InteractivityResult<DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs> result;
            int direction = 0;
            IEnumerable<string> firstbatch;
            do
            {
                // rotate batch so relevant is always first index 1 2 3 4 5 6 7 8 9 10 11 
                spritebatches = direction == 0 ? spritebatches : direction > 0 ?
                    spritebatches.Skip(1).Concat(spritebatches.Take(1)) :
                    Enumerable.TakeLast(spritebatches, 1).Concat(Enumerable.SkipLast(spritebatches, 1));

                firstbatch = spritebatches.Count() > 0 ? spritebatches.First() : Enumerable.Empty<string>();
                List<string>[] fielded = new List<string>[3];
                fielded[0] = firstbatch.ToList();
                for (int i = 1; i < 3; i++)
                {
                    fielded[i] = Enumerable.Skip(fielded[i - 1], firstbatch.Count() / 3).ToList();
                    fielded[i - 1] = fielded[i - 1].Take(fielded[i - 1].Count() - fielded[i].Count()).ToList();
                }
                sleft.Value = fielded[0].Count() > 0 ? fielded[0].ToDelimitedString("\n") : "\u200b ";
                smiddle.Value = fielded[1].Count() > 0 ? fielded[1].ToDelimitedString("\n") : "\u200b ";
                sright.Value = fielded[2].Count() > 0 ? fielded[2].ToDelimitedString("\n") : "\u200b ";

                setComponents("Navigate Sprites (" + firstbatch.Count() + "/" + spritebatches.Flatten().Count() + ")", false);
                response.Embed = embed.Build();
                sent = sent is null ? await response.SendAsync(context.Channel) : await sent.ModifyAsync(response);
                result = await Program.Interactivity.WaitForButtonAsync(sent, context.User, TimeSpan.FromMinutes(2));
                if (!result.TimedOut)
                {
                    await result.Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage);
                    direction = result.Result.Id == "next" ? 1 : -1;
                }
            }
            while (!result.TimedOut);
            setComponents("Navigate Sprites (" + firstbatch.Count() + "/" + spritebatches.Flatten().Count() + ")", true);
            await sent.ModifyAsync(response);
        }

        [Description("See who's got the most bubbles. (old layout)")]
        [Command("oldleaderboard")]
        [Aliases("oldlbd", "oldldb")]
        public async Task Leaderboard(CommandContext context, string mode = "bubbles")
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());
            Program.Feanor.UpdateMemberGuilds();
            DiscordMessage leaderboard = await context.RespondAsync("`⏱️` Loading members of `" + context.Guild.Name + "`...");
            var interactivity = context.Client.GetInteractivity();
            List<MemberEntity> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => (mode == "drops" ? m.Drops : m.Bubbles)).Where(m => m.Bubbles > 0).ToList();
            List<IEnumerable<MemberEntity>> memberBatches = members.Batch(9).ToList();
            int unranked = 0;

            DiscordEmoji down = await (await Program.Client.GetGuildAsync(779435254225698827)).GetEmojiAsync(790349869138968596);
            int page = 0;
            do
            {
                try
                {
                    await leaderboard.DeleteAllReactionsAsync();
                }
                catch (Exception e) { }
                try
                {
                    await leaderboard.CreateReactionAsync(down);
                }
                catch (Exception e) { }
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Title = "🔮  Leaderboard of " + context.Guild.Name;
                embed.Color = DiscordColor.Magenta;
                IEnumerable<MemberEntity> memberBatch = memberBatches[page];
                foreach (MemberEntity member in memberBatch)
                {
                    string name = "<@" + JsonConvert.DeserializeObject<Member>(member.Member).UserID + ">";
                    PermissionFlag perm = new PermissionFlag((byte)member.Flag);
                    if (perm.BubbleFarming)
                    {
                        unranked++;
                        embed.AddField("\u200b", "**`🚩` - " + name + "**\n `This player has been flagged as *bubble farming*`.", true);
                    }
                    else embed.AddField("\u200b", "**#" + (members.IndexOf(member) + 1 - unranked).ToString() + " - " + name + "**" + (perm.BotAdmin ? " ` Admin` " : "") + (perm.Patron ? " ` 🎖️ Patron` " : "") + "\n🔮 " + BubbleWallet.GetBubbles(member.Login).ToString() + " Bubbles\n💧 " + BubbleWallet.GetDrops(member.Login).ToString() + " Drops", true);
                }
                embed.WithFooter(context.Member.DisplayName + " can react within 2 mins to show the next page.");
                await leaderboard.ModifyAsync(embed: embed.Build(), content: "");
                page++;
                if (page >= memberBatches.Count) { page = 0; unranked = 0; }
            }
            while (!(await interactivity.WaitForReactionAsync(reaction => reaction.Emoji == down, context.User, TimeSpan.FromMinutes(2))).TimedOut);
            try { await leaderboard.DeleteAllReactionsAsync(); }
            catch { }
            await leaderboard.CreateReactionAsync(DiscordEmoji.FromName(Program.Client, ":no_entry_sign:"));
        }

        [Description("Display the link to connect to this server.")]
        [RequireGuild()]
        [Command("invite")]
        public async Task Serverinvite(CommandContext context)
        {
            ObservedGuild guild = Program.Feanor.PalantirTethers.FirstOrDefault(g => g.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirEndpoint;
            if(guild is null)
            {
                await Program.SendEmbed(context.Channel, "Aw, shoot :(", "This server is not using Palantir yet :/\nVisit https://typo.rip#admin to find out how!");
            }
            else
            {
                await context.RespondAsync("https://typo.rip/i?invite=" + guild.ObserveToken);
            }
        }

        [Description("See who's got the most bubbles.")]
        [Command("Leaderboard")]
        [Aliases("lbd", "ldb")]
        public async Task NewLeaderboard(CommandContext context, string mode = "bubbles")
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());
            Program.Feanor.UpdateMemberGuilds();
            //DiscordMessage leaderboard = await context.RespondAsync("`⏱️` Loading members of `" + context.Guild.Name + "`...");
            var interactivity = context.Client.GetInteractivity();
            List<MemberEntity> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => (mode == "drops" ? m.Drops : m.Bubbles)).Where(m => m.Bubbles > 0).ToList();
            List<IEnumerable<MemberEntity>> memberBatches = members.Batch(9).ToList();
            List<string> ranks = new List<string>();
            members.ForEach(member => {
                if (!(new PermissionFlag((byte)member.Flag)).BubbleFarming) ranks.Add(member.Login);
            });
            int page = 0;

            DiscordMessageBuilder leaderboard = new DiscordMessageBuilder();
            DiscordButtonComponent btnnext, btnprev;
            DiscordSelectComponent generateSelectWithDefault(int selected = 0, bool disabled = false)
            {
                return new DiscordSelectComponent(
                    "lbdselect",
                    "Select Page",
                    memberBatches.ConvertAll(batch => new DiscordSelectComponentOption(
                            "Page " + (memberBatches.IndexOf(batch) + 1).ToString(),
                            "page" + memberBatches.IndexOf(batch).ToString(),
                            "",
                            memberBatches.IndexOf(batch) == selected
                        )).ToArray(),
                    disabled
                );
            }
            DiscordSelectComponent selectIndex = generateSelectWithDefault();
            btnnext = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "lbdnext", "Next Page");
            btnprev = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "lbdprev", "Previous Page");
            leaderboard.WithContent("`⏱️` Loading members of `" + context.Guild.Name + "`...");
            DiscordMessage msg = await leaderboard.SendAsync(context.Channel);

            InteractivityResult<DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs> press;
            do
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Title = "🔮  Leaderboard of " + context.Guild.Name;
                embed.Color = DiscordColor.Magenta;
                IEnumerable<MemberEntity> memberBatch = memberBatches[page];
                foreach (MemberEntity member in memberBatch)
                {
                    string name = "<@" + JsonConvert.DeserializeObject<Member>(member.Member).UserID + ">";
                    PermissionFlag perm = new PermissionFlag((byte)member.Flag);
                    if (perm.BubbleFarming)
                    {
                        embed.AddField("\u200b", "**`🚩` - " + name + "**\n `This player has been flagged as *bubble farming*`.", true);
                    }
                    else embed.AddField("\u200b", "**#" + (ranks.IndexOf(member.Login) + 1) + " - " + name + "**" + (perm.BotAdmin ? " \n`Admin` " : "") + (perm.Patron ? " \n`🎖️ Patron` " : "") + (perm.Patronizer ? " \n`🎁 Patronizer` " : "") + "\n🔮 " + BubbleWallet.GetBubbles(member.Login).ToString() + " Bubbles\n💧 " + BubbleWallet.GetDrops(member.Login).ToString() + " Drops", true);
                }
                embed.WithFooter(context.Member.DisplayName + " can react within 10 mins to show the next page.");

                leaderboard.Embed = embed.Build();
                leaderboard.Content = "";
                leaderboard.AddComponents(btnprev, btnnext).AddComponents(generateSelectWithDefault(page));
                await msg.ModifyAsync(leaderboard);

                press = await interactivity.WaitForEventArgsAsync<DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs>(
                    args => {
                        if(args.Message.Id == msg.Id && args.User.Id != context.User.Id)
                        {
                            args.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage);
                            args.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder().WithContent("Hands off!\nThat's not your interaction ;)").AsEphemeral(true)
                            );
                        }
                        return args.Message.Id == msg.Id && args.User.Id == context.User.Id;
                        }, TimeSpan.FromMinutes(10));
                if (!press.TimedOut)
                {
                    await press.Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage);
                    if (press.Result.Id == "lbdprev") page--;
                    else if (press.Result.Id == "lbdnext") page++;
                    else if (press.Result.Interaction.Data.Values[0].StartsWith("page")) page = Convert.ToInt32(press.Result.Interaction.Data.Values[0].Replace("page",""));
                    if (page >= memberBatches.Count) page = 0;
                    else if (page < 0) page = memberBatches.Count - 1;
                    leaderboard.Clear();
                }
            }
            while (!press.TimedOut);

            leaderboard.ClearComponents();
            await msg.ModifyAsync(leaderboard);
        }

        [Description("Manual on how to use Bubbles")]
        [Command("Bubbles")]
        public async Task Bubbles(CommandContext context)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "🔮  How to use Bubbles";
            embed.Color = DiscordColor.Magenta;
            embed.AddField("What are Bubbles?", "Bubbles are a fictional currency of the Palantir Bot.\nWhen you're connected to the Bot, you will be rewarded 1 Bubble every 10 seconds.\nBubbles are used to buy Sprites which other users of the Skribbl-Typo extension can see in your Skribbl avatar.\nOnce in a while, on the skribbl canvas will apear a drop icon - the player who clicks it first is rewarded a Drop to their inventory.\nA Drop is worth 50 Bubbles and adds up to your Bubble credit.");
            embed.AddField("Commands", "➜ `>inventory` List your Sprites and Bubble statistics.\n➜ `>sprites` Show top 10 Sprites.\n➜ `>sprites [id]` Show a specific Sprite.\n➜ `>buy [id]` Buy a Sprite.\n➜ `>use [id]` Select one of your Sprites.\n➜ `>leaderboard` Show your server's leaderboard.\n➜ `>calc` Calculate various things.\n➜ `>event` Show details for the current event.");

            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Show bubble gain statistics")]
        [Command("stat")]
        public async Task Stat(CommandContext context, [Description("Time span mode: 'day', 'week' or 'month'.")] string mode = "day")
        {
            CultureInfo iv = CultureInfo.InvariantCulture;
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            string msg = "```css\n";
            QuartzJobs.BubbleTrace trace;
            if (mode == "week")
            {
                trace = new QuartzJobs.BubbleTrace(login, 7 * 10);
                trace.History = trace.History.Where(
                    t => t.Key.DayOfWeek == DayOfWeek.Monday || t.Key == trace.History.Keys.Min() || t.Key == trace.History.Keys.Max()
                    ).ToDictionary();
                msg += " Weekly"; }
            else if (mode == "month")
            {
                trace = new QuartzJobs.BubbleTrace(login);
                trace.History = trace.History.Where(
                     t => t.Key.Day == 1 || t.Key == trace.History.Keys.Min() || t.Key == trace.History.Keys.Max()
                     ).ToDictionary();
                msg += " Monthly";
            }
            else
            {
                trace = new QuartzJobs.BubbleTrace(login, 30);
                msg += " Daily";
            }

            msg += " Bubble-Gain from " + Convert.ToDateTime(trace.History.Keys.Min()).ToString("M", iv) + " to " + Convert.ToDateTime(trace.History.Keys.Max()).ToString("M", iv) + "\n\n";
            double offs = trace.History.Values.Min() * 0.8;
            double res = (trace.History.Values.Max() - offs) / 45;
            int prev = trace.History.Values.Min();

            trace.History.ForEach(t =>
            {
                msg += (Convert.ToDateTime(t.Key)).ToString("dd.MM") + " " +
                new string('█', (int)Math.Round((t.Value - offs) / res, 0)) +
                (t.Value - prev > 0 ? "    +" + (t.Value - prev) : "") +
                (trace.History.Keys.ToList().IndexOf(t.Key) == 0 || trace.History.Keys.ToList().IndexOf(t.Key) == trace.History.Count - 1 ? "    @" + t.Value : "") +
                "\n";
                prev = t.Value;
            });
            msg += "```\n";
            int diff = trace.History.Values.Max() - trace.History.Values.Min();
            int diffDays = (trace.History.Keys.Max() - trace.History.Keys.Min()).Days;
            msg += "> ➜ Total gained: `" + diff + " Bubbles` \n";
            double hours = (double)diff / 360;
            msg += "> ➜ Equals `" + (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h "
                + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                + TimeSpan.FromHours(hours).Seconds.ToString() + "s` on skribbl.io\n";
            msg += "> ➜ Average `" + (diff / diffDays) + " Bubbles` per day";

            await context.Channel.SendMessageAsync(msg);
        }

        [Description("Fancy calculation stuff")]
        [Command("calc")]
        public async Task Calc(CommandContext context, [Description("Calc mode: bubbles, rank or sprite")] string mode = "", [Description("Whatever fits your mode.")] double target = 0)
        {
            double hours = 0;

            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            switch (mode)
            {
                case "sprite":
                    List<Sprite> available = BubbleWallet.GetAvailableSprites();
                    Sprite sprite = available.FirstOrDefault(s => (ulong)s.ID == target);
                    if (sprite.EventDropID > 0) { await Program.SendEmbed(context.Channel, "This is an event sprite!", "It can only be bought with event drops."); return; }
                    hours = ((double)sprite.Cost - BubbleWallet.CalculateCredit(login)) / 360;
                    await Program.SendEmbed(context.Channel, "🔮  Time to get " + sprite.Name + ":",
                        (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h "
                        + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                        + TimeSpan.FromHours(hours).Seconds.ToString() + "s on skribbl.io left.");
                    break;
                case "bubbles":
                    hours = (double)target / 360;
                    await Program.SendEmbed(context.Channel, "🔮  Time to get " + target + " more Bubbles:",
                        (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h "
                        + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                        + TimeSpan.FromHours(hours).Seconds.ToString() + "s on skribbl.io left.");
                    break;
                case "rank":
                    List<MemberEntity> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => m.Bubbles).Where(m => m.Bubbles > 0).ToList();
                    hours = ((double)members[Convert.ToInt32(target) - 1].Bubbles - BubbleWallet.GetBubbles(login)) / 360;
                    await Program.SendEmbed(context.Channel, "🔮  Time catch up #" + target + ":",
                        (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h "
                        + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                        + TimeSpan.FromHours(hours).Seconds.ToString() + "s on skribbl.io left.");
                    break;
                default:
                    await Program.SendEmbed(context.Channel, "🔮  Calculate following things:", "➜ `>calc sprite 1` Calculate remaining hours to get Sprite 1 depending on your actual Bubbles left.\n➜ `>calc bubbles 1000` Calculate remaining hours to get 1000 more bubbles.\n➜ `>calc rank 4` Calculate remaining hours to catch up the 4rd ranked member.");
                    break;
            }
        }

        [Description("Calulate random loss average")]
        [Command("loss")]
        public async Task Loss(CommandContext context, [Description("The amount of a single gift")] int amount, [Description("The total drop amount with repeated gifts of before specified amount")] int total = 0)
        {
            int sum = 0;
            for (int i = 0; i < 100; i++) sum += (new Random()).Next(0, amount / 3 + 1);
            string totalres = "";
            if (total > amount)
            {
                int times = total / amount + (total % amount > 0 ? 1 : 0);
                totalres = "\nTo gift a total of " + total + " Drops " + times + " gifts of each " + amount + " Drops are required, which equals a loss of " + Math.Round((sum * times) / 100.0, 2) + " Drops.";
            }
            await Program.SendEmbed(context.Channel, "Such a nerd...", "With 100 random tries, an average of " + Math.Round(sum / 100.0, 2) + " Drops of " + amount + " gifted Drops is lost." + totalres);
        }

        [Description("HAHAH some nerd stuff, shows EXACTLY what the emoji(s) consist of & how they're bound together")]
        [Command("parsemoji")]
        public async Task Parsemoji(CommandContext context, string emoji)
        {
            
            string result = emoji + " - length: " + emoji.Length + "\n";
            string regexEmoji = "(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])";

            List<string> emojiGlyphs = Program.StringToGlyphs(emoji).Where(e => Regex.Match(e, regexEmoji).Success).ToList();
            result += "Detected emoji glyphs: " + emojiGlyphs.ConvertAll(glyph => "[`" + glyph + "`]").ToDelimitedString("-") + "\n";
            result += "Consisting of codepoints: " + emojiGlyphs.ConvertAll(
                glyph => "[" + glyph.ToCharArray().ToList().ConvertAll(codept => ((int)codept).ToString("X")).ToDelimitedString(", ") + "]")
                .ToDelimitedString(" - ") + "\n";
            List<List<int>> cpByEmojis = Program.SplitCodepointsToEmojis(
                emojiGlyphs.ConvertAll(glyph => glyph.ToCharArray().ToList().ConvertAll(character => (int)character).ToList()));
            result += "Independend emojis from codepoints: " + cpByEmojis
                .ConvertAll(emojiCodepoints => "[" + emojiCodepoints.ToDelimitedString(", ") + "]").ToDelimitedString(" - ") + "\n";
            result += "\nFirst complete emoji: " + cpByEmojis[0].ConvertAll(point => Convert.ToChar(point)).ToDelimitedString("");
            
            await Program.SendEmbed(context.Channel, "Emoji Parse Analysis", result);
        }

        [Description("Gets ping statistics.")]
        [Command("ping")]
        public async Task Ping(CommandContext context)
        {
            System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
            long discordRTT = ping.Send("discord.gg", 100).RoundtripTime;

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

            embed.Title = "Latency results:";
            embed.AddField("`🗄️` Database singe read", Program.Feanor.DatabaseReadTime(context.User.Id.ToString(), 1) + "ms");
            embed.AddField("`🗂️` Database average for 100 reads", Program.Feanor.DatabaseReadTime(context.User.Id.ToString(), 100) + "ms");
            embed.AddField("`🌐` Discord API request", Program.Client.Ping + "ms");
            embed.AddField("`⌛` Discord.gg ping RTT", discordRTT + "ms");
            await context.RespondAsync(embed: embed);
        }

        [Description("Show some nice information about the bot.")]
        [Command("about")]
        public async Task About(CommandContext context)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

            embed.Title = "About me";
            embed.AddField("`🤖` Hi!", "I'm Palantir - I integrate skribbl typo into Discord. \nMy main task is to sell sprites and show you information what's going on with typo.");
            embed.AddField("`👪` ", "Currently **" + Program.Feanor.PalantirTethers.Count + " ** servers are using me to show skribbl lobbies.");
            int avgMembers = 0;
            Program.Client.Guilds.ForEach(guild => avgMembers += guild.Value.MemberCount / Program.Client.Guilds.Count);
            embed.AddField("`🗄️` ", "Overall **" + Program.Client.Guilds.Count + " ** servers invited me to join.\nIn average, these servers have " + avgMembers + " members.");
            PalantirDbContext cont = new PalantirDbContext();
            int members = cont.Members.Count();
            cont.Dispose();
            embed.AddField("`👥` ", "**" +  members + " ** people have registered on Palantir.");
            embed.AddField("`❤️` ", "**" + Program.Feanor.PatronCount + " ** Patrons are supporting Typo on Patreon.");
            await context.RespondAsync(embed: embed);
        }


        [Description("Gets a png of a sprite combo.")]
        [Command("combopng")]
        public async Task Combopng(CommandContext context, [Description("The id of the sprites (eg '15 0 16 17')")] params int[] sprites)
        {
            string path = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(sprites), "/home/pi/tmpGen/");
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                var msg = await new DiscordMessageBuilder()
                    .WithFiles(new Dictionary<string, System.IO.Stream>() { { "combo.png", fs } })
                    .SendAsync(context.Channel);
            }
        }

        [Description("Show available typo themes.")]
        [Command("themes")]
        [Aliases("theme")]
        public async Task Themes(CommandContext context, [Description("The id of the theme")] int id = 0)
        {
            var embed = new DiscordEmbedBuilder();
            PalantirDbContext db = new();
            List<TypoThemeEntity> themes = db.Themes.Where(theme => !String.IsNullOrEmpty(theme.Theme)).ToList();
            db.Dispose();

            if(id <= 0 || id > themes.Count)
            {
                embed.WithTitle("Listing all **Typo Themes**:");
                embed.WithDescription("Click a link to add the theme or use `>themes [id]` to view theme details!\nTo add your own theme, contact a Palantir mod.");
                themes.ForEach((theme, index) =>
                {
                    embed.AddField("➜ " + theme.Name, "#" + (index + 1) +" - by `" + theme.Author + "` - https://typo.rip/t?ticket=" + theme.Ticket);
                });
            }
            else
            {
                TypoThemeEntity theme = themes[id - 1];
                embed.WithTitle("Theme **" + theme.Name + "**");
                embed.WithDescription(theme.Description);
                embed.AddField("Add the theme:", "https://typo.rip/t?ticket=" + theme.Ticket);
                embed.WithFooter("Created by " + theme.Author);
                embed.WithImageUrl(theme.ThumbnailLanding);
            }
            await context.RespondAsync(embed);
        }

        [Description("See the trend of ppl using Palantir")]
        [Command("trend")]
        [RequireBeta()]
        public async Task ActiveUsers(CommandContext context)
        {
            string graph = "";
            PalantirDbContext db = new PalantirDbContext();
            List<BubbleTraceEntity> traces = db.BubbleTraces.ToList();
            db.Dispose();
            List<BubbleTraceEntity> dailyChangedTraces = traces.DistinctBy(t => new { t.Bubbles, t.Login }).ToList();
            var x = traces.Select(trace => trace.Date).Distinct().ToList().ConvertAll(
                date => date + "," + dailyChangedTraces.Where(trace => trace.Date == date).Count()
                );
            graph = x.ToDelimitedString("\n");
            System.IO.File.WriteAllText("/home/pi/graph.csv", graph);
            var msg = new DiscordMessageBuilder().WithFile(System.IO.File.OpenRead("/home/pi/graph.csv"));
            await context.RespondAsync(msg);
            System.IO.File.Delete("/home/pi/graph.csv");
        }

        [Description("Search the image cloud for an image")]
        [Command("cloudsearch")]
        [RequireBeta()]
        public async Task Cloudsearch(CommandContext context)
        {
            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
            ImageDbContext idb = new ImageDbContext("/home/pi/Webroot/rippro/userdb/udb" + login + ".db");
            int count = idb.Drawings.Count();
            //idb.Drawings.FromSqlInterpolated("select * from drawings where ")
            await Program.SendEmbed(context.Channel,count.ToString(), "");
        }

        [Description("Get the average drop frequency")]
        [Command("droprate")]
        public async Task DropRate(CommandContext context)
        {
            const int attempts = 10;
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            double average = 0;
            for (int i = 0; i < attempts; i++) average += Drops.CalculateDropTimeoutSeconds() / attempts;
            List<BoostEntity> boostlist = Drops.GetActiveBoosts();
            string boosts;
            if (boostlist.Count > 0)
            {
                boosts = boostlist.ConvertAll(
                boost => " x" + boost.Factor
                + " (" + (Math.Round((boost.StartUTCS + boost.DurationS - now)/60000,1) + "min left)")).ToDelimitedString("\n");
                boosts += "\n=============\n **x" + Math.Round(Drops.GetCurrentFactor(),1) + " Boost active**";
            }
            else boosts = "No Drop Boosts active :(";
            await Program.SendEmbed(context.Channel, "Current Drop Rate", "ATM, drops appear in an average frequency of about " + Math.Round(average, 0) + "s\n\nThis includes following boosts:\n" + boosts + "\n\nYou can boost once a week with `>dropboost`.");
        }

        [Description("Boost the drop frequency. You can do this once a week.")]
        [Command("dropboost")]
        public async Task DropBoost(CommandContext context)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (perm.Permanban)
            {
                await Program.SendEmbed(context.Channel, "So... you're one of the bad guys, huh?", "Users with a permanban obviously cant boost, lol");
                return;
            }
            int login = Convert.ToInt32(BubbleWallet.GetLoginOfMember(context.User.Id.ToString()));
            double factor = 1.1;
            if (perm.Patron) factor = 1.5;
            if (perm.Patronizer) factor = 1.8;
            BoostEntity boost;
            bool boosted = Drops.AddBoost(login, factor, 60 * 60, out boost);
            if (!boosted)
            {
                string left = Drops.BoostCooldown(login).ToString(@"dd\d\ hh\h\ mm\m\ ss\s");
                await Program.SendEmbed(context.Channel, "Take your time...", "The cooldown after a drop boost is one week.\nYou can't boost yet!\nWait " + left);
            }
            else await Program.SendEmbed(context.Channel, "Wooohoo!", "You " + (perm.Patron ? "used Patron perks and " : "") + "boosted drops for one hour by the factor " + boost.Factor + "!\nCheck boosts with `>droprate`, you can boost again in **one week**.");
        }

    }
}
