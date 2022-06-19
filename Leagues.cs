using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir

{
    internal class League
    {
        public static double Weight(double catchSeconds)
        {
            //convert to ms ugh
            catchSeconds *= 1000;
            if (catchSeconds < 0) return 0;
            if (catchSeconds > 1000) return 40;
            return -1.78641975945623 * Math.Pow(10, -9) * Math.Pow(catchSeconds, 4) + 0.00000457264006980028 * Math.Pow(catchSeconds, 3) - 0.00397188791256729 * Math.Pow(catchSeconds, 2) + 1.21566760222325 * catchSeconds;
        }

        public static List<double> GetLeagueDropWeights(string userid)
        {
            PalantirDbContext palantirDbContext = new PalantirDbContext();
            var userDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE LeagueWeight > 0 AND CaughtLobbyPlayerID == \"{userid}\"")
                .ToList();
            palantirDbContext.Dispose();
            List<double> weights = new();
            foreach (var item in userDrops)
            {
                if (item.EventDropID == 0) weights.Add(League.Weight(item.LeagueWeight / 1000.0) / 100);
            }

            return weights;
        }

        public static List<double> GetLeagueEventDropWeights(string userid)
        {
            PalantirDbContext palantirDbContext = new PalantirDbContext();
            var userDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE LeagueWeight > 0 AND EventDropID != 0 AND CaughtLobbyPlayerID == \"{userid}\"")
                .ToList();
            palantirDbContext.Dispose();
            List<double> weights = new();
            foreach (var item in userDrops)
            {
                if (item.EventDropID > 0) weights.Add(League.Weight(item.LeagueWeight / 1000.0) / 100);
            }

            return weights;
        }

        public static List<PastDropEntity> GetLeagueEventDrops(string userid, int[] eventdrops)
        {
            PalantirDbContext palantirDbContext = new PalantirDbContext();
            var userDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE EventDropID > 0 AND LeagueWeight > 0 AND CaughtLobbyPlayerID == \"{userid}\"")
                .ToList();
            palantirDbContext.Dispose();
            List<PastDropEntity> drops = new();
            foreach (var item in userDrops)
            {
                if(eventdrops.Contains(item.EventDropID)) drops.Add(item);
            }

            return drops;
        }

        public struct MemberLeagueResult
        {
            public List<PastDropEntity> LeagueDrops;
            public double AverageWeight;
            public double AverageTime;
            public double Score;
            public string Login;
            public int Streak;
        }

        private List<PastDropEntity> leagueDrops, allDrops;
        private string month, year;


        public League(string month, string year)
        {

            month = month.PadLeft(2, '0');
            this.month = month;
            this.year = year;

            PalantirDbContext palantirDbContext = new PalantirDbContext();
            this.leagueDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE LeagueWeight > 0 AND substr(ValidFrom, 6, 2) LIKE \"{month}\" AND substr(ValidFrom, 1, 4) == \"{year}\"")
                .ToList();
            this.allDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE substr(ValidFrom, 6, 2) LIKE \"{month}\" AND substr(ValidFrom, 1, 4) == \"{year}\"")
                .ToList();
            palantirDbContext.Dispose();
            //this.leagueDrops = palantirDbContext.PastDrops.Where(drop => 
            //    drop.ValidFrom.Substring(5,1).Contains(month) && drop.ValidFrom.Substring(4).Contains(year.ToString())
            //).ToList();
        }

        public bool IsActive()
        {
            return DateTime.Now.Month == Int32.Parse(this.month) && DateTime.Now.Year == Int32.Parse(this.year);
        }

        public long GetEndTimestamp()
        {
            return DateTimeOffset.Parse("01/" + this.month + "/" + this.year).AddMonths(1).ToUnixTimeSeconds();
        }

        public Dictionary<string, int> GetStreaks()
        {

            var participants = this.leagueDrops.Select(d => d.CaughtLobbyPlayerID).Distinct().ToList();
            int[] streaks = new int[participants.Count];
            int[] maxStreaks = new int[participants.Count];

            this.allDrops.GroupBy(d => d.DropID).ToList().ForEach(drop =>
            {
                for(int i = 0; i < participants.Count; i++)
                {
                    if(drop.Any(d => d.CaughtLobbyPlayerID == participants[i] && d.LeagueWeight > 0))
                    {
                        streaks[i]++;
                    }
                    else
                    {
                        if (streaks[i] > maxStreaks[i]) maxStreaks[i] = streaks[i];
                        streaks[i] = 0;
                    }
                }
            });

            Dictionary<string, int> results = new Dictionary<string, int>();

            for(int i = 0; i < participants.Count; i++)
            {
                results.Add(participants[i], maxStreaks[i]);
            }
            return results;
        }

        public List<MemberLeagueResult> LeagueResults()
        {

            var streaks = GetStreaks();

            List<MemberLeagueResult> results = new();
            leagueDrops.ConvertAll(drop => drop.CaughtLobbyPlayerID).Distinct().ToList().ForEach(userid =>
            {
                MemberLeagueResult result = new MemberLeagueResult();
                result.LeagueDrops = leagueDrops.Where(drop => drop.CaughtLobbyPlayerID == userid).ToList();
                result.AverageTime = Math.Round(result.LeagueDrops.Average(drop => drop.LeagueWeight));
                result.Score = Math.Round(result.LeagueDrops.Sum(drop => League.Weight(drop.LeagueWeight / 1000.0)));
                result.AverageWeight = Math.Round(result.Score / result.LeagueDrops.Count);
                result.Login = BubbleWallet.GetLoginOfMember(userid);
                result.Streak = streaks.ContainsKey(userid) ? streaks[userid] : 0;

                // lower score for readability
                result.Score = result.Score / 10;

                results.Add(result);
            });


            return results;
        }

    }
}
