using System;
using System.Collections.Generic;
using System.Linq;
using Quartz;
using Quartz.Impl;
using System.Threading.Tasks;

namespace Palantir.Tracer
{
    public class TracerJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Tracer for Bubbles is loading values from DB...");
            PalantirDbContext dbcontext = new PalantirDbContext();
            List<MemberEntity> members = dbcontext.Members.ToList();
            List<BubbleTraceEntity> dailyMemberTraces = new List<BubbleTraceEntity>();
            Console.WriteLine("Deleting daily duplicates...");
            dbcontext.BubbleTraces.ToList().ForEach(t =>
            {
                if (t.Date == DateTime.UtcNow.ToShortDateString()) dbcontext.BubbleTraces.Remove(t);
            });
            await dbcontext.SaveChangesAsync();

            Console.WriteLine("Creating trace entities...");
            int maxid = 0;
            try { maxid = dbcontext.BubbleTraces.OrderByDescending(t => t.ID).ToList()[0].ID + 1; }
            catch { }
            members.ForEach(m =>
            {
                BubbleTraceEntity trace = new BubbleTraceEntity();
                trace.Login = m.Login;
                trace.Date = DateTime.UtcNow.ToShortDateString();
                trace.Bubbles = m.Bubbles;
                trace.ID = ++maxid;
                dailyMemberTraces.Add(trace);
            });

            Console.WriteLine("Writing trace entities...");
            dbcontext.BubbleTraces.AddRange(dailyMemberTraces);
            await dbcontext.SaveChangesAsync();

            Console.WriteLine("All done!");
            dbcontext.Dispose();
        }
    }

    public class BubbleTrace
    {
        public Dictionary<DateTime, int> History { get; private set; }
        public BubbleTrace(string login, int dayLimit)
        {
            History = new Dictionary<DateTime, int>();
            PalantirDbContext context = new PalantirDbContext();
            List<BubbleTraceEntity> traces = context.BubbleTraces.Where(t => t.Login == login && Convert.ToDateTime(t.Date) <= DateTime.UtcNow.AddDays(dayLimit * -1)).ToList();
            Dictionary<DateTime, int> combined = new Dictionary<DateTime, int>();
            for(int daysAgo = dayLimit; daysAgo >= 0; daysAgo--)
            {
                DateTime historyPoint = DateTime.Now.AddDays(-1 * daysAgo);
                traces.Sort((first, second) =>
                {
                    DateTime firstD = Convert.ToDateTime(first.Date);
                    DateTime secondD = Convert.ToDateTime(second.Date);
                    if ((firstD - historyPoint).Days == 0 || (firstD - historyPoint).Days < 0 && firstD < secondD) return -1;
                    else if ((secondD - historyPoint).Days == 0 || (secondD - historyPoint).Days < 0 && secondD < firstD) return 1;
                    else return -1;
                });
                History.Add(Convert.ToDateTime(traces.First().Date), traces.First().Bubbles);
            }
            context.Dispose();
        }
    }

}
