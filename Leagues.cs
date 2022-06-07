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
        }

        private List<PastDropEntity> leagueDrops;

        public League(string month, string year)
        {

            month = month.PadLeft(2, '0');

            PalantirDbContext palantirDbContext = new PalantirDbContext();
            this.leagueDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE LeagueWeight > 0 AND substr(ValidFrom, 6, 2) LIKE \"{month}\" AND substr(ValidFrom, 1, 4) == \"{year}\"")
                .ToList();
            //this.leagueDrops = palantirDbContext.PastDrops.Where(drop => 
            //    drop.ValidFrom.Substring(5,1).Contains(month) && drop.ValidFrom.Substring(4).Contains(year.ToString())
            //).ToList();
        }

        public List<MemberLeagueResult> LeagueResults()
        {
            List<MemberLeagueResult> results = new();
            leagueDrops.ConvertAll(drop => drop.CaughtLobbyPlayerID).Distinct().ToList().ForEach(userid =>
            {
                MemberLeagueResult result = new MemberLeagueResult();
                result.LeagueDrops = leagueDrops.Where(drop => drop.CaughtLobbyPlayerID == userid).ToList();
                result.AverageTime = Math.Round(result.LeagueDrops.Average(drop => drop.LeagueWeight));
                result.Score = Math.Round(result.LeagueDrops.Sum(drop => League.Weight(drop.LeagueWeight / 1000.0)));
                result.AverageWeight = Math.Round(result.Score / result.LeagueDrops.Count);
                result.Login = BubbleWallet.GetLoginOfMember(userid);

                results.Add(result);
            });

            return results;
        }

    }
}
