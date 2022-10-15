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
    [SlashCommandGroup(">", "Commands for Palantir")]
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

    }
}
