using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;

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
                catch (Microsoft.Data.Sqlite.SqliteException e)
                {
                    if (e.SqliteErrorCode == 8)
                    {
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Error clearing table: Database locked. Waiting 100ms then retry.");
                        Thread.Sleep(100);
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Unhandled SQL error clearing table, immediately trying again: " + e.ToString());
                    }
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Unhandled error clearing table, immediately trying again: " + e.ToString());
                    continue;
                }

                DropEntity drop = new DropEntity();
                drop.CaughtLobbyKey = "";
                drop.CaughtLobbyPlayerID = "";
                drop.DropID = (new Random()).Next(1, 99999999).ToString();
                drop.ValidFrom = DateTime.UtcNow.AddSeconds(20).ToString("yyyy-MM-dd HH:mm:ss");
                drop.EventDropID = Events.GetRandomEventDropID();

                try
                {
                    context.Drop.Add(drop);
                    context.SaveChanges();
                }
                catch (Microsoft.Data.Sqlite.SqliteException e )
                {
                    if (e.SqliteErrorCode == 8)
                    {
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Error adding drop: Database locked. Waiting 100ms then retry..");
                        Thread.Sleep(100);
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Unhandled SQL error adding drop, immediately trying again: " + e.ToString());
                    }
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Unhandled error adding drop, immediately trying again: " + e.ToString());
                    continue;
                }

                context.Dispose();
                int sleep = CalculateDropTimeoutSeconds() * 1000 + 20000;
                Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Next drop in " + sleep 
                    + " ms at " + DateTime.Now.AddMilliseconds(sleep).ToString("HH:mm:ss")
                    + " for EventDropID #" + drop.EventDropID);
                Thread.Sleep(sleep);
            }
        }

        public static int CalculateDropTimeoutSeconds()
        {
            PalantirDbContext context = new PalantirDbContext();

            List<string> onlineIDs = new List<string>();
            PalantirDbContext dbcontext = new PalantirDbContext();
            dbcontext.Status.ToList().ForEach(status =>
            {
                string id = JsonConvert.DeserializeObject<PlayerStatus>(status.Status).PlayerMember.UserID;
                if (!onlineIDs.Contains(id)) onlineIDs.Add(id);
            });
            int count = onlineIDs.Count();
            context.Dispose();

            if (count <= 0) count = 1;
            int min = 600 / count;
            if (min < 30) min = 30;

            // modify by boosts
            List<BoostEntity> boosts = GetActiveBoosts();
            if(boosts.Count > 0)
            {
                double factor = GetActiveBoosts().ConvertAll(boost => boost.Factor).Aggregate((a, x) => a + x);
                if (factor > 1) min = Convert.ToInt32(Math.Round(min / factor, 0));
            }
            
            return (new Random()).Next(min, 4 * min);
        }

        public static List<BoostEntity> GetActiveBoosts()
        {
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PalantirDbContext db = new();
            // free up boosts that have expired one week, enabling boosting again
            db.DropBoosts.RemoveRange(db.DropBoosts.Where(boost => boost.StartUTCS + TimeSpan.FromDays(7).TotalMilliseconds < now).ToArray());
            db.SaveChanges();
            // get all active boosts
            List<BoostEntity> boosts = db.DropBoosts.Where(boost => boost.DurationS + boost.StartUTCS > now).ToList();
            db.Dispose();
            return boosts;
        }

        public static bool AddBoost(int login, double factor, int duration, out BoostEntity currentBoost)
        {
            BoostEntity boost = new()
            {
                Login = login,
                Factor = factor,
                DurationS = duration * 1000,
                StartUTCS = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            bool inserted = false;
            PalantirDbContext db = new();
            if(!db.DropBoosts.Any(boost => boost.Login == login))
            {
                db.DropBoosts.Add(boost);
                db.SaveChanges();
                inserted = true;
            }
            currentBoost = db.DropBoosts.FirstOrDefault(boost => boost.Login == login);
            db.Dispose();
            return inserted;
        }
    }
}
