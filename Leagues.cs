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
        private static Dictionary<string, League> Cache = new();

        public static double Weight(double catchSeconds)
        {
            //convert to ms ugh
            catchSeconds *= 1000;
            if (catchSeconds < 0) return 0;
            if (catchSeconds > 1000) return 30;
            return -1.78641975945623 * Math.Pow(10, -9) * Math.Pow(catchSeconds, 4) + 0.00000457264006980028 * Math.Pow(catchSeconds, 3) - 0.00397188791256729 * Math.Pow(catchSeconds, 2) + 1.21566760222325 * catchSeconds;
        }

        public static int CalcLeagueDropsValue(List<double> weights)
        {
            return Convert.ToInt32(Math.Floor(weights.Sum()));
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

        public class MemberLeagueReward
        {
            public MemberLeagueResult result;
            public int splits;
            public List<string> rewards;
        }

        private List<PastDropEntity> leagueDrops, allDrops;
        private string month, year;
        public string seasonName;


        public League(string month, string year)
        {

            month = month.PadLeft(2, '0');
            this.month = month;
            this.year = year;
            this.seasonName = DateTime.Parse("01/02/2022 +0000").ToString("MMMM yyyy");


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
            return DateTime.UtcNow.Month == Int32.Parse(this.month) && DateTime.UtcNow.Year == Int32.Parse(this.year);
        }

        public long GetEndTimestamp()
        {
            return DateTimeOffset.Parse("01/" + this.month + "/" + this.year + " +0000").AddMonths(1).ToUnixTimeSeconds();
        }

        private Dictionary<string, int> GetStreaks()
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

        public List<MemberLeagueReward> Evaluate()
        {
            List<MemberLeagueReward> rewards = new();

            Action<string, string, int, MemberLeagueResult> addReward = (string login, string desc, int splits, MemberLeagueResult res) =>
            {
                var reward = rewards.FirstOrDefault(r => r.result.Login == login);
                if(reward is null)
                {
                    reward = new MemberLeagueReward();
                    reward.result = res;
                    reward.splits = 0;
                    reward.rewards = new();
                    rewards.Add(reward);
                }

                reward.rewards.Add(desc);
                reward.splits += splits;
            };

            var results = this.LeagueResults().OrderByDescending(res => res.Score).ToList();

            // add #1 overall
            var overall_1 = results[0];
            addReward(overall_1.Login, "Overall Ranking Leader", 5, overall_1);

            // add #2 overall
            var overall_2 = results[1];
            addReward(overall_2.Login, "Overall Ranking #2", 4, overall_2);

            // add #3 overall
            var overall_3 = results[2];
            addReward(overall_3.Login, "Overall Ranking #3", 3, overall_3);

            // add top 10
            foreach(var overall_10 in results.Skip(3).Take(7))
            {
                addReward(overall_10.Login, "Overall Ranking Top 10", 2, overall_10);
            }

            // add top 20
            foreach (var overall_20 in results.Skip(10).Take(10))
            {
                addReward(overall_20.Login, "Overall Ranking Top 20", 1, overall_20);
            }

            // ------ add top streak -----------
            var resultsStreak = results.OrderByDescending(results => results.Streak).ToList();

            // add #1 streak
            var firstStreakDisqualified = results[0].Login == resultsStreak[0].Login;
            var streak_1 = firstStreakDisqualified ? resultsStreak[1] : resultsStreak[0];
            addReward(streak_1.Login, "Highest Streak (" + streak_1.Streak + ")", 3, streak_1);

            // add #2 streak
            var streak_2 = firstStreakDisqualified ? resultsStreak[2] : resultsStreak[1];
            addReward(streak_2.Login, "#2 Streak (" + streak_2.Streak + ")", 2, streak_2);

            // add #3 streak
            var streak_3 = firstStreakDisqualified ? resultsStreak[3] : resultsStreak[2];
            addReward(streak_3.Login, "#3 Streak (" + streak_3.Streak + ")", 1, streak_3);

            // ------ add top count -----------
            var resultsCount = results.OrderByDescending(results => results.LeagueDrops.Count).ToList();

            // add #1 count
            var firstCountDisqualified = results[0].Login == resultsCount[0].Login;
            var count_1 = firstCountDisqualified ? resultsCount[1] : resultsCount[0];
            addReward(streak_1.Login, "Most Drops (" + streak_1.LeagueDrops.Count + ")", 3, streak_1);

            // add #2 count
            var count_2 = firstCountDisqualified ? resultsCount[2] : resultsCount[1];
            addReward(streak_2.Login, "#2 Drops (" + count_2.LeagueDrops.Count + ")", 2, count_2);

            // add #3 count
            var count_3 = firstCountDisqualified ? resultsCount[3] : resultsCount[2];
            addReward(streak_3.Login, "#3 Drops (" + streak_3.LeagueDrops.Count + ")", 1, streak_3);

            // --- total domination
            if(results[0].Login == resultsStreak[0].Login && results[0].Login == resultsCount[0].Login)
            {
                addReward(results[0].Login, "League Champion in all categories", 3, results[0]);
            }

            return rewards;
        }

    }
}
