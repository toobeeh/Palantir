using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Palantir
{
    public static class Events
    {
        public static List<EventEntity> GetEvents()
        {
            PalantirDbContext context = new PalantirDbContext();
            List<EventEntity> events = context.Events.ToList();
            context.Dispose();
            return events;
        }

        public static List<EventDropEntity> GetEventDrops()
        {
            PalantirDbContext context = new PalantirDbContext();
            List<EventDropEntity> drops = context.EventDrops.ToList();
            context.Dispose();
            return drops;
        }


        public static int GetRandomEventDropID()
        {
            if (GetEvents().Count <= 0) return 0;
            List<EventDropEntity> drops = GetEventDrops();
            int randInd = (new Random()).Next(0, drops.Count);
            return drops[randInd].EventDropID;
        }



    }
}
