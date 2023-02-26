using Microsoft.EntityFrameworkCore;
using Palantir.Model;
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

        public static int GetRandomEventDropID()
        {
            List<Event> events = GetEvents(true);
            if (events.Count <= 0) return 0;
            List<EventDrop> drops = GetEventDrops(events);
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
            BubbleWallet.ChangeEventDropCredit(login, targetDropID, value);

            PalantirContext context = new PalantirContext();
            consumed.ForEach(drop => {
                   context.PastDrops.FirstOrDefault(past => past.DropId == drop.DropId && past.CaughtLobbyPlayerId == drop.CaughtLobbyPlayerId).EventDropId *= -1;
            });
            context.SaveChanges();
            context.Dispose();
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
