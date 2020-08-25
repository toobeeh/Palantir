using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;

namespace Palantir
{
    class BubbleWallet
    {
        public static Dictionary<string, DateTime> Ticks = new Dictionary<string, DateTime>();
        public static void AddBubble(string login)
        {
            // Remove all ticks that passed the max tick interval
            Ticks.Where(tick => (tick.Value > DateTime.UtcNow.AddSeconds(-10))).ToList().ForEach(tick => Ticks.Remove(tick.Key));

            if (Ticks.ContainsKey(login)) return;

            Ticks.Add(login, DateTime.Now);

            PalantirDbContext context = new PalantirDbContext();
            MemberEntity entity = context.Members.FirstOrDefault(s => s.Login == login);

            if (entity != null)
            {
                entity.Bubbles++;
                context.SaveChanges();
            }
            context.SaveChanges();
            context.Dispose();
        }

        public static int GetBubbles(string login)
        {
            PalantirDbContext context = new PalantirDbContext();
            MemberEntity entity = context.Members.FirstOrDefault(s => s.Login == login);
            int bubbles = 0;

            if (entity != null)
            {
                bubbles = entity.Bubbles;
            }
            context.SaveChanges();
            context.Dispose();

            return bubbles;
        }
    }
}
