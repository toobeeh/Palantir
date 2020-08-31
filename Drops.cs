using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace Palantir
{
    public static class Drops
    {
        private static Thread Dropper = null;
        public static void StartDropping()
        {
            if (Dropper is null)
            {
                Dropper = new Thread(new ThreadStart(Drop));
                Dropper.Start();
            }

        }

        private static void Drop()
        {
            while(true){
                PalantirDbContext context = new PalantirDbContext();

                try
                {
                    context.Drop.RemoveRange(context.Drop);
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error clearing table: " + e.ToString());
                    continue;
                }

                DropEntity drop = new DropEntity();
                drop.CaughtLobbyKey = "";
                drop.CaughtLobbyPlayerID = "";
                drop.DropID = (new Random()).Next(1, 99999999).ToString();
                drop.ValidFrom = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                try
                {
                    context.Drop.Add(drop);
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error adding drop: " + e.ToString());
                    continue;
                }

                context.Dispose();
                Thread.Sleep(CalculateDropTimeoutSeconds() * 1000);
            }
        }

        private static int CalculateDropTimeoutSeconds()
        {
            PalantirDbContext context = new PalantirDbContext();

            int count = context.Status.ToList().Count;

            context.SaveChanges();
            context.Dispose();
            int min = 600 / count;
            if (min < 60) min = 60;

            return 30;//debug(new Random()).Next(min, 10 * min);
        }



    }
}
