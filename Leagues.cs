using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir

{
    internal class League
    {
        public static double Weight(int catchSeconds)
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

        public League(int month, int year)
        {

            PalantirDbContext palantirDbContext = new PalantirDbContext();
            this.leagueDrops = palantirDbContext.PastDrops.Where(drop => 
                drop.ValidFrom.Substring(5,1) == month.ToString().PadLeft(2,'0') && drop.ValidFrom.Substring(4) == year.ToString()
            ).ToList();
        }

        public List<MemberLeagueResult> LeagueResults()
        {
            List<MemberLeagueResult> results = new();
            leagueDrops.ConvertAll(drop => drop.CaughtLobbyPlayerID).Distinct().ToList().ForEach(userid =>
            {
                MemberLeagueResult result = new MemberLeagueResult();
                result.LeagueDrops = leagueDrops.Where(drop => drop.CaughtLobbyPlayerID == userid).ToList();
                result.AverageTime = Math.Round(leagueDrops.Average(drop => drop.LeagueWeight));
                result.Score = Math.Round(leagueDrops.Sum(drop => League.Weight(drop.LeagueWeight / 1000)));
                result.AverageWeight = Math.Round(result.Score / result.LeagueDrops.Count);
                result.Login = BubbleWallet.GetLoginOfMember(userid);

                results.Add(result);
            });

            return results;
        }

    }
}
