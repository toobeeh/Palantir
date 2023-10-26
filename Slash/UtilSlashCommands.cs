using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.SlashCommands;
using MoreLinq;
using Newtonsoft.Json;
using Palantir.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir.Slash
{
    internal class UtilSlashCommands : ApplicationCommandModule
    {
        public enum LeaderboardType
        {
            [ChoiceName("Bubbles")]
            bubbles,
            [ChoiceName("Drops")]
            drops
        }

        public enum StatisticsTimespan
        {
            [ChoiceName("Day")]
            day,
            [ChoiceName("Week")]
            week,
            [ChoiceName("Month")]
            month
        }
        public enum CalcMode
        {
            [ChoiceName("Bubbles")]
            bubbles,
            [ChoiceName("Rank")]
            rank,
            [ChoiceName("Sprite")]
            sprite
        }

        [SlashCommand("droprate", "Calculate the current rate in which in-game drops appear")]
        public async Task Droprate(InteractionContext context)
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

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                 "Current Drop Rate",
                 "ATM, drops appear in an average frequency of about " + mins + "min " + secs + "s.\n\n" + QuartzJobs.StatusUpdaterJob.currentOnlineIDs + " people are playing.\n\nFollowing boosts are active:\n" + boosts + "\n\nYou can boost once a week with `>dropboost`."
            )));
        }

        [SlashCommand("invite", "Connect a Palantir account to this server.")]
        public async Task Invite(InteractionContext context)
        {
            ObservedGuild guild = Program.Feanor.PalantirTethers.FirstOrDefault(g => g.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirEndpoint;
            if (guild is null)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                    "Aw, shoot :(",
                    "This server is not using Palantir yet :/\nVisit https://typo.rip#admin to find out how!")
                ));
            }
            else
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("https://typo.rip/i?invite=" + guild.ObserveToken));
            }
        }

        [SlashCommand("splits", "Show your earned boost splits.")]
        public async Task Splits(InteractionContext context)
        {
            PermissionFlag flags = new PermissionFlag(Program.Feanor.GetFlagByMember(context.User));
            int login = Convert.ToInt32(BubbleWallet.GetLoginOfMember(context.User.Id.ToString()));

            var memberSplits = BubbleWallet.GetMemberSplits(login, flags);

            var message = new DiscordEmbedBuilder();
            message.WithTitle(context.User.Username + "s Split Achievements");
            message.WithColor(DiscordColor.Magenta);

            if (memberSplits.Count == 0)
            {
                message.WithDescription("You haven't earned any Splits yet :(\n\nSplits are used to make your Drop Boosts more powerful.\nYou get them by occasional giveaways/challenges or by competing in Drop Leagues.");
            }
            else
            {
                message.AddField(memberSplits.Sum(s => s.Value).ToString(), "total earned Splits\n_ _");

                memberSplits.ForEach(split =>
                {
                    message.AddField("➜ " + split.Name + (split.RewardDate != null && split.RewardDate != "" ? "  `" + split.RewardDate + "`" : "") + (split.Expired ? " / *expired*" : ""), split.Description + "\n *worth " + split.Value + " Splits*" + (split.Comment != null && split.Comment.Length > 0 ? " ~ `" + split.Comment + "`" : ""));
                });

                message.WithDescription("You can use your Splits to customize your Drop Boosts.\nChoose the boost intensity, duration or cooldown individually when using `>dropboost`\n_ _");
            }

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(message));
        }

        [SlashCommand("statistics", "Show your earned bubbles over time.")]
        public async Task Stat(InteractionContext context, [Option("Time", "The time span of the statistics")] StatisticsTimespan span = StatisticsTimespan.day)
        {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Processing data..."));

            CultureInfo iv = CultureInfo.InvariantCulture;
            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
            string msg = "```css\n";
            QuartzJobs.BubbleTrace trace;
            if (span == StatisticsTimespan.week)
            {
                trace = new QuartzJobs.BubbleTrace(login, 7 * 10);
                trace.History = trace.History.Where(
                    t => t.Key.DayOfWeek == DayOfWeek.Monday || t.Key == trace.History.Keys.Min() || t.Key == trace.History.Keys.Max()
                    ).ToDictionary();
                msg += " Weekly";
            }
            else if (span == StatisticsTimespan.month)
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

            await (await context.GetOriginalResponseAsync()).ModifyAsync(msg);
        }

        [SlashCommand("themes", "Show verified typo themes")]
        public async Task Themes(InteractionContext context, [Option("Theme", "Show details of a specific theme")] long id = 0)
        {
            var embed = new DiscordEmbedBuilder();
            PalantirContext db = new();
            List<Theme> themes = db.Themes.Where(theme => !String.IsNullOrEmpty(theme.Theme1)).ToList();
            db.Dispose();

            if (id <= 0 || id > themes.Count)
            {
                embed.WithTitle("Listing all **Typo Themes**:");
                embed.WithDescription("Click a link to add the theme or use `>themes [id]` to view theme details!\nTo add your own theme, contact a Palantir mod.");
                themes.ForEach((theme, index) =>
                {
                    embed.AddField("➜ " + theme.Name, "#" + (index + 1) + " - by `" + theme.Author + "` - https://typo.rip/t?ticket=" + theme.Ticket);
                });
            }
            else
            {
                Theme theme = themes[(int)id - 1];
                embed.WithTitle("Theme **" + theme.Name + "**");
                embed.WithDescription(theme.Description);
                embed.AddField("Add the theme:", "https://typo.rip/t?ticket=" + theme.Ticket);
                embed.WithFooter("Created by " + theme.Author);
                embed.WithImageUrl(theme.ThumbnailLanding);
            }

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("calc", "Calculate how long it takes to reach bubbles, a sprite or a leaderboard rank")]
        public async Task Calc(InteractionContext context, [Option("Mode", "What you want to reach with your bubbles")] CalcMode mode = CalcMode.bubbles, [Option("Goal", "The amount/rank/sprite to reach")] long target = 0)
        {
            double hours = 0;

            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
            switch (mode)
            {
                case CalcMode.sprite:
                    List<Sprite> available = BubbleWallet.GetAvailableSprites();
                    Sprite sprite = available.FirstOrDefault(s => (long)s.ID == target);
                    if (sprite.EventDropID > 0) { await Program.SendEmbed(context.Channel, "This is an event sprite!", "It can only be bought with event drops."); return; }
                    hours = ((double)sprite.Cost - BubbleWallet.CalculateCredit(login, context.User.Id.ToString())) / 360;
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                        "🔮  Time to get " + sprite.Name + ":",
                        (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h "
                        + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                        + TimeSpan.FromHours(hours).Seconds.ToString() + "s on skribbl.io left."
                    )));
                    break;
                case CalcMode.bubbles:
                    hours = (double)target / 360;
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                       "🔮  Time to get " + target + " more Bubbles:",
                        (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h "
                        + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                        + TimeSpan.FromHours(hours).Seconds.ToString() + "s on skribbl.io left."
                    )));
                    break;
                case CalcMode.rank:
                    List<Model.Member> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => m.Bubbles).Where(m => m.Bubbles > 0).ToList();
                    hours = ((double)members[Convert.ToInt32(target) - 1].Bubbles - BubbleWallet.GetBubbles(login)) / 360;
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                       "🔮  Time catch up #" + target + ":",
                         (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h "
                        + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                        + TimeSpan.FromHours(hours).Seconds.ToString() + "s on skribbl.io left."
                    )));
                    break;
                default:
                    await Program.SendEmbed(context.Channel, "🔮  Calculate following things:", "➜ `/calc sprite 1` Calculate remaining hours to get Sprite 1 depending on your actual Bubbles left.\n➜ `/calc bubbles 1000` Calculate remaining hours to get 1000 more bubbles.\n➜ `/calc rank 4` Calculate remaining hours to catch up the 4rd ranked member.");
                    break;
            }
        }

        [SlashCommand("leaderboard", "See who's got the most bubbles.")]
        public async Task Leaderboard(InteractionContext context, [Option("Ranking", "The leaderboard ranking type")] LeaderboardType type = LeaderboardType.bubbles)
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());
            Program.Feanor.UpdateMemberGuilds();
            //DiscordMessage leaderboard = await context.RespondAsync("`⏱️` Loading members of `" + context.Guild.Name + "`...");
            var interactivity = Program.Interactivity;
            List<Model.Member> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => (type == LeaderboardType.drops ? m.Drops : m.Bubbles)).Where(m => m.Bubbles > 0).ToList();
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
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("`⏱️` Loading members of `" + context.Guild.Name + "`..."));
            var msg = await context.GetOriginalResponseAsync();

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

        [SlashCommand("inventory", "View an overview of your Palantir profile")]
        public async Task Inventory(InteractionContext context, [Option("Column-size", "Set how many sprites are shown per column")] long batchsize = 7)
        {
            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
            int drops = BubbleWallet.GetDrops(login, context.User.Id.ToString());
            int bubbles = BubbleWallet.GetBubbles(login);
            int credit = BubbleWallet.CalculateCredit(login, context.User.Id.ToString());
            List<SpriteProperty> inventory = BubbleWallet.GetInventory(login).OrderBy(s => s.ID).ToList();
            PermissionFlag perm = new PermissionFlag(Program.Feanor.GetFlagByMember(context.User));
            int splits = BubbleWallet.GetMemberSplits(Convert.ToInt32(login), perm).Sum(s => s.Value);
            int regLeagueDrops = League.GetLeagueEventDropWeights(context.User.Id.ToString()).Count;
            int leagueDrops = regLeagueDrops + League.GetLeagueDropWeights(context.User.Id.ToString()).Count;

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Magenta)
                .WithTitle("🔮  " + context.User.Username + "s Inventory");

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
            if (perm.CloudUnlimited || perm.Patron) flags += "`📦 Unlimited cloud storage`\n";
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
                var comboPath = SpriteComboImage.GenerateImage(
                    SpriteComboImage.GetSpriteSources(
                        inventory.Where(s => s.Activated).OrderBy(s => s.Slot).Select(s => s.ID).ToArray(),
                        BubbleWallet.GetMemberRainbowShifts(login)
                    ));
                var s3 = await Program.S3.UploadPng(comboPath, context.User.Id + "/sprite-combo-" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString());
                embed.ImageUrl = s3;
            }

            DiscordEmbedField sleft = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            DiscordEmbedField smiddle = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            DiscordEmbedField sright = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            var spritebatches = sprites.Batch((int)batchsize * 3);

            if (inventory.Count < 5) embed.AddField("Command help: ", "Use `>use [id]` to select your Sprite!\n`>use 0` will set no Sprite.\nBuy a Sprite with `>buy [id]`.\nSpecial Sprites :sparkles: replace your whole avatar! \nRainbow Sprites :rainbow: can be color-customized! (`>rainbow`) ");
            embed.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)");

            DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder();
            DiscordMessageBuilder responseEdit = new DiscordMessageBuilder();

            Action<string, bool> setComponents = (string navText, bool disabled) =>
            {
                response.ClearComponents();
                responseEdit.ClearComponents();
                response.AddComponents(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "last", "Previous", disabled),
                    new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "nav", navText, true),
                    new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "next", "Next", disabled));
                responseEdit.AddComponents(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "last", "Previous", disabled),
                   new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "nav", navText, true),
                   new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "next", "Next", disabled));
            };
            setComponents("Navigate Sprites", false);
            DiscordMessage sent = null;
            InteractivityResult<DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs> result;
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
                response.AddEmbed(embed.Build());
                responseEdit.AddEmbed(embed.Build());
                if (sent is null)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
                    sent = await context.GetOriginalResponseAsync();
                }
                else await sent.ModifyAsync(responseEdit);

                result = await Program.Interactivity.WaitForButtonAsync(sent, context.User, TimeSpan.FromMinutes(2));
                if (!result.TimedOut)
                {
                    await result.Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage);
                    direction = result.Result.Id == "next" ? 1 : -1;
                }
            }
            while (!result.TimedOut);
            setComponents("Navigate Sprites (" + firstbatch.Count() + "/" + spritebatches.Flatten().Count() + ")", true);
            await sent.ModifyAsync(responseEdit);
        }

        [SlashCommand("ping", "Show a ping statistic.")]
        public async Task Ping(InteractionContext context)
        {
            System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
            long discordRTT = ping.Send("discord.gg", 100).RoundtripTime;

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

            embed.Title = "Latency results:";
            embed.AddField("`🗄️` Database singe read", Program.Feanor.DatabaseReadTime(context.User.Id.ToString(), 1) + "ms");
            embed.AddField("`🗂️` Database average for 100 reads", Program.Feanor.DatabaseReadTime(context.User.Id.ToString(), 100) + "ms");
            embed.AddField("`🌐` Discord API request", Program.Client.Ping + "ms");
            embed.AddField("`⌛` Discord.gg ping RTT", discordRTT + "ms");
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("about", "Show some nice information about the bot.")]
        public async Task About(InteractionContext context)
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
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("dropboost", "Boost the current droprate for a while")]
        public async Task Dropboost(InteractionContext context, [Option("factor", "Amount of splits to increase boost factor")] long factorSplits = 0, [Option("duration", "Amount of splits to increase boost duration")] long durationSplits = 0, [Option("cooldown", "Amount of splits to lower boost cooldown")] long cooldownSplits = 0, [Option("instant", "Set to start the boost instantly")] bool now = false)
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


                var chooseMessage = new DiscordInteractionResponseBuilder()
                    .WithContent("> **Customize your Dropboost**\n> \n> You have `" + memberAvailableSplits + "` Splits available.\n> Using splits, you can power up your boost. \n> Use `>splits` to learn more about your Splits.\n> \n> `🔥 Intensity: +2 Splits => +0.1 factor`\n> `⌛ Duration:  +1 Split  => +20min boost`\n> `💤 Cooldown:  +1 Split  => -12hrs until next boost`\n> _ _");
                var chooseMessageEdit = new DiscordMessageBuilder()
                    .WithContent("> **Customize your Dropboost**\n> \n> You have `" + memberAvailableSplits + "` Splits available.\n> Using splits, you can power up your boost. \n> Use `>splits` to learn more about your Splits.\n> \n> `🔥 Intensity: +2 Splits => +0.1 factor`\n> `⌛ Duration:  +1 Split  => +20min boost`\n> `💤 Cooldown:  +1 Split  => -12hrs until next boost`\n> _ _");

                Action<string, bool> updateComponents = (string starttext, bool disable) =>
                {
                    chooseMessage.ClearComponents();
                    chooseMessageEdit.ClearComponents();

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

                    chooseMessageEdit
                        .AddComponents(minusFactor, labelFactor, plusFactor)
                        .AddComponents(minusDur, labelDur, plusDur)
                        .AddComponents(minusCool, labelCool, plusCool)
                        .AddComponents(start);
                };

                updateComponents("Start Dropboost", false);

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, chooseMessage);
                var sent = await context.GetOriginalResponseAsync();

                async Task StartBoost()
                {
                    DropBoost boost;

                    factor = factor + factorSplits * 0.05;
                    long duration = (60 + durationSplits * 20) * 60;
                    long cooldownRed = 60 * 60 * 12 * cooldownSplits;

                    bool boosted = Drops.AddBoost(login, factor, Convert.ToInt32(duration), Convert.ToInt32(cooldownRed), out boost);

                    updateComponents("You boosted! 🔥", true);
                    await sent.ModifyAsync(chooseMessageEdit);
                }

                if (now) await StartBoost();
                else while (true)
                    {

                        var reaction = await Program.Interactivity.WaitForButtonAsync(sent, context.User, TimeSpan.FromSeconds(60));
                        if (reaction.TimedOut)
                        {
                            updateComponents("Timed out", true);
                            await sent.ModifyAsync(chooseMessageEdit);
                            break;
                        }

                        await reaction.Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage);

                        if (reaction.Result.Id == "-dur" && durationSplits > 0) durationSplits--;
                        if (reaction.Result.Id == "-fac" && factorSplits > 1) factorSplits -= 2;
                        if (reaction.Result.Id == "-cool" && cooldownSplits > 0) cooldownSplits--;

                        if (reaction.Result.Id == "+dur" && (durationSplits + factorSplits + cooldownSplits) < memberAvailableSplits) durationSplits++;
                        if (reaction.Result.Id == "+fac" && (durationSplits + factorSplits + cooldownSplits) < memberAvailableSplits - 1) factorSplits += 2;
                        if (reaction.Result.Id == "+cool" && (durationSplits + factorSplits + cooldownSplits) < memberAvailableSplits) cooldownSplits++;

                        if (reaction.Result.Id == "start" || now)
                        {
                            await StartBoost();
                            break;
                        }

                        updateComponents("Start Dropboost", false);
                        await sent.ModifyAsync(chooseMessageEdit);
                    }
            }
        }
    }
}
