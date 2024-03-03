using Microsoft.EntityFrameworkCore;
using Palantir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir

{
    internal struct MemberLeagueResult
    {
        public List<PastDrop> LeagueDrops;
        public double AverageWeight;
        public double AverageTime;
        public double Score;
        public string Login;
        public MemberSpanStreak Streak;
    }

    class MemberLeagueReward
    {
        public MemberLeagueResult result;
        public int splits;
        public List<string> rewards;
    }

    struct MemberSpanStreak
    {
        public int streakStart;
        public int streakEnd;
        public int streakMax;
    }

    struct LeagueCache
    {
        public List<MemberLeagueResult> results;
        public long lastDrop;
    }

    internal class League
    {

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
            PalantirContext palantirDbContext = new PalantirContext();
            var userDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE LeagueWeight > 0 AND EventDropID = 0 AND CaughtLobbyPlayerID = '{userid}'")
                .ToList();
            palantirDbContext.Dispose();
            List<double> weights = new();
            foreach (var item in userDrops)
            {
               weights.Add(League.Weight(item.LeagueWeight / 1000.0) / 100);
            }

            return weights;
        }

        public static List<double> GetLeagueEventDropWeights(string userid)
        {
            PalantirContext palantirDbContext = new PalantirContext();
            var userDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE LeagueWeight > 0 AND EventDropID != 0 AND CaughtLobbyPlayerID = '{userid}'")
                .ToList();
            palantirDbContext.Dispose();
            List<double> weights = new();
            foreach (var item in userDrops)
            {
                weights.Add(League.Weight(item.LeagueWeight / 1000.0) / 100);
            }

            return weights;
        }

        public static List<PastDrop> GetLeagueEventDrops(string userid, int[] eventdrops)
        {
            PalantirContext palantirDbContext = new PalantirContext();
            var userDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE EventDropID > 0 AND LeagueWeight > 0 AND CaughtLobbyPlayerID = '{userid}'")
                .ToList();
            palantirDbContext.Dispose();
            List<PastDrop> drops = new();
            foreach (var item in userDrops)
            {
                if(eventdrops.Contains(item.EventDropId)) drops.Add(item);
            }

            return drops;
        }

        private List<PastDrop> leagueDrops, allDrops;
        private string month, year;
        public string seasonName;


        public League(string month, string year)
        {

            month = month.PadLeft(2, '0');
            this.month = month;
            this.year = year;
            this.seasonName = DateTime.Parse("01/" + this.month + "/" + this.year + " +0000").ToString("MMMM yyyy");

            PalantirContext palantirDbContext = new PalantirContext();
            this.leagueDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE LeagueWeight > 0 AND substr(ValidFrom, 6, 2) = '{month}' AND substr(ValidFrom, 1, 4) = '{year}'")
                .ToList();
            this.allDrops = palantirDbContext.PastDrops
                .FromSqlRaw($"SELECT * FROM \"PastDrops\" WHERE substr(ValidFrom, 6, 2)= '{month}' AND substr(ValidFrom, 1, 4) = '{year}'")
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

        private Dictionary<string, MemberSpanStreak> GetStreaks(long startDropID)
        {

            var participants = this.leagueDrops.Select(d => d.CaughtLobbyPlayerId).Distinct().ToList();
            int[] streaks = new int[participants.Count];
            int[] startStreaks = new int[participants.Count];
            int[] maxStreaks = new int[participants.Count];

            int dropCount = 0;
            this.allDrops.Where(drop => Convert.ToInt64(drop.DropId) > startDropID).GroupBy(d => d.DropId).ToList().ForEach(drop =>
            {
                dropCount++;

                for (int i = 0; i < participants.Count; i++)
                {
                    if(drop.Any(d => d.CaughtLobbyPlayerId == participants[i] && d.LeagueWeight > 0))
                    {
                        streaks[i]++;
                        if (startStreaks[i] == dropCount - 1) startStreaks[i] = dropCount;
                    }
                    else
                    {
                        if (streaks[i] > maxStreaks[i]) maxStreaks[i] = streaks[i];
                        streaks[i] = 0;
                    }
                }

            });

            Dictionary<string, MemberSpanStreak> results = new Dictionary<string, MemberSpanStreak>();

            for(int i = 0; i < participants.Count; i++)
            {
                MemberSpanStreak streak = new MemberSpanStreak()
                {
                    streakStart = startStreaks[i],
                    streakEnd = streaks[i],
                    streakMax = maxStreaks[i]
                };
                results.Add(participants[i], streak);
            }
            return results;
        }

        private static Dictionary<string,LeagueCache> cachedResults = new();

        public List<MemberLeagueResult> LeagueResults()
        {

            // get cached results if available
            LeagueCache cached;
            if (League.cachedResults.ContainsKey(this.seasonName)) cached = League.cachedResults[this.seasonName];
            else cached = new() { results = new(), lastDrop = 0 };

            var streaks = GetStreaks(cached.lastDrop);

            List<MemberLeagueResult> results = new();

            List<PastDrop> uncached = leagueDrops.Where(drop => Convert.ToInt64(drop.DropId) > cached.lastDrop).ToList();
            leagueDrops.Where(drop => Convert.ToInt64(drop.DropId) > cached.lastDrop).ToList().ConvertAll(drop => drop.CaughtLobbyPlayerId).Distinct().ToList().ForEach(userid =>
            {
                MemberLeagueResult result = new MemberLeagueResult();
                result.Login = BubbleWallet.GetLoginOfMember(userid);

                if(new PermissionFlag(Program.Feanor.GetFlagByMemberId(userid)).Dropban) return;

                MemberLeagueResult cachedResult = new()
                {
                    LeagueDrops = new(),
                    AverageTime = 0,
                    Score = 0,
                    AverageWeight = 0,
                    Streak = new MemberSpanStreak()
                    {
                        streakStart = 0,
                        streakEnd = 0,
                        streakMax = 0
                    }
                };

                if (cached.results.Any(c => c.Login == result.Login)) cachedResult = cached.results.FirstOrDefault(c => c.Login == result.Login);

                List<PastDrop> uncachedMemerDrops = uncached.Where(drop => drop.CaughtLobbyPlayerId == userid).ToList();

                result.LeagueDrops = cachedResult.LeagueDrops.Concat(uncachedMemerDrops).ToList();
                result.AverageTime = Math.Round(cachedResult.AverageTime * cachedResult.LeagueDrops.Count / result.LeagueDrops.Count + uncachedMemerDrops.Average(drop => drop.LeagueWeight) * uncachedMemerDrops.Count / result.LeagueDrops.Count);
                result.Score = Math.Round(cachedResult.Score + uncachedMemerDrops.Sum(drop => League.Weight(drop.LeagueWeight / 1000.0)) / 10, 1);
                result.AverageWeight = Math.Round(result.Score * 10 / result.LeagueDrops.Count);

                bool hasNewStreak = streaks.ContainsKey(userid);
                result.Streak = new MemberSpanStreak()
                {
                    streakStart = cachedResult.Streak.streakStart,
                    streakEnd = hasNewStreak ? streaks[userid].streakEnd : Convert.ToInt64(this.allDrops.Last().DropId) == cached.lastDrop ? cachedResult.Streak.streakEnd : 0,
                    streakMax = (new int[] { cachedResult.Streak.streakMax, cachedResult.Streak.streakEnd + (hasNewStreak ? streaks[userid].streakStart : 0), hasNewStreak ? streaks[userid].streakMax : 0 }).Max()
                };

                results.Add(result);
            });

            // add results that were not updated
            results = results.Concat(cached.results.Where(c => !results.Any(r => r.Login == c.Login))).ToList();

            // update cache
            League.cachedResults[this.seasonName] = new LeagueCache()
            {
                lastDrop = Convert.ToInt64(this.allDrops.Last().DropId),
                results = results
            };

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
            addReward(overall_1.Login, "Overall Ranking Leader", 6, overall_1);

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
            var resultsStreakAll = results.OrderByDescending(results => results.Streak.streakMax).ToList();
            var resultsStreak = results.OrderByDescending(results => results.Streak.streakMax).Where(res => res.Login != overall_1.Login).ToList();

            // add #1 streak
            var streak_1 = resultsStreak[0];
            addReward(streak_1.Login, "Highest Streak (" + streak_1.Streak.streakMax + ")", 2, streak_1);

            // add #2 streak
            var streak_2 = resultsStreak[1];
            addReward(streak_2.Login, "#2 Streak (" + streak_2.Streak.streakMax + ")", 1, streak_2);

            // add #3 streak
            var streak_3 = resultsStreak[2];
            addReward(streak_3.Login, "#3 Streak (" + streak_3.Streak.streakMax + ")", 1, streak_3);

            // ------ add top count -----------
            var resultsCountAll = results.OrderByDescending(results => results.LeagueDrops.Count).ToList();
            var resultsCount = results.OrderByDescending(results => results.LeagueDrops.Count).Where(res => res.Login != overall_1.Login).ToList();

            // add #1 count
            var count_1 = resultsCount[0];
            addReward(count_1.Login, "Most Drops (" + count_1.LeagueDrops.Count + ")", 2, count_1);

            // add #2 count
            var count_2 = resultsCount[1];
            addReward(count_2.Login, "#2 Drops (" + count_2.LeagueDrops.Count + ")", 1, count_2);

            // add #3 count
            var count_3 = resultsCount[2];
            addReward(count_3.Login, "#3 Drops (" + count_3.LeagueDrops.Count + ")", 1, count_3);

            // --- total domination
            if(results[0].Login == resultsStreakAll[0].Login && results[0].Login == resultsCountAll[0].Login)
            {
                addReward(results[0].Login, "League Champion in all categories", 4, results[0]);
            }

            return rewards;
        }

    }
}
