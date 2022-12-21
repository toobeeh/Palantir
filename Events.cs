using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Palantir
{
    public static class Events
    {
        public static int eventSceneDayValue = 500;
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
            EventEntity evt = GetEvents(false).FirstOrDefault(evt => evt.EventID == eventID);
            DateTime eventStart = Convert.ToDateTime(evt.ValidFrom);
            DateTime eventEnd = eventStart.AddDays(evt.DayLength);
            int bubblesDuringEvent = BubbleWallet.GetCollectedBubblesInTimespan(eventStart, eventEnd.AddDays(-1), login);
            int eventSceneValue = evt.DayLength * eventSceneDayValue;
            if (eventID == 15) eventSceneValue = 7000;
            return bubblesDuringEvent >= eventSceneValue;
        }

        public static double GetAvailableLeagueTradeDrops(string userid, EventEntity evt, out List<PastDropEntity> consumable)
        {
            var drops = GetEventDrops(new List<EventEntity>() { evt }).ConvertAll(drop => drop.EventDropID).ToArray();
            var caught = League.GetLeagueEventDrops(userid, drops);
            consumable = caught;

            var weight = caught.Sum(result => League.Weight(result.LeagueWeight / 1000.0) / 100);
            return weight;
        }

        public static double GetCollectedEventDrops(string userid, EventEntity evt)
        {
            var drops = GetEventDrops(new List<EventEntity>() { evt }).ConvertAll(drop => drop.EventDropID).ToArray();

            PalantirDbContext palantirDbContext = new PalantirDbContext();
            var userDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE EventDropID < 0 AND CaughtLobbyPlayerID == \"{userid}\"")
                .ToList();
            palantirDbContext.Dispose();

            double sum = 0;
            foreach (var item in userDrops)
            {
                if (drops.Contains(item.EventDropID * -1) || drops.Contains(item.EventDropID))
                {
                    if (item.LeagueWeight > 0) sum += (League.Weight(item.LeagueWeight / 1000.0) / 100);
                    else sum++;
                }
            }

            return sum;
        }

        public static int TradeLeagueEventDrops(List<PastDropEntity> consumed, int targetDropID, string login)
        {
            int value = Convert.ToInt32(consumed.Sum(drop => League.Weight(drop.LeagueWeight / 1000.0) / 100));
            BubbleWallet.ChangeEventDropCredit(login, targetDropID, value);

            PalantirDbContext context = new PalantirDbContext();
            consumed.ForEach(drop => {
                   context.PastDrops.FirstOrDefault(past => past.DropID == drop.DropID && past.CaughtLobbyPlayerID == drop.CaughtLobbyPlayerID).EventDropID *= -1;
            });
            context.SaveChanges();
            context.Dispose();
            return value;
        }

        public static double CurrentGiftLossRate(List<SpritesEntity> eventsprites, double collected)
        {
            int totalNeeded = eventsprites.ConvertAll(s => s.Cost).Sum();
            double ratio = collected / totalNeeded;

            if (ratio > 4) return 0.8;
            else if (ratio < 0.5) return 0.1;
            else return ratio / 5;
        }

    }
}
