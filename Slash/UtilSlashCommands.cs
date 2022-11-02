using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir.Slash
{
    internal class UtilSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("droprate", "Calculate the current rate in which in-game drops appear")]
        public async Task Droprate(InteractionContext context)
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
                + " (" + (Math.Round((boost.StartUTCS + boost.DurationS - now) / 60000, 1) + "min left)")).ToDelimitedString("\n");
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

        [SlashCommand("dropboost", "Boost the current droprate for a while")]
        public async Task Dropboost(InteractionContext context, [Option("factor", "Amount of splits to increase boost factor")] int factorSplits = 0, [Option("duration", "Amount of splits to increase boost duration")] int durationSplits = 0, [Option("cooldown", "Amount of splits to lower boost cooldown")] int cooldownSplits = 0, [Option("instant", "Set to start the boost instantly")] bool now = false)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
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

                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,chooseMessage);
                var sent = await context.GetOriginalResponseAsync();

                async Task StartBoost()
                {
                    BoostEntity boost;

                    factor = factor + factorSplits * 0.05;
                    int duration = (60 + durationSplits * 20) * 60;
                    int cooldownRed = 60 * 60 * 12 * cooldownSplits;

                    bool boosted = Drops.AddBoost(login, factor, duration, cooldownRed, out boost);

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
