using Microsoft.EntityFrameworkCore;
using Palantir.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Palantir
{
    public class ProgressiveEventDropInfo
    {
        public EventDrop drop;
        public bool isRevealed;
        public long revealTimeStamp;
        public long endTimestamp;
    }

    public static class Events
    {
        public static int eventSceneDayValue = 500;
        public static List<Event> GetEvents(bool active = true)
        {
            PalantirContext context = new PalantirContext();
            List<Event> events = context.Events.ToList();
            if (active) events = events.Where(e => Convert.ToDateTime(e.ValidFrom) <= DateTime.Now && Convert.ToDateTime(e.ValidFrom).AddDays(e.DayLength) >= DateTime.Now).ToList();
            context.Dispose();
            return events;
        }

        public static List<EventDrop> GetEventDrops(List<Event> fromEvents = null)
        {
            PalantirContext context = new PalantirContext();
            List<EventDrop> drops = context.EventDrops.ToList();
            if (fromEvents is object) drops = drops.Where(d => fromEvents.Any(e => e.EventId == d.EventId)).ToList();
            context.Dispose();
            return drops;
        }

        public static List<ProgressiveEventDropInfo> GetProgressiveEventDrops(Event evt)
        {
            var now = DateTime.Now;

            PalantirContext ctx = new();
            var drops = ctx.EventDrops.Where(e => e.EventId == evt.EventId).OrderBy(d => d.EventDropId).ToList();
            ctx.Dispose();

            var daysPerDrop = evt.DayLength / drops.Count;
            var lastDropRemainder = evt.DayLength % drops.Count;

            var daySplits = Enumerable.Repeat(daysPerDrop, drops.Count).ToList();
            for(int i=0; i<daySplits.Count && lastDropRemainder > 0; i++)
            {
                daySplits[i]++;
                lastDropRemainder--;
            }

            var startDate = Convert.ToDateTime(evt.ValidFrom);
            var timeSinceStart = startDate.Subtract(now);
            var daysPassed = timeSinceStart.TotalDays;

            var dropsInfo = drops.ConvertAll(drop =>
            {
                var index = drops.FindIndex(d => d.EventDropId == drop.EventDropId);
                var requiredDays = daySplits.Take(index).Sum();
                var activeDays = daySplits[index];

                var info = new ProgressiveEventDropInfo
                {
                    drop= drop,
                    isRevealed= (startDate > now) && (daysPassed > requiredDays),
                    revealTimeStamp= new DateTimeOffset(startDate.AddDays(requiredDays)).ToUnixTimeSeconds(),
                    endTimestamp = new DateTimeOffset(startDate.AddDays(requiredDays + activeDays)).ToUnixTimeSeconds()
                };
                return info;
            });
            
            return dropsInfo;
        }

        public static int GetRandomEventDropID()
        {
            List<Event> events = GetEvents(true);
            if (events.Count <= 0) return 0;
            List<EventDrop> drops;

            if (events[0].Progressive == 1)
            {
                var progressiveDrops = GetProgressiveEventDrops(events[0]);
                drops = progressiveDrops.Where(d => d.isRevealed).Select(d => d.drop).ToList();
            }
            else
            {
                drops = GetEventDrops(events);
            }

            drops.AddRange(Enumerable.Repeat(new EventDrop { EventDropId = 0 },drops.Count));
            int randInd = (new Random()).Next(0, drops.Count);
            return drops[randInd].EventDropId;
        }
        public static List<Model.Sprite> GetEventSprites(int eventDropID)
        {
            PalantirContext context = new PalantirContext();
            List<Model.Sprite> sprites = context.Sprites.Where(s => s.EventDropId == eventDropID).ToList();
            context.Dispose();
            return sprites;
        }

        public static bool EligibleForEventScene(string login, int eventID)
        {
            Event evt = GetEvents(false).FirstOrDefault(evt => evt.EventId == eventID);
            DateTime eventStart = Convert.ToDateTime(evt.ValidFrom);
            DateTime eventEnd = eventStart.AddDays(evt.DayLength);
            int bubblesDuringEvent = BubbleWallet.GetCollectedBubblesInTimespan(eventStart, eventEnd.AddDays(-1), login);
            int eventSceneValue = evt.DayLength * eventSceneDayValue;
            if (eventID == 15) eventSceneValue = 7000;
            return bubblesDuringEvent >= eventSceneValue;
        }

        public static double GetAvailableLeagueTradeDrops(string userid, Event evt, out List<PastDrop> consumable)
        {
            var drops = GetEventDrops(new List<Event>() { evt }).ConvertAll(drop => drop.EventDropId).ToArray();
            var caught = League.GetLeagueEventDrops(userid, drops);
            consumable = caught;

            var weight = caught.Sum(result => League.Weight(result.LeagueWeight / 1000.0) / 100);
            return weight;
        }

        public static Dictionary<EventDrop, double> GetAvailableProgressiveLeagueTradeDrops(string userid, Event evt, out System.Collections.Concurrent.ConcurrentDictionary<EventDrop, List<PastDrop>> consumable)
        {
            var drops = GetEventDrops(new List<Event>() { evt });
            var dropIds = drops.ConvertAll(drop => drop.EventDropId).ToArray();
            var caught = League.GetLeagueEventDrops(userid, dropIds);

            consumable = new();
            foreach(var c in caught)
            {
                var drop = drops.FirstOrDefault(drop => drop.EventDropId == c.EventDropId);
                consumable.AddOrUpdate(drop, drop => new List<PastDrop> { c }, (drop, list) => { list.Add(c); return list; });
            }

            var weights = new Dictionary<EventDrop, double>();
            foreach(var key in consumable.Keys)
            {
                weights[key] = consumable[key].Sum(result => League.Weight(result.LeagueWeight / 1000.0) / 100);
            }
            return weights;
        }

        public static double GetCollectedEventDrops(string userid, Event evt)
        {
            var drops = GetEventDrops(new List<Event>() { evt }).ConvertAll(drop => drop.EventDropId).ToArray();

            PalantirContext palantirDbContext = new PalantirContext();
            var userDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE (EventDropID < 0 OR EventDropID > 0) AND CaughtLobbyPlayerID = '{userid}'")
                .ToList();
            palantirDbContext.Dispose();

            double sum = 0;
            foreach (var item in userDrops)
            {
                if (drops.Contains(item.EventDropId * -1) || drops.Contains(item.EventDropId))
                {
                    if (item.LeagueWeight > 0) sum += (League.Weight(item.LeagueWeight / 1000.0) / 100);
                    else sum++;
                }
            }

            return sum;
        }

        public static int TradeLeagueEventDrops(List<PastDrop> consumed, int targetDropID, string login)
        {
            int value = Convert.ToInt32(consumed.Sum(drop => League.Weight(drop.LeagueWeight / 1000.0) / 100));

            try
            {

                PalantirContext context = new PalantirContext();
                consumed.ForEach(drop => {
                    context.PastDrops.FirstOrDefault(past => past.DropId == drop.DropId && past.CaughtLobbyPlayerId == drop.CaughtLobbyPlayerId).EventDropId *= -1;
                });
                context.SaveChanges();
                context.Dispose();
            }
            catch
            {
                return -1;
            }

            BubbleWallet.ChangeEventDropCredit(login, targetDropID, value);
            return value;
        }

        public static double CurrentGiftLossRate(List<Model.Sprite> eventsprites, double collected)
        {
            int totalNeeded = eventsprites.ConvertAll(s => s.Cost).Sum();
            double ratio = collected / totalNeeded;

            double loss = ratio / 5 + 0.1;
            if (loss < 0.2) loss = 0.2;
            if (loss > 0.8) loss = 0.8;

            return loss;
        }

    }
}
