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
            return -372.505925447102 * Math.Pow(catchSeconds, 4) + 1093.85046326223 * Math.Pow(catchSeconds, 3) - 988.674423615601 * Math.Pow(catchSeconds, 2) + 187.221934927817 * catchSeconds + 90.1079508726569;
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


        public League(string month, string year)
        {

            month = month.PadLeft(2, '0');

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

        public Dictionary<string, int> GetStreaks()
        {
            Dictionary<string,int> streaks = new Dictionary<string,int>();

            var participants = this.leagueDrops.Select(d => d.CaughtLobbyPlayerID).Distinct().ToList();

            this.allDrops.GroupBy(d => d.DropID).ToList().ForEach(drop =>
            {
                participants.ForEach(p =>
                {
                    if (drop.Any(d => d.CaughtLobbyPlayerID == p && d.LeagueWeight > 0)){
                        int val;
                        if (streaks.TryGetValue(p, out val))
                            streaks[p] = val++;
                        else streaks[p] = 1;
                    }
                    else
                    {
                        streaks[p] = 0;
                    }
                });
            });

            return streaks;
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
