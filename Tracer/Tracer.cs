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
            Console.WriteLine("Tracer for Bubbles is loading values from DB...");
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
}
