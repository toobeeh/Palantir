using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Palantir
{
    public static class Events
    {
        public static int eventSceneDayValue = 350;
        public static List<EventEntity> GetEvents(bool active = true)
        {
            PalantirDbContext context = new PalantirDbContext();
            List<EventEntity> events = context.Events.ToList();
            if (active) events = events.Where(e => Convert.ToDateTime(e.ValidFrom) <= DateTime.Now && Convert.ToDateTime(e.ValidFrom).AddDays(e.DayLength) >= DateTime.Now).ToList();
            context.Dispose();
            return events;
        }

        public static List<EventDropEntity> GetEventDrops(List<EventEntity> fromEvents = null)
        {
            PalantirDbContext context = new PalantirDbContext();
            List<EventDropEntity> drops = context.EventDrops.ToList();
            if (fromEvents is object) drops = drops.Where(d => fromEvents.Any(e => e.EventID == d.EventID)).ToList();
            context.Dispose();
            return drops;
        }

        public static int GetRandomEventDropID()
        {
            List<EventEntity> events = GetEvents(true);
            if (events.Count <= 0) return 0;
            List<EventDropEntity> drops = GetEventDrops(events);
            drops.AddRange(Enumerable.Repeat(new EventDropEntity { EventDropID = 0 },drops.Count));
            int randInd = (new Random()).Next(0, drops.Count);
            return drops[randInd].EventDropID;
        }
        public static List<SpritesEntity> GetEventSprites(int eventDropID)
        {
            PalantirDbContext context = new PalantirDbContext();
            List<SpritesEntity> sprites = context.Sprites.Where(s => s.EventDropID == eventDropID).ToList();
            context.Dispose();
            return sprites;
        }

        public static bool EligibleForEventScene(string login, int eventID)
        {
            EventEntity evt = GetEvents().FirstOrDefault(evt => evt.EventID == eventID);
            DateTime eventStart = DateTime.Parse(evt.ValidFrom, new CultureInfo("en-CA"));
            DateTime eventEnd = eventStart.AddDays(evt.DayLength);
            int bubblesDuringEvent = BubbleWallet.GetCollectedBubblesInTimespan(eventStart, eventEnd, login);
            int eventSceneValue = evt.DayLength * eventSceneDayValue;
            return bubblesDuringEvent >= eventSceneValue;
        }


    }
}
