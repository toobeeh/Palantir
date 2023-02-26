using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using Palantir.Model;

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
            // sync with last drop: get last dispatch time and wait until then
            PalantirContext context = new PalantirContext();
            //DateTime nextDrop = DateTime.ParseExact(context.Drop.First().ValidFrom, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            //int syncTime = Convert.ToInt32((nextDrop - DateTime.UtcNow).TotalMilliseconds);
            //if(syncTime > 0)
            //{
            //    //Program.Client.SendMessageAsync(Program.Client.GetChannelAsync(923282307723436122).GetAwaiter().GetResult(), timeout.ToString());
            //    Thread.Sleep(syncTime + 5000);
            //}

            while (true)
            {
                try
                {
                    context.NextDrops.RemoveRange(context.NextDrops);
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

                NextDrop drop = new NextDrop();
                drop.CaughtLobbyKey = "";
                drop.CaughtLobbyPlayerId = "";
                drop.DropId = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                drop.ValidFrom = DateTime.UtcNow.AddMilliseconds(dropTimeout).ToString("yyyy-MM-dd HH:mm:ss");
                drop.EventDropId = Events.GetRandomEventDropID();

                Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " > Next drop in " + dropTimeout
                    + " ms at " + DateTime.Now.AddMilliseconds(dropTimeout).ToString("HH:mm:ss")
                    + " for EventDropID #" + drop.EventDropId);

                try
                {
                    context.NextDrops.Add(drop);
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

                Thread.Sleep(dropTimeout); // wait untli drop was dispatched by ithil server

                // poll with 200ms to wait until drop was claimed or 5s passed (timeout), then continue loop
                int timeout = 0;
                int poll = 200;
                while (timeout < 5000 && context.NextDrops.Any(drop => drop.CaughtLobbyPlayerId == ""))
                {
                    timeout += poll;
                    Thread.Sleep(poll);
                }
            }

            context.Dispose();
        }

        public static int CalculateDropTimeoutSeconds()
        {

            List<string> onlineIDs = new List<string>();
            PalantirContext dbcontext = new PalantirContext();
            dbcontext.Statuses.ToList().ForEach(status =>
            {
                string id = JsonConvert.DeserializeObject<PlayerStatus>(status.Status1).PlayerMember.UserID;
                if (!onlineIDs.Contains(id)) onlineIDs.Add(id);
            });
            int count = onlineIDs.Count();
            dbcontext.Dispose();

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
            List<DropBoost> boosts = GetActiveBoosts();
            if (boosts.Count > 0)
            {
                double factor = GetActiveBoosts().ConvertAll(boost => Convert.ToDouble(boost.Factor)).Aggregate((a, x) => (a - 1) + x);
                if (factor > 1) return factor;
                else return 1;
            }
            else return 1;
        }

        public static List<DropBoost> GetActiveBoosts()
        {
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PalantirContext db = new();
            // free up boosts that have expired one week, enabling boosting again
            db.DropBoosts.RemoveRange(db.DropBoosts.Where(boost => Convert.ToInt64(boost.StartUtcs) + TimeSpan.FromDays(7).TotalMilliseconds - boost.CooldownBonusS < now && Convert.ToInt64(boost.StartUtcs) + boost.DurationS < now).ToArray());
            db.SaveChanges();
            // get all active boosts
            List<DropBoost> boosts = db.DropBoosts.Where(boost => boost.DurationS + Convert.ToInt64(boost.StartUtcs) > now).ToList();
            db.Dispose();
            return boosts;
        }

        public static bool AddBoost(int login, double factor, int duration, int cooldownSubtraction, out DropBoost currentBoost)
        {
            DropBoost boost = new()
            {
                Login = login,
                Factor = factor.ToString(),
                DurationS = duration * 1000,
                StartUtcs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                CooldownBonusS = cooldownSubtraction * 1000
            };
            bool inserted = false;
            PalantirContext db = new();
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
            PalantirContext db = new();
            DropBoost boost = db.DropBoosts.FirstOrDefault(boost => boost.Login == login);
            if (boost == null) cooldown = TimeSpan.FromSeconds(0);
            else
            {
                TimeSpan boostRemaining = TimeSpan.FromMilliseconds(Convert.ToDouble(boost.StartUtcs) + boost.DurationS - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                cooldown = TimeSpan.FromMilliseconds(Convert.ToDouble(boost.StartUtcs) + TimeSpan.FromDays(7).TotalMilliseconds - boost.CooldownBonusS - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                if(boostRemaining > cooldown) cooldown = boostRemaining;
            }
            db.Dispose();
            return cooldown;
        }
    }
}
