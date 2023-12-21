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
using Palantir.Model;
using System.IO;
using Palantir.PalantirCommandModule;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.EventArgs;

namespace Palantir.Commands
{
    public class MiscCommands : PalantirCommandModule.PalantirCommandModule
    {

        [Command("manual")]
        [Description("Show the manual for bot usage")]
        public async Task Manual(CommandContext context)
        {
            string msg = "";
            msg += "**Visit the website:**\n";
            msg += "https://www.typo.rip\n";
            msg += "**Connect to the bot**\n";
            msg += " - Message the bot in DM `>login`\n";
            msg += " - Copy the login number\n";
            msg += " - Enter the login number in the browser extension popup\n";
            msg += " - Copy the server token (from the bot message or ask your admin)\n";
            msg += " - Enter the server token in the browser extension popup\n\n";
            msg += " Now all your added servers will display when you're online. \n";

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

            PermissionFlag perm = new PermissionFlag(Program.Feanor.GetFlagByMember(context.User));
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
                string path = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(spriteIDs.ToArray()));
                var s3 = await Program.S3.UploadPng(path, context.Message.Author.Id + "/sprite-combo-" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString());
                embed.ImageUrl = s3;
            }

            int drops = BubbleWallet.GetDrops(login, context.User.Id.ToString());
            int splits = BubbleWallet.GetMemberSplits(Convert.ToInt32(login), perm).Sum(s => s.Value);
            if (inventory.Count <= 0) desc = "You haven't unlocked any sprites yet!";
            desc += "\n\n🔮 **" + BubbleWallet.CalculateCredit(login, context.User.Id.ToString()) + "** of " + BubbleWallet.GetBubbles(login) + " collected Bubbles available.";
            desc += "\n\n💧 **" + drops + "** Drops collected.";
            if (splits > 0) desc += "\n\n🏆 **" + splits + "** Splits rewarded.";
            if (drops >= 1000 || perm.BotAdmin || perm.Patron) desc += "\n\n<a:chest:810521425156636682> **" + (perm.BotAdmin ? "Infinite" : (drops / 1000 + 1 + (perm.Patron ? 1 : 0)).ToString()) + " ** Sprite slots available.";

            embed.AddField("\u200b ", desc);

            if (inventory.Count < 5) embed.AddField("\u200b ", "Use `>use [id]` to select your Sprite!\n`>use 0` will set no Sprite.\nBuy a Sprite with `>buy [id]`.\nSpecial Sprites :sparkles: replace your whole avatar! ");
            embed.AddField("\u200b", "[View all Sprites](https://www.typo.rip/tools/sprites)");
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Get a overview of your inventory.")]
        [Command("inventory")]
        [Aliases("inv")]
        public async Task Inventory(CommandContext context, int batchsize = 7)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            int drops = BubbleWallet.GetDrops(login, context.User.Id.ToString());
            int bubbles = BubbleWallet.GetBubbles(login);
            int credit = BubbleWallet.CalculateCredit(login, context.User.Id.ToString());
            List<SpriteProperty> inventory = BubbleWallet.GetInventory(login).OrderBy(s => s.ID).ToList();
            PermissionFlag perm = new PermissionFlag(Program.Feanor.GetFlagByMember(context.User));
            int splits = BubbleWallet.GetMemberSplits(Convert.ToInt32(login), perm).Sum(s => s.Value);
            int regLeagueDrops = League.GetLeagueEventDropWeights(context.User.Id.ToString()).Count;
            int leagueDrops = regLeagueDrops + League.GetLeagueDropWeights(context.User.Id.ToString()).Count;

