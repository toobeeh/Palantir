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
            while (true)
            {
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

                int dropTimeout = CalculateDropTimeoutSeconds() * 1000;

                DropEntity drop = new DropEntity();
                drop.CaughtLobbyKey = "";
                drop.CaughtLobbyPlayerID = "";
                drop.DropID = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                drop.ValidFrom = DateTime.UtcNow.AddMilliseconds(dropTimeout).ToString("yyyy-MM-dd HH:mm:ss");
                drop.EventDropID = Events.GetRandomEventDropID();

                Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Next drop in " + dropTimeout
                    + " ms at " + DateTime.Now.AddMilliseconds(dropTimeout).ToString("HH:mm:ss")
                    + " for EventDropID #" + drop.EventDropID);

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

                // poll with 200ms to wait until drop was claimed or 5s passed (timeout)
                int timeout = 0;
                int poll = 500;
                string pollInfo = "";
                while (timeout < 15000 && context.Drop.Any(drop => drop.CaughtLobbyPlayerID == ""))
                {
                    pollInfo += timeout + ":" + context.Drop.First().CaughtLobbyPlayerID + "\n";
                    timeout += poll;
                    Thread.Sleep(poll);
                }
                Program.Client.SendMessageAsync(Program.Client.GetChannelAsync(923282307723436122).GetAwaiter().GetResult(), "polled " + timeout.ToString() + "-" + pollInfo);

                Thread.Sleep(dropTimeout + 1000); // add next drop 1s after old was claimed
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
            min += 20; // minimum offset

            // modify by boosts
            min = Convert.ToInt32(Math.Round(min / GetCurrentFactor(), 0));
            
            return (new Random()).Next(min, 4 * min);
        }

        public static double GetCurrentFactor()
        {
            List<BoostEntity> boosts = GetActiveBoosts();
            if (boosts.Count > 0)
            {
                double factor = GetActiveBoosts().ConvertAll(boost => boost.Factor).Aggregate((a, x) => (a - 1) + x);
                if (factor > 1) return factor;
                else return 1;
            }
            else return 1;
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

        public static TimeSpan BoostCooldown(int login)
        {
            TimeSpan cooldown = new();
            PalantirDbContext db = new();
            if (!db.DropBoosts.Any(boost => boost.Login == login)) cooldown = TimeSpan.FromSeconds(0);
            else cooldown = TimeSpan.FromMilliseconds(db.DropBoosts.FirstOrDefault(boost => boost.Login == login).StartUTCS + TimeSpan.FromDays(7).TotalMilliseconds - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            db.Dispose();
            return cooldown;
        }
    }
}
