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


    }
}