            DiscordMessageBuilder response = new DiscordMessageBuilder();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Magenta)
                .WithTitle("🔮  " + context.Message.Author.Username + "s Inventory");

            List<string> sprites = new List<string>();
            inventory.OrderBy(s => s.ID).ToList().ForEach(s =>
            {
                sprites.Add(
                    "**#" + s.ID + "** -" + s.Name
                    + (s.Special ? " :sparkles: " : "")
                    + (s.Rainbow ? " :rainbow: " : ""));
            });

            TimeSpan boostCooldown = Drops.BoostCooldown(Convert.ToInt32(login));
            embed.AddField("\u200b ",
                "`🔮` **" + credit + " ** / " + bubbles + " Bubbles\n"
                + "`💧` **" + drops + "** Drops caught\n"
                + (splits > 0 ? "`🏆` **" + splits + "** Splits rewarded\n" : "")
                + "`🔥` " + (boostCooldown.TotalSeconds > 0 ? "Next `>dropboost` in " + boostCooldown.ToString(@"dd\d\ hh\h\ mm\m\ ss\s") : "`>dropboost` available!")
                + (leagueDrops > 0 ? "\n\n<a:league_rnk1:987699431350632518> **" + leagueDrops + "** League Drops caught\n" : ""));

            string flags = "";
            if (perm.BubbleFarming) flags += "`🚩 Bubble Farming`\n";
            if (perm.BotAdmin) flags += "`✔️ Verified cool guy aka Admin`\n";
            if (perm.Moderator) flags += "`🛠️ Palantir Moderator`\n";
            //if (perm.CloudUnlimited || perm.Patron) flags += "`📦 Unlimited cloud storage`\n";
            if (BubbleWallet.IsEarlyUser(login)) flags += "`💎 Early User`\n";
            if (perm.Patron) flags += "`💖️ Patreon Subscriber`\n";
            if (perm.Patronizer) flags += "`🎁 Patronizer`\n";
            if (flags.Length > 0) embed.AddField("Flags:", flags);

            List<SceneProperty> sceneInv = BubbleWallet.GetSceneInventory(login, false, false);
            if (sceneInv.Count > 0)
            {
                embed.AddField("Scenes:", sceneInv.OrderBy(scene => scene.Id).ToList().ConvertAll(scene => "#" + scene.Id + " - " + scene.Name + (scene.Activated ? " (active)" : "")).ToDelimitedString("\n"));
            }

            string selected = "";
            inventory.Where(spt => spt.Activated).OrderBy(slot => slot.Slot).ForEach(sprite =>
            {
                selected += "Slot " + sprite.Slot + ": " + sprite.Name + " (#" + sprite.ID + ")\n";
            });
            if (drops >= 1000 || perm.BotAdmin || perm.Patron) selected += "\n<a:chest:810521425156636682> **" + (perm.BotAdmin ? "Infinite" : ((drops) / 1000 + 1 + (perm.Patron ? 1 : 0)).ToString()) + " ** Sprite slots available.";
            embed.AddField("Selected Sprites:", selected.Length > 0 ? selected : "None");

            if (inventory.Where(spt => spt.Activated).Count() == 1)
                embed.ImageUrl = inventory.FirstOrDefault(s => s.Activated).URL;
            if (inventory.Where(spt => spt.Activated).Count() > 1)
            {
                var path = SpriteComboImage.GenerateImage(
                    SpriteComboImage.GetSpriteSources(
                        inventory.Where(s => s.Activated).OrderBy(s => s.Slot).Select(s => s.ID).ToArray(),
                        BubbleWallet.GetMemberRainbowShifts(login)
                    ));

                response.AddFile("combo.png", File.OpenRead(path));
           
                embed.ImageUrl = "attachment://combo.png";

                //var s3 = await Program.S3.UploadPng(path, context.Message.Author.Id + "/card-" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString());
                //embed.ImageUrl = s3;
            }

            DiscordEmbedField sleft = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            DiscordEmbedField smiddle = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            DiscordEmbedField sright = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            var spritebatches = sprites.Batch(batchsize * 3);

            if (inventory.Count < 5) embed.AddField("Command help: ", "Use `>use [id]` to select your Sprite!\n`>use 0` will set no Sprite.\nBuy a Sprite with `>buy [id]`.\nSpecial Sprites :sparkles: replace your whole avatar! \nRainbow Sprites :rainbow: can be color-customized! (`>rainbow`) ");
            embed.AddField("\u200b", "[View all Sprites](https://www.typo.rip/tools/sprites)");

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
                if (sent is not null && sent.Embeds.Count > 0 && sent.Embeds[0].Image is not null)
                {
                    // tried using attachment scheme, url of sent message, modifying without specifying attachments...
                    embed.ImageUrl = $"https://cdn.discordapp.com/attachments/{sent.ChannelId}/{sent.Id}/combo.png";
                }
                response.Embed = embed.Build();
                sent = sent is null ? await response.SendAsync(context.Channel) : await sent.ModifyAsync(response, false, sent.Attachments);
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
            List<Model.Member> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => (mode == "drops" ? m.Drops : m.Bubbles)).Where(m => m.Bubbles > 0).ToList();
            List<IEnumerable<Model.Member>> memberBatches = members.Batch(9).ToList();
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
                IEnumerable<Model.Member> memberBatch = memberBatches[page];
                foreach (Model.Member member in memberBatch)
                {
                    string name = "<@" + JsonConvert.DeserializeObject<Member>(member.Member1).UserID + ">";
                    PermissionFlag perm = new PermissionFlag(Convert.ToInt16(member.Flag));
                    if (perm.BubbleFarming)
                    {
                        unranked++;
                        embed.AddField("\u200b", "**`🚩` - " + name + "**\n `This player has been flagged as *bubble farming*`.", true);
                    }
                    else embed.AddField("\u200b", "**#" + (members.IndexOf(member) + 1 - unranked).ToString() + " - " + name + "**" + (perm.BotAdmin ? " ` Admin` " : "") + (perm.Patron ? " ` 🎖️ Patron` " : "") + "\n🔮 "
                        + BubbleWallet.GetBubbles(member.Login.ToString()).ToString() + " Bubbles\n💧 "
                        + BubbleWallet.GetDrops(member.Login.ToString(), JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(member.Login.ToString()).Member1).UserID).ToString() + " Drops", true
                       );
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
            if (guild is null)
            {
                await Program.SendEmbed(context.Channel, "Aw, shoot :(", "This server is not using Palantir yet :/\nVisit https://www.typo.rip/help/palantir to find out how!");
            }
            else
            {
                await context.RespondAsync("https://www.typo.rip/invite/" + guild.ObserveToken);
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
            List<Model.Member> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => (mode == "drops" ? m.Drops : m.Bubbles)).Where(m => m.Bubbles > 0).ToList();
            List<IEnumerable<Model.Member>> memberBatches = members.Batch(9).ToList();
            List<string> ranks = new List<string>();
            members.ForEach(member =>
            {
                if (!(new PermissionFlag(Convert.ToInt16(member.Flag))).BubbleFarming) ranks.Add(member.Login.ToString());
            });
            int page = 0;

            DiscordMessageBuilder leaderboard = new DiscordMessageBuilder();
            DiscordButtonComponent btnnext, btnprev;
            DiscordSelectComponent generateSelectWithDefault(int selected = 0, bool disabled = false)
            {
                var truncBatches = new List<IEnumerable<Model.Member>>();
                if (memberBatches.Count >= 25)
                {
                    int right = selected + 12;
                    if (right >= memberBatches.Count) right = memberBatches.Count - 1;
                    int left = right - 25;
                    if (left < 0)
                    {
                        left = 0;
                        right = 25;
                    }

                    truncBatches = memberBatches.Skip(left).Take(25).ToList();
                }
                else truncBatches = memberBatches;

                return new DiscordSelectComponent(
                    "lbdselect",
                    "Select Page",
                    truncBatches.ConvertAll(batch => new DiscordSelectComponentOption(
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
                embed.WithDescription("`Note: This leaderboard does not account league drops!`");
                IEnumerable<Model.Member> memberBatch = memberBatches[page];
                foreach (Model.Member member in memberBatch)
                {
                    string name = "<@" + JsonConvert.DeserializeObject<Member>(member.Member1).UserID + ">";
                    PermissionFlag perm = new PermissionFlag(Convert.ToInt16(member.Flag));
                    if (perm.BubbleFarming)
                    {
                        embed.AddField("\u200b", "**`🚩` - " + name + "**\n `This player has been flagged as *bubble farming*`.", true);
                    }
                    else embed.AddField("\u200b", "**#" + (ranks.IndexOf(member.Login.ToString()) + 1) + " - " + name + "**" + (perm.BotAdmin ? " \n`Admin` " : "") + (perm.Patron ? " \n`🎖️ Patron` " : "") + (perm.Patronizer ? " \n`🎁 Patronizer` " : "") + "\n🔮 " + BubbleWallet.GetBubbles(member.Login.ToString()).ToString() + " Bubbles\n💧 " + BubbleWallet.GetDrops(member.Login.ToString(), JsonConvert.DeserializeObject<Member>(Program.Feanor.GetMemberByLogin(member.Login.ToString()).Member1).UserID).ToString() + " Drops", true);
                }
                embed.WithFooter(context.Member.DisplayName + " can react within 10 mins to show the next page.");

                leaderboard.Embed = embed.Build();
                leaderboard.Content = "";
                leaderboard.AddComponents(btnprev, btnnext).AddComponents(generateSelectWithDefault(page));
                await msg.ModifyAsync(leaderboard);

                press = await interactivity.WaitForEventArgsAsync<DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs>(
                    args =>
                    {
                        if (args.Message.Id == msg.Id && args.User.Id != context.User.Id)
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
                    else if (press.Result.Interaction.Data.Values[0].StartsWith("page")) page = Convert.ToInt32(press.Result.Interaction.Data.Values[0].Replace("page", ""));
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
                msg += " Weekly";
            }
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
                    hours = ((double)sprite.Cost - BubbleWallet.CalculateCredit(login, context.User.Id.ToString())) / 360;
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
                    List<Model.Member> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => m.Bubbles).Where(m => m.Bubbles > 0).ToList();
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
            PalantirContext cont = new PalantirContext();
            int members = cont.Members.Count();
            cont.Dispose();
            embed.AddField("`👥` ", "**" + members + " ** people have registered on Palantir.");
            embed.AddField("`❤️` ", "**" + Program.Feanor.PatronCount + " ** Patrons are supporting Typo on Patreon.");
            await context.RespondAsync(embed: embed);
        }


        [Description("Gets a png of a sprite combo.")]
        [Command("combopng")]
        public async Task Combopng(CommandContext context, [Description("The id of the sprites (eg '15 0 16 17')")] params int[] sprites)
        {
            string path = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(sprites)); 
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                var msg = await new DiscordMessageBuilder()
                    .AddFiles(new Dictionary<string, System.IO.Stream>() { { "combo.png", fs } })
                    .SendAsync(context.Channel);
            }
            File.Delete(path);
        }

        //[Description("Show available typo themes.")]
        //[Command("themes")]
        //[Aliases("theme")]
        //public async Task Themes(CommandContext context, [Description("The id of the theme")] int id = 0)
        //{
        //    var embed = new DiscordEmbedBuilder();
        //    PalantirContext db = new();
        //    List<Theme> themes = db.Themes.Where(theme => !String.IsNullOrEmpty(theme.Theme1)).ToList();
        //    db.Dispose();

        //    if (id <= 0 || id > themes.Count)
        //    {
        //        embed.WithTitle("Listing all **Typo Themes**:");
        //        embed.WithDescription("Click a link to add the theme or use `>themes [id]` to view theme details!\nTo add your own theme, contact a Palantir mod.");
        //        themes.ForEach((theme, index) =>
        //        {
        //            embed.AddField("➜ " + theme.Name, "#" + (index + 1) + " - by `" + theme.Author + "` - https://typo.rip/t?ticket=" + theme.Ticket);
        //        });
        //    }
        //    else
        //    {
        //        Theme theme = themes[id - 1];
        //        embed.WithTitle("Theme **" + theme.Name + "**");
        //        embed.WithDescription(theme.Description);
        //        embed.AddField("Add the theme:", "https://typo.rip/t?ticket=" + theme.Ticket);
        //        embed.WithFooter("Created by " + theme.Author);
        //        embed.WithImageUrl(theme.ThumbnailLanding);
        //    }
        //    await context.RespondAsync(embed);
        //}

        [Description("See the trend of ppl using Palantir")]
        [Command("trend")]
        [RequireBeta()]
        public async Task ActiveUsers(CommandContext context)
        {
            string graph = "";
            PalantirContext db = new PalantirContext();
            List<BubbleTrace> traces = db.BubbleTraces.ToList();
            db.Dispose();
            List<BubbleTrace> dailyChangedTraces = MoreLinq.Extensions.DistinctByExtension.DistinctBy(traces, t => new { t.Bubbles, t.Login }).ToList();
            var x = traces.Select(trace => trace.Date).Distinct().ToList().ConvertAll(
                date => date + "," + dailyChangedTraces.Where(trace => trace.Date == date).Count()
                );
            graph = x.ToDelimitedString("\n");
            System.IO.File.WriteAllText(Program.CacheDataPath + "/graph.csv", graph);
            var msg = new DiscordMessageBuilder().AddFile(System.IO.File.OpenRead(Program.CacheDataPath + "/graph.csv"));
            await context.RespondAsync(msg);
            System.IO.File.Delete(Program.CacheDataPath + "/graph.csv");
        }

        [Description("Get the average drop frequency")]
        [Command("droprate")]
        public async Task DropRate(CommandContext context)
        {
            const int attempts = 10;
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            double average = 0;
            for (int i = 0; i < attempts; i++) average += Drops.CalculateDropTimeoutSeconds() / attempts;
            List<DropBoost> boostlist = Drops.GetActiveBoosts();
            string boosts;
            if (boostlist.Count > 0)
            {
                boosts = boostlist.ConvertAll(
                boost => " x" + boost.Factor
                + " (" + (Math.Round((Convert.ToInt64(boost.StartUtcs) + boost.DurationS - now) / 60000, 1) + "min left)")).ToDelimitedString("\n");
                boosts += "\n=============\n **x" + Math.Round(Drops.GetCurrentFactor(), 1) + " Boost active**";
            }
            else boosts = "No Drop Boosts active :(";

            double mins = Math.Floor(average / 60);
            double secs = Math.Floor(average - mins * 60);

            await Program.SendEmbed(context.Channel, "Current Drop Rate", "ATM, drops appear in an average frequency of about " + mins + "min " + secs + "s.\n\n" + QuartzJobs.StatusUpdaterJob.currentOnlineIDs + " people are playing.\n\nFollowing boosts are active:\n" + boosts + "\n\nYou can boost once a week with `>dropboost`.");
        }

        //[Description("Boost the drop frequency. You can do this once a week.")]
        //[Command("dropboost")]
        //public async Task DropBoost(CommandContext context)
        //{
        //    PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
        //    if (perm.Permanban)
        //    {
        //        await Program.SendEmbed(context.Channel, "So... you're one of the bad guys, huh?", "Users with a permanban obviously cant boost, lol");
        //        return;
        //    }
        //    int login = Convert.ToInt32(BubbleWallet.GetLoginOfMember(context.User.Id.ToString()));
        //    double factor = 1.1;
        //    if (perm.Patron) factor = 1.5;
        //    if (perm.Patronizer) factor = 1.8;
        //    BoostEntity boost;
        //    bool boosted = Drops.AddBoost(login, factor, 60 * 60, 0, out boost);
        //    if (!boosted)
        //    {
        //        string left = Drops.BoostCooldown(login).ToString(@"dd\d\ hh\h\ mm\m\ ss\s");
        //        await Program.SendEmbed(context.Channel, "Take your time...", "The cooldown after a drop boost is one week.\nYou can't boost yet!\nWait " + left);
        //    }
        //    else await Program.SendEmbed(context.Channel, "Wooohoo!", "You " + (perm.Patron ? "used Patron perks and " : "") + "boosted drops for one hour by the factor " + boost.Factor + "!\nCheck boosts with `>droprate`, you can boost again in **one week**.");
        //}

        [Description("Add a split reward to someone")]
        [Command("splitreward")]
        [RequirePermissionFlag(2)]
        public async Task Splitreward(CommandContext context, long id, int split)
        {
            int login = Convert.ToInt32(BubbleWallet.GetLoginOfMember(id.ToString()));

            SplitReward reward = new SplitReward()
            {
                Login = login,
                Split = split,
                RewardDate = DateTime.UtcNow.ToShortDateString(),
                Comment = "",
                ValueOverride = -1
            };
            PalantirContext db = new();
            db.SplitCredits.Add(reward);
            db.SaveChanges();
            db.Dispose();
            await Program.SendEmbed(context.Channel, "Added split reward", "Userid: " + id + ", Split: " + split);
        }

        [Description("Show your earned boost splits.")]
        [Command("splits")]
        public async Task Splits(CommandContext context)
        {
            PermissionFlag flags = new PermissionFlag(Program.Feanor.GetFlagByMember(context.User));
            int login = Convert.ToInt32(BubbleWallet.GetLoginOfMember(context.User.Id.ToString()));

            var memberSplits = BubbleWallet.GetMemberSplits(login, flags);

            var pages = new List<Page>();

            foreach (var batch in memberSplits.Batch(20))
            {
                var message = new DiscordEmbedBuilder();
                message.WithTitle(context.Message.Author.Username + "s Split Achievements");
                message.WithColor(DiscordColor.Magenta);

                if (memberSplits.Count == 0)
                {
                    message.WithDescription("You haven't earned any Splits yet :(\n\nSplits are used to make your Drop Boosts more powerful.\nYou get them by occasional giveaways/challenges or by competing in Drop Leagues.");
                }
                else
                {
                    message.AddField(memberSplits.Sum(s => s.Value).ToString() + " total earned Splits", memberSplits.Where(s => !s.Expired).Sum(s => s.Value).ToString() + " Splits available");

                    batch.ForEach(split =>
                    {
                        message.AddField("➜ " + split.Name + (split.RewardDate != null && split.RewardDate != "" ? "  `" + split.RewardDate + "`" : "") + (split.Expired ? " / *expired*" : ""), split.Description + "\n *worth " + split.Value + " Splits*" + (split.Comment != null && split.Comment.Length > 0 ? " ~ `" + split.Comment + "`" : ""));
                    });

                    message.WithDescription("You can use your Splits to customize your Drop Boosts.\nChoose the boost intensity, duration or cooldown individually when using `>dropboost`\n_ _");
                }
                pages.Add(new Page(embed: message));
            }

            if (pages.Count == 1)
            {
                await context.Message.RespondAsync(pages[0].Embed);
            }
            else
            {
                await context.Client.GetInteractivity().SendPaginatedMessageAsync(context.Channel, context.User, pages);
            }
        }

        [Description("Boost the drop rate. You can do this once a week.")]
        [Synchronized]
        [Command("dropboost")]
        public async Task SplitBoost(CommandContext context, int factorSplits = 0, int durationSplits = 0, int cooldownSplits = 0, string modifier = "")
        {
            PermissionFlag perm = new PermissionFlag(Program.Feanor.GetFlagByMember(context.User));
            if (perm.Permanban)
            {
                await Program.SendEmbed(context.Channel, "So... you're one of the bad guys, huh?", "Users with a permanban obviously cant boost, lol");
                return;
            }
            int login = Convert.ToInt32(BubbleWallet.GetLoginOfMember(context.User.Id.ToString()));

            TimeSpan cooldown = Drops.BoostCooldown(login);

            if (cooldown.TotalMilliseconds > 0)
            {
                string left = Drops.BoostCooldown(login).ToString(@"dd\d\ hh\h\ mm\m\ ss\s");
                await Program.SendEmbed(context.Channel, "Take your time...", "The default cooldown after a drop boost is one week.\nYou can't boost yet!\nWait " + left);
                return;
            }
            else
            {
                double factor = 1.1;
                factorSplits = factorSplits - factorSplits % 2;
                var memberSplits = BubbleWallet.GetMemberSplits(login, perm).Where(split => !split.Expired).ToList();
                int memberAvailableSplits = memberSplits.Sum(s => s.Value);

                if (factorSplits + durationSplits + cooldownSplits > memberAvailableSplits) factorSplits = durationSplits = cooldownSplits = 0;


                var chooseMessage = new DiscordMessageBuilder()
                    .WithContent("> **Customize your Dropboost**\n> \n> You have `" + memberAvailableSplits + "` Splits available.\n> Using splits, you can power up your boost. \n> Use `>splits` to learn more about your Splits.\n> \n> `🔥 Intensity: +2 Splits => +0.1 factor`\n> `⌛ Duration:  +1 Split  => +20min boost`\n> `💤 Cooldown:  +1 Split  => -12hrs until next boost`\n> _ _");

                Action<string, bool> updateComponents = (string starttext, bool disable) =>
                {
                    chooseMessage.ClearComponents();

                    var minusFactor = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "-fac", "-", disable);
                    var plusFactor = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "+fac", "+", disable);
                    var labelFactor = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "fac", "Boost Factor: " + factorSplits + " Splits (+" + Math.Round(factorSplits * 0.05, 1) + "x)", true);

                    var minusDur = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "-dur", "-", disable);
                    var plusDur = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "+dur", "+", disable);
                    var labelDur = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "dur", "Boost Duration: " + durationSplits + " Splits (+" + durationSplits * 20 + "min)", true);

                    var minusCool = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "-cool", "-", disable);
                    var plusCool = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "+cool", "+", disable);
                    var labelCool = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "cool", "Boost Cooldown: " + cooldownSplits + " Splits (-" + cooldownSplits * 12 + "hrs)", true);

                    var start = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "start", starttext + " (" + (cooldownSplits + durationSplits + factorSplits) + "/" + memberAvailableSplits + " Splits selected)", disable);

                    chooseMessage
                        .AddComponents(minusFactor, labelFactor, plusFactor)
                        .AddComponents(minusDur, labelDur, plusDur)
                        .AddComponents(minusCool, labelCool, plusCool)
                        .AddComponents(start);
                };

                updateComponents("Start Dropboost", false);

                var sent = await context.RespondAsync(chooseMessage);

                async Task StartBoost()
                {
                    DropBoost boost;

                    factor = factor + factorSplits * 0.05;
                    int duration = (60 + durationSplits * 20) * 60;
                    int cooldownRed = 60 * 60 * 12 * cooldownSplits;

                    bool boosted = Drops.AddBoost(login, factor, duration, cooldownRed, out boost);

                    updateComponents("You boosted! 🔥", true);
                    await sent.ModifyAsync(chooseMessage);
                }

                if (modifier == "now") await StartBoost();
                else while (true)
                    {
                        var reaction = await sent.WaitForButtonAsync(context.User, TimeSpan.FromSeconds(60));
                        if (reaction.TimedOut)
                        {
                            updateComponents("Timed out", true);
                            await sent.ModifyAsync(chooseMessage);
                            break;
                        }

                        await reaction.Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage);

                        if (reaction.Result.Id == "-dur" && durationSplits > 0) durationSplits--;
                        if (reaction.Result.Id == "-fac" && factorSplits > 1) factorSplits -= 2;
                        if (reaction.Result.Id == "-cool" && cooldownSplits > 0) cooldownSplits--;

                        if (reaction.Result.Id == "+dur" && (durationSplits + factorSplits + cooldownSplits) < memberAvailableSplits) durationSplits++;
                        if (reaction.Result.Id == "+fac" && (durationSplits + factorSplits + cooldownSplits) < memberAvailableSplits - 1) factorSplits += 2;
                        if (reaction.Result.Id == "+cool" && (durationSplits + factorSplits + cooldownSplits) < memberAvailableSplits) cooldownSplits++;

                        if (reaction.Result.Id == "start" || modifier == "now")
                        {
                            await StartBoost();
                            break;
                        }

                        updateComponents("Start Dropboost", false);
                        await sent.ModifyAsync(chooseMessage);
                    }
            }

        }

        [Description("Show your awards inventory and open your weekly award pack.")]
        [Command("awards")]
        [Synchronized]
        public async Task Awards(CommandContext context)
        {
            PermissionFlag flags = new PermissionFlag(Program.Feanor.GetFlagByMember(context.User));
            int login = Convert.ToInt32(BubbleWallet.GetLoginOfMember(context.User.Id.ToString()));
            var inv = BubbleWallet.GetAwardInventory(login);
            var received = BubbleWallet.GetReceivedAwards(login);
            var given = BubbleWallet.GetGivenAwards(login);
            var packLevel = BubbleWallet.GetAwardPackLevel(login);

            var message = new DiscordMessageBuilder();

            var embed = new DiscordEmbedBuilder();
            embed.WithTitle(context.Message.Author.Username + "s Award Inventory");
            embed.WithColor(DiscordColor.Magenta);

            embed.WithDescription("Awards are items that you can give on skribbl to special drawings.\nThe person who receives the award will see it in their gallery.\n");

            var awardReceivedString = string.Join("\n", received.GroupBy(i => i.award.Rarity).OrderBy(g => g.FirstOrDefault().award.Rarity).ToList().ConvertAll(group =>
            {
                var groupAward = group.FirstOrDefault().award;
                var count = group.Count();
                return "- " + count + " " + ((AwardRarity) groupAward.Rarity) + " awarded";
            }));
            embed.AddField("`🎁`  **Received Awards**", awardReceivedString.Length > 0 ? awardReceivedString : "You haven't received any awards yet.", true);

            var awardGivenString = string.Join("\n", given.GroupBy(i => i.award.Rarity).OrderBy(g => g.FirstOrDefault().award.Rarity).ToList().ConvertAll(group =>
            {
                var groupAward = group.FirstOrDefault().award;
                var count = group.Count();
                return "- " + count + " " + ((AwardRarity)groupAward.Rarity) + " given";
            }));
            embed.AddField("`👏`  **Given Awards** ", awardGivenString.Length > 0 ? awardGivenString : "You haven't given any awards yet.", true);

            var awardInvString = string.Join("\n", inv.GroupBy(i => i.award.Rarity).OrderBy(g => g.FirstOrDefault().award.Rarity).ToList().ConvertAll(group =>
            {
                var groupAward = group.FirstOrDefault().award;
                var distincts = System.Linq.Enumerable.DistinctBy(group, i => i.award.Id).ToList();
                var awards = string.Join("\n", distincts.ConvertAll(item => "> " + item.award.Name + " `(x" + group.Where(i => i.award.Id == item.award.Id).Count() + ")`"));
                return "‎ " + BubbleWallet.GetRarityIcon(groupAward.Rarity) +  " ‎  **" + ((AwardRarity)groupAward.Rarity) + "**\n" + awards + "\n" ;
            }));
           if(awardInvString.Length > 0) embed.AddField("\n_ _\n**Available Awards**", "You have following awards ready to gift on skribbl:\n_ _\n _ _" + awardInvString, false);
           else embed.AddField("\n_ _\n**Available Awards**", "You have no awards in your inventory. Open an award pack to get some!", false);

            embed.AddField("_ _\nAward Gallery", "You can see all your awarded drawings with `>gallery`");

            var cooldown = BubbleWallet.AwardPackCooldown(login);
            if(cooldown.TotalSeconds == 0)
            {
                var button = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "openPack", "✨ Open Award Pack");
                embed.AddField("Award Pack", "You can open your award pack now!");
                message.AddComponents(button);
            }
            else
            {
                embed.AddField("Award Pack", "You need to wait until <t:" + DateTimeOffset.UtcNow.Add(cooldown).ToUnixTimeSeconds()  + ":R> to open your next Award Pack.\nYou can open a pack once a week. \nThe more Bubbles you have collected in the past week, the better are the chances to get rare awards!");
            }

            embed.WithFooter("Award pack level: " + packLevel.Rarity + " (" + packLevel.CollectedBubbles + " Bubbles)");

            message.WithEmbed(embed.Build());

            var sent = await context.Message.RespondAsync(message);

            InteractivityResult<ComponentInteractionCreateEventArgs> result;
            if (sent.Components.Count() > 0) result = await sent.WaitForButtonAsync(context.User, TimeSpan.FromMinutes(1));
            else return;

            if (!result.TimedOut)
            {
                var newAwards = BubbleWallet.OpenAwardPack(login, packLevel);
                var builder = new DiscordInteractionResponseBuilder();

                builder.WithContent("### " + (context.Member is not null ? context.Member.DisplayName : context.User.Username) + " opened their " + packLevel.Rarity + " award pack:");

                foreach(var award in newAwards)
                {
                    builder.AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle("You pulled a **" + award.award.Name + "**!")
                        .WithDescription(award.award.Description + "\n \n" + BubbleWallet.GetRarityIcon(award.award.Rarity) + "  " + ((AwardRarity)award.award.Rarity) + " Award")
                        .WithThumbnail(award.award.Url)
                        .WithColor(DiscordColor.Magenta)
                    );
                }

                await result.Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, builder);
            }
            
            message.ClearComponents();
            await sent.ModifyAsync(message);
        }

        [Description("Show your received awards and the images.")]
        [Command("gallery")]
        public async Task Gallery(CommandContext context, int? id = null)
        {
            int login = Convert.ToInt32(BubbleWallet.GetLoginOfMember(context.User.Id.ToString()));
            var list = BubbleWallet.GetAwardGallery(login);

            if(id is null)
            {
                var items = list.ConvertAll(award =>
                    award.image is not null ?
                    $"- {BubbleWallet.GetRarityIcon(award.award.Rarity)} {award.award.Name}: [{award.image.Title}](https://eu2.contabostorage.com/45a0651c8baa459daefd432c0307bb5b:cloud/{context.User.Id}/{award.image.ImageId}/image.png)" :
                    $"- {BubbleWallet.GetRarityIcon(award.award.Rarity)} {award.award.Name}: No image found :(");

                var pages = list.Batch(25).ToList().Select((batch, index) =>
                {
                    var builder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Magenta)
                        .WithFooter("To view a single item, use >gallery [id]")
                        .WithTitle((context.Member is not null ? context.Member.DisplayName : context.User.Username) + "s Award Gallery");
                    var pageIndex = index * 25;

                    foreach (var item in batch) {

                        pageIndex++;
                        if (item.image is not null) builder.AddField(
                            "`#" + pageIndex + "` " + item.image.Title, 
                            $"> {BubbleWallet.GetRarityIcon(item.award.Rarity)}  {item.award.Name}\n> By {BubbleWallet.GetUsername(item.inv.OwnerLogin)}‎ ‎ ‎ \n> On <t:{(int)(item.inv.Date/1000)}:d>\n> [Show Image](https://eu2.contabostorage.com/45a0651c8baa459daefd432c0307bb5b:cloud/{context.User.Id}/{item.image.ImageId}/image.png)", 
                            true);
                        else builder.AddField("Unknown Image :(", $"> {BubbleWallet.GetRarityIcon(item.award.Rarity)}  {item.award.Name}\n> By {BubbleWallet.GetUsername(item.inv.OwnerLogin)}‎ ‎ ‎  \n> <t:{(int)(item.inv.Date / 1000)}:d>", true);
                    }
                    return new Page(embed: builder);
                });

                if(pages.Count() == 0)
                {
                    var builder = new DiscordEmbedBuilder()
                       .WithColor(DiscordColor.Magenta)
                       .WithFooter("To view a single item, use >gallery [id]")
                       .WithDescription("You haven't received any awards yet.\nPeople can give you awards when you're drawing on skribbl.")
                       .WithTitle((context.Member is not null ? context.Member.DisplayName : context.User.Username) + "s Award Gallery");
                    pages = new List<Page>() { new Page(embed: builder) };
                }

                await context.Client.GetInteractivity().SendPaginatedMessageAsync(context.Channel, context.User, pages);
            }
            else
            {
                id--;
                if (id < 0 || id > list.Count) await Program.SendEmbed(context.Channel, "Oopsie", "There's now image for this ID. Check >gallery again!");
                var item = list[(int)id];

                var embed = new DiscordEmbedBuilder()
                    .WithTitle(item.image.Title + " by " + context.User.Username)
                    .WithImageUrl($"https://eu2.contabostorage.com/45a0651c8baa459daefd432c0307bb5b:cloud/{context.User.Id}/{item.image.ImageId}/image.png")
                    .WithThumbnail(item.award.Url)
                    .WithColor(DiscordColor.Magenta)
                    .WithDescription($"‎‎{BubbleWallet.GetRarityIcon(item.award.Rarity)}  **{item.award.Name}**\n> {item.award.Description}\n> \\- {BubbleWallet.GetUsername(item.inv.OwnerLogin)} on <t:{(int)(item.inv.Date / 1000)}:d>\n");

                await context.RespondAsync(embed);
            }
              
        }


        //[Description("Set your unique typo lobby stream code.")]
        //[Command("streamcode")]
        //public async Task Streamcode(CommandContext context, string code = "")
        //{
        //    PermissionFlag perm = new PermissionFlag(Program.Feanor.GetFlagByMember(context.User));
        //    if (perm.Permanban)
        //    {
        //        await Program.SendEmbed(context.Channel, "So... you're one of the bad guys, huh?", "You're permabanned.");
        //        return;
        //    }

        //    string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());

        //    if (code.StartsWith("typoStrm_"))
        //    {
        //        await Program.SendEmbed(context.Channel, "Sneak over 9000?", "Your code may not start with the random identifier.");
        //        return;
        //    }

        //    PalantirContext ctx = new();

        //    if (ctx.Members.Any(m => m.Streamcode == code))
        //    {
        //        await Program.SendEmbed(context.Channel, ":/", "This code is already being used by someone else, sorry..");
        //        return;
        //    }

        //    ctx.Members.FirstOrDefault(m => m.Login.ToString() == login).Streamcode = code;
        //    ctx.SaveChanges();
        //    ctx.Dispose();


        //    await Program.SendEmbed(context.Channel, "Nice one!", code == "" ? "Your code has been reset. You'll be assigned random codes when streaming." : "Your code is now `" + code + "`. Dont forget to enable it on skribbl!");
        //}


    }
}
