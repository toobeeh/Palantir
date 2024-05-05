using System;
using System.Collections.Generic;
using System.Linq;
using Quartz;
using Quartz.Impl;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System.Net;
using Palantir.Model;

namespace Palantir.QuartzJobs
{
    public class TracerJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Tracer for Bubbles is loading values from DB...");
            Model.PalantirContext dbcontext = new PalantirContext();
            List<Model.Member> members = dbcontext.Members.ToList();
            List<Model.BubbleTrace> dailyMemberTraces = new List<Model.BubbleTrace>();
            Console.WriteLine("Deleting daily duplicates...");
            dbcontext.BubbleTraces.ToList().ForEach(t =>
            {
                if (t.Date == DateTime.UtcNow.ToShortDateString()) dbcontext.BubbleTraces.Remove(t);
            });
            await dbcontext.SaveChangesAsync();

            Console.WriteLine("Creating trace entities...");
            int maxid = 0;
            try { maxid = dbcontext.BubbleTraces.OrderByDescending(t => t.Id).ToList()[0].Id + 1; }
            catch { }
            members.ForEach(m =>
            {
                Model.BubbleTrace trace = new Model.BubbleTrace();
                trace.Login = m.Login;
                trace.Date = DateTime.UtcNow.ToShortDateString();
                trace.Bubbles = m.Bubbles;
                trace.Id = ++maxid;
                dailyMemberTraces.Add(trace);
            });

            Console.WriteLine("Writing trace entities...");
            dbcontext.BubbleTraces.AddRange(dailyMemberTraces);
            await dbcontext.SaveChangesAsync();

            Console.WriteLine("All done!");
            dbcontext.Dispose();
        }
    }

    public class StatusUpdaterJob : IJob
    {
        public static int currentOnlineIDs;
        public async Task Execute(IJobExecutionContext context)
        {
            /*List<string> onlineIDs = new List<string>();
            PalantirContext dbcontext = new PalantirContext();
            dbcontext.Statuses.ToList().ForEach(status =>
            {
                string id = JsonConvert.DeserializeObject<PlayerStatus>(status.Status1).PlayerMember.UserID;
                if (!onlineIDs.Contains(id)) onlineIDs.Add(id);
            });
            int count = onlineIDs.Count();
            StatusUpdaterJob.currentOnlineIDs = count;
            dbcontext.Dispose();
            double boost = Math.Round(Drops.GetCurrentFactor(), 1);
            string status = " " + count + " ppl " + (boost <= 1 ? "on skribbl.io" : "(" + boost + " Boost)");
            await Program.Client.UpdateStatusAsync(new DiscordActivity(status, ActivityType.Watching));*/
            await Program.Feanor.UpdatePatrons(Program.Servant);
        }
    }

    public class PictureUpdaterJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await Program.RefreshPicture();
        }
    }

    public class BubbleTrace
    {
        public Dictionary<DateTime, int> History { get; set; }
        public BubbleTrace(string login, int? dayLimit = null)
        {
            History = new Dictionary<DateTime, int>();
            PalantirContext context = new PalantirContext();
            List<Model.BubbleTrace> traces = context.BubbleTraces.Where(t => t.Login.ToString() == login).ToList();
            if (dayLimit is null) dayLimit = traces.Count;
             Dictionary<DateTime, int> combined = new Dictionary<DateTime, int>();
            traces.OrderBy(k => k.Bubbles);
            for (int daysAgo = (int)dayLimit; daysAgo > 1; daysAgo--)
            {
                DateTime historyPoint = DateTime.Now.AddDays(-1 * daysAgo);
                int lastEarlier = 0;
                while (lastEarlier+1 < traces.Count && Convert.ToDateTime(traces[lastEarlier].Date) < historyPoint ) lastEarlier++;
                if (!History.ContainsKey(historyPoint)) History.Add(historyPoint.AddDays(1), traces[lastEarlier].Bubbles);
            }
            context.Dispose();
        }
    }

}
