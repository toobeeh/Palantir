using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Quartz;
using MoreLinq;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Palantir.Model;

namespace Palantir
{

    public class BubbleCounter : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            string[] logins = BubbleWallet.loginBubbleTicks.Distinct().ToArray();
            BubbleWallet.loginBubbleTicks = new List<string>();
            Dictionary<string, int> cache = new Dictionary<string, int>();
            PalantirContext dbcontext = new PalantirContext();
            try
            {
                foreach (string login in logins)
                {
                    Model.Member member = dbcontext.Members.FirstOrDefault(s => s.Login.ToString() == login);
                    if(JsonConvert.DeserializeObject<Member>(member.Member1).Guilds.Count > 0) member.Bubbles++;
                }
            }
            catch(Exception e) { Console.WriteLine(e.ToString()); }
            await dbcontext.SaveChangesAsync();
            foreach(Model.Member member in dbcontext.Members)
            {
                cache.Add(member.Login.ToString(), member.Bubbles);
            }
            dbcontext.Dispose();
            BubbleWallet.BubbleCache = cache;
        }
    }
    public static class BubbleWallet
    {
        public static int SceneStartPrice = 20000;
        public static int ScenePriceFactor = 2;
        public static List<string> loginBubbleTicks = new List<string>();
        public static Dictionary<string, int> BubbleCache = new Dictionary<string, int>();
        public static void AddBubble(string login)
        {
            loginBubbleTicks.Add(login);
        }

        public static int GetBubbles(string login)
        {
            int bubbles = 0;
            if(! BubbleCache.TryGetValue(login, out bubbles))
            {
                var ctx = new PalantirContext();
                bubbles = ctx.Members.FirstOrDefault(m => m.Login.ToString() == login).Bubbles;
                ctx.Dispose();
            }
            return bubbles;
        }

        public static int GetDrops(string login, string userid)
        {
            PalantirContext context = new PalantirContext();
            Model.Member entity = context.Members.FirstOrDefault(s => s.Login.ToString() == login);
            int drops = 0;

            if (entity != null)
            {
                drops = entity.Drops;
            }
            context.SaveChanges();
            context.Dispose();

            int leagueWieght = League.CalcLeagueDropsValue(League.GetLeagueDropWeights(userid));
            drops += leagueWieght;

            return drops;
        }

        public static int GetCollectedBubblesInTimespan(DateTime start, DateTime end, string login)
        {
            start = start.AddDays(-1);
            PalantirContext db = new();
            List<BubbleTrace> bubbleTraces = db.BubbleTraces.Where(trace => trace.Login.ToString() == login).ToList();
            List<int> bubbles = bubbleTraces.Where(trace =>
            {
                DateTime tracedt = Convert.ToDateTime(trace.Date);
                return tracedt <= end && tracedt >= start;
            }).Select(trace => trace.Bubbles).ToList();
            int timespanStartBubbles = bubbles.Count > 0 ? bubbles.Min() : 0;
            int timespanEndBubbles = bubbles.Count > 0 ? bubbles.Max() : 0;

            // if for last day isn't a trace existent, use current bubble value 
            if(bubbles.Count > 0 && !bubbleTraces.Any(trace => Convert.ToDateTime(trace.Date).Date.Equals(end.Date)))
            {
                timespanEndBubbles = BubbleWallet.GetBubbles(login);
            }
            return timespanEndBubbles - timespanStartBubbles;
        }

        public static List<SpriteProperty> ParseSpriteInventory(string sprites)
        {
            List<Sprite> availableSprites = GetAvailableSprites();
            List<SpriteProperty> spriteInventory = new List<SpriteProperty>();
            List<string> spriteIds = sprites.Split(',').ToList();
            spriteIds.ForEach(i =>
            {
                bool activated = false;
                int slot = -1;
                if (i.StartsWith(".")) 
                { 
                    activated = true; 
                    slot = i.Count(ctr => ctr == '.'); 
                    i = i.Replace(".", ""); 
                }
                availableSprites.ForEach(s =>
                {
                    try
                    {
                        int id;
                        if (int.TryParse(i,out id) && id == s.ID) spriteInventory.Add(new SpriteProperty(s.Name, s.URL, s.Cost, s.ID, s.Special, s.Rainbow, s.EventDropID, s.Artist, activated, slot));
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Error parsing " + i + " from " + sprites + " : " + e.ToString());
                        throw new InvalidCastException("Could not parse inventory string " + sprites + " at " + i + "\n" + e.ToString());
                    }
                });
            });
            return spriteInventory;
        }

        public static string SetInventory(List<SpriteProperty> sprites, string login)
        {
            string inv = "";
            List<Sprite> available = GetAvailableSprites();
            available.ForEach(s =>
            {
                if (sprites.Any(a => a.ID == s.ID))
                {
                    SpriteProperty found = sprites.FirstOrDefault(a => a.ID == s.ID);
                    inv += (found.Activated ? new string('.', found.Slot) : "") + s.ID + ",";
                }
            });
            inv = inv.Remove(inv.Length - 1);
            PalantirContext context = new PalantirContext();
            context.Members.FirstOrDefault(m => m.Login.ToString() == login).Sprites = "0," + inv;
            context.SaveChanges();
            context.Dispose();
            return inv;
        }

        public static List<Sprite> GetAvailableSprites()
        {
            List<Sprite> sprites = new List<Sprite>();
            PalantirContext context = new PalantirContext();
            context.Sprites.ToList().ForEach(s => sprites.Add(new Sprite(s.Name, s.Url, s.Cost, s.Id, s.Special, s.Rainbow == 1, s.EventDropId, s.Artist)));
            context.SaveChanges();
            context.Dispose();
            return sprites;
        }
        public static Sprite GetSpriteByID(int id)
        {
            return GetAvailableSprites().FirstOrDefault(s => s.ID == id);
        }

        public static void AddSprite(Sprite sprite)
        {
            PalantirContext context = new PalantirContext();
            Model.Sprite s = new();
            s.Id = sprite.ID;
            s.Name = sprite.Name;
            s.Special = sprite.Special;
            s.Url = sprite.URL;
            s.EventDropId = sprite.EventDropID;
            s.Cost = sprite.Cost;
            s.Artist = sprite.Artist;
            context.Sprites.Add(s);
            context.SaveChanges();
            context.Dispose();
        }

        public static Scene AddScene(string name, string color, string guessedColor, string artist, string url, int eventID, bool exclusive)
        {
            PalantirContext context = new PalantirContext();
            int id = context.Scenes.Count() > 0 ? context.Scenes.Select(s => s.Id).Max() + 1 : 1;
            Scene scene = new Scene()
            {
                Artist = artist,
                Color = color,
                GuessedColor = guessedColor,
                Url = url,
                Name = name,
                Id = id,
                EventId = eventID,
                Exclusive = exclusive
            };
            context.Scenes.Add(scene);
            context.SaveChanges();
            context.Dispose();
            return scene;
        }

        public static int NextSceneId()
        {
            PalantirContext context = new PalantirContext();
            int id = context.Scenes.Count() > 0 ? context.Scenes.Select(s => s.Id).Max() + 1 : 1;
            return id;
        }

        public static int CalculateCredit(string login, string userid)
        {
            int total = GetBubbles(login);
            GetInventory(login).ForEach(s =>
            {
                if(s.EventDropID == 0) total -= s.Cost;
            });
            total += GetDrops(login, userid) * 50;
            int nextPrice = SceneStartPrice;
            List<SceneProperty> regScenes = GetSceneInventory(login, false, true);
            foreach (SceneProperty scene in regScenes)
            {
                if (!scene.Exclusive)
                {
                    total -= nextPrice;
                    nextPrice *= ScenePriceFactor;
                }
            }
            return total;
        }

        public static void BuyScene(string login, int id)
        {
            PalantirContext db = new();
            Model.Member member = db.Members.FirstOrDefault(member => member.Login.ToString() == login);
            string sceneInv = GetSceneInventory(login, false, false).ConvertAll(scene => (scene.Activated ? "." : "") + scene.Id.ToString()).ToDelimitedString(",");
            sceneInv += (sceneInv.Length > 0 ? "," : "") + id.ToString();
            member.Scenes = sceneInv;
            db.SaveChanges();
            db.Dispose();
        }

        public static void SetSceneInventory(string login, List<SceneProperty> inv)
        {
            PalantirContext db = new();
            db.Members.FirstOrDefault(member => member.Login.ToString() == login).Scenes = inv
                .ConvertAll(scene => (scene.Activated ? "." : "") + scene.Id.ToString())
                .ToDelimitedString(",");
            db.SaveChanges();
            db.Dispose();
        }

        public static int GetEventCredit(string login, int eventDropID)
        {
            PalantirContext context = new PalantirContext();
            int credit = 0;
            try { 
                credit = context.EventCredits.FirstOrDefault(c => c.EventDropId == eventDropID && c.Login.ToString() == login).Credit;
            }
            catch { }
            
            context.Dispose();
            return credit;
        }
        public static List<Scene> GetAvailableScenes()
        {
            PalantirContext db = new PalantirContext();
            List<Scene> scenes = db.Scenes.ToList();
            db.Dispose();
            return scenes;
        }

        public static List<SpriteProperty> GetInventory(string login)
        {
            PalantirContext db = new PalantirContext();
            string inventoryString = db.Members.FirstOrDefault(m => m.Login.ToString() == login).Sprites;
            db.Dispose();
            return ParseSpriteInventory(inventoryString);
        }

        public static List<SceneProperty> GetSceneInventory(string login, bool onlyActive = false, bool noEventScenes = true)
        {
            PalantirContext db = new PalantirContext();
            string inventoryString = db.Members.FirstOrDefault(m => m.Login.ToString() == login).Scenes;
            if (String.IsNullOrEmpty(inventoryString)) return new List<SceneProperty>();
            List<SceneProperty> inv = inventoryString.Split(",")
                .Where(id => !onlyActive || onlyActive && id.Contains("."))
                .ToList()
                .ConvertAll(id => GetSceneProperty(
                    db.Scenes.ToList().FirstOrDefault(scene => scene.Id.ToString() == id.Replace(".", "")), id.Contains('.')
                    ));
            db.Dispose();
            if (noEventScenes) inv = inv.Where(s => s.EventId == 0).ToList();
            return inv;
        }

        public static bool IsEarlyUser(string login)
        {
            PalantirContext db = new PalantirContext();
            bool exists = db.BubbleTraces.Any(trace => trace.Date == "31/08/2020" && trace.Login.ToString() == login);
            db.Dispose();
            return exists;
        }
        public static string FirstTrace(string login)
        {
            PalantirContext db = new PalantirContext();
            List<string> dates = db.BubbleTraces.Where(trace => trace.Login.ToString() == login).Select(trace => trace.Date).ToList();
            db.Dispose();
            return dates.Min(date => DateTime.Parse(date)).ToShortDateString();
        }
        public static List<int> ParticipatedEvents(string login)
        {
            List<int> events = new List<int>();
            PalantirContext db = new PalantirContext();
            List<int> eventdrops = db.EventCredits.Where(credit => credit.Login.ToString() == login).Select(credit => credit.EventDropId).ToList();
            eventdrops.ForEach(caughtdrop =>
            {
                int eventid = db.EventDrops.FirstOrDefault(drop => caughtdrop == drop.EventDropId).EventId;
                if (!events.Contains(eventid)) events.Add(eventid);
            });
            db.Dispose();
            return events;
        }
        public static int CaughtEventdrops(string discordID)
        {
            PalantirContext db = new PalantirContext();
            int caught = db.PastDrops.Where(drop => drop.CaughtLobbyPlayerId == discordID && drop.EventDropId != 0 && drop.LeagueWeight == 0).Count();
            db.Dispose();
            return caught;
        }

        public static int GlobalRanking(string login, bool drops = false)
        {
            PalantirContext db = new PalantirContext();
            int index = db.Members.ToList().Where(member=> !(new PermissionFlag(Convert.ToInt16(member.Flag)).BubbleFarming)).OrderByDescending(member => drops ? member.Drops : member.Bubbles).Select(member => member.Login).ToList().IndexOf(Convert.ToInt32(login)) + 1;
            db.Dispose();
            return index;
        }

        public static Dictionary<int, int[]> SpriteScoreboard()
        {
            List<Sprite> sprites = BubbleWallet.GetAvailableSprites();
            // get all bought sprites
            List<SpriteProperty> joined = new List<SpriteProperty>();
            PalantirContext db = new PalantirContext();
            db.Members.ForEach(member => {
                string[] sprites = member.Sprites.Split(",");
                sprites.ForEach(id =>
                {
                    int indOfActive = id.ToString().LastIndexOf(".");
                    id = id.Substring(indOfActive < 0 ? 0 : indOfActive + 1);
                    int spriteid = 0;
                    if (Int32.TryParse(id, out spriteid))
                    {
                        joined.Add(new SpriteProperty("", "", 0, spriteid, false, false, 0, "", indOfActive >= 0, 0));
                    }
                });
            });
            db.Dispose();
            // calculate scores
            Dictionary<int, int[]> spriteScores = new Dictionary<int, int[]>();
            sprites.ForEach(sprite =>
            {
                int score = 0;
                int active = joined.Where(spriteprop => spriteprop.ID == sprite.ID && spriteprop.Activated).ToList().Count;
                int bought = joined.Where(spriteprop => spriteprop.ID == sprite.ID && !spriteprop.Activated).ToList().Count;
                score = active * 10 + bought;
                int[] value = { score, active, bought };
                spriteScores.Add(sprite.ID, value);
            });
            spriteScores = spriteScores.OrderByDescending(score => score.Value[0]).ToDictionary();
            return spriteScores;
        }

        public static string GetLoginOfMember(string id)
        {
            PalantirContext db = new PalantirContext();
            string login;
            try
            {
                login = db.Members.First(m => m.Member1.Contains(id)).Login.ToString();
            }
            catch
            {
                throw new Exception("There is no palantir account connected with this discord account.\nCreate one by messaging Palantir `>login` in DM!");
            }
            db.Dispose();
            return login;
        }

        public static List<SplitReward> GetMemberSplits(int login, PermissionFlag flags)
        {
            PalantirContext db = new PalantirContext();
            List<BoostSplit> sources = db.BoostSplits.ToList();
            List<SplitCredit> splits = db.SplitCredits.Where(s => s.Login == login).ToList();
            db.Dispose();
            List<SplitReward> rewards = splits.ConvertAll(split =>
            {
                SplitReward reward = new SplitReward();
                BoostSplit boostSplit = sources.Find(s => s.Id == split.Split);
                reward.Login = login;
                reward.Split = split.Split;
                reward.ID = boostSplit.Id;
                reward.Value = split.ValueOverride >= 0 ? split.ValueOverride : boostSplit.Value;
                reward.RewardDate = split.RewardDate;
                reward.Name = boostSplit.Name;
                reward.CreateDate = boostSplit.Date;
                reward.Description = boostSplit.Description;
                reward.Comment = split.Comment;
                reward.Expired = 
                    reward.Name.Contains("League") && DateTime.Parse(reward.CreateDate) < DateTime.Now.AddMonths(-4)
                    || (boostSplit.Id == 20 && DateTime.Parse(reward.RewardDate) < DateTime.Now.AddDays(-14))
                    || (boostSplit.Id == 21 && DateTime.Parse(reward.RewardDate) < DateTime.Now.AddDays(-28));

                return reward;
            });

            if (flags.Patron)
            {
                rewards.Add(new SplitReward()
                {
                    Login = login,
                    Split = -1,
                    ID = -1,
                    Value = flags.Patronizer ? 16 : 10,
                    Name = flags.Patronizer ? " 💜 Patronizer Crew" : " 💜 Patron Crew",
                    Description = "Some extra Splits that come with a Typo Patronage on patreon.com",
                    Comment = "",
                    Expired = false
                });
            }

            if (flags.BotAdmin)
            {
                rewards.Add(new SplitReward()
                {
                    Login = login,
                    Split = -1,
                    ID = -1,
                    Value = 9001,
                    Name = "Unlimited POWAAAH",
                    Description = "'its over 9000' ~ sheev palpatine",
                    Comment = "",
                    Expired = false
                });
            }

            if (flags.Booster)
            {
                rewards.Add(new SplitReward()
                {
                    Login = login,
                    Split = -1,
                    ID = -1,
                    Value = 4,
                    Name = "Typo Server Booster",
                    Description = "Rewarded for as long as you boost the Typo Discord Server.",
                    Comment = "",
                    Expired = false
                });
            }

            return rewards;
        }

        public static Dictionary<int, int> GetMemberRainbowShifts(string login)
        {
            PalantirContext db = new PalantirContext();
            string shifts = db.Members.FirstOrDefault(mem => mem.Login.ToString() == login).RainbowSprites;
            db.Dispose();

            Dictionary<int, int> spriteShifts = new();
            if (shifts is not null && shifts != "")
            {
                foreach (string shift in shifts.Split(","))
                {
                    spriteShifts.Add(Convert.ToInt32(shift.Split(":")[0]), Convert.ToInt32(shift.Split(":")[1]));
                }
            }
            return spriteShifts;
        }

        public static void SetMemberRainbowShifts(string login, Dictionary<int, int> shifts)
        {
            List<string> spriteShifts = new();
            shifts.Keys.ForEach(key =>
            {
                if(shifts[key] >= 0) spriteShifts.Add(key.ToString() + ":" + shifts[key].ToString());
            });
            string rainbowSprites = spriteShifts.ToDelimitedString(",");

            PalantirContext db = new PalantirContext();
            db.Members.FirstOrDefault(mem => mem.Login.ToString() == login).RainbowSprites = rainbowSprites;
            db.SaveChanges();
            db.Dispose();
        }

        public static void SetOnlineSprite(string login, string lobbyKey, string lobbyPlayerID){
            List<SpriteProperty> playersprites = GetInventory(login).Where(i => i.Activated).ToList();
            List<SceneProperty> scenes = GetSceneInventory(login, true, false);
            PalantirContext db = new PalantirContext();

            db.OnlineSprites.RemoveRange(db.OnlineSprites.Where(o => o.LobbyKey == lobbyKey && lobbyPlayerID == o.LobbyPlayerId.ToString()));
            try
            {
                db.SaveChanges();
            }
            catch (Exception e) { //Console.WriteLine("Error deleting sprite:\n" + e); 
            }

            if(scenes.Count() > 0)
            {
                OnlineSprite sceneEntity = new()
                {
                    LobbyKey = lobbyKey,
                    LobbyPlayerId = Convert.ToInt32(lobbyPlayerID),
                    Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Slot = -1,
                    Id = lobbyKey + lobbyPlayerID + "scene",
                    Sprite = scenes[0].Id,
                };
                db.OnlineSprites.Add(sceneEntity);
            }

            foreach(SpriteProperty slot in playersprites)
            {
                OnlineSprite newsprite = new();
                newsprite.LobbyKey = lobbyKey;
                newsprite.LobbyPlayerId = Convert.ToInt32(lobbyPlayerID);
                newsprite.Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                newsprite.Sprite = slot is object ? slot.ID : 0;
                newsprite.Slot = slot.Slot + 1;
                newsprite.Id = lobbyKey + lobbyPlayerID + slot.Slot.ToString();
                db.OnlineSprites.Add(newsprite);
            }
            //OnlineSpritesEntity aprilf = new OnlineSpritesEntity();
            //aprilf.LobbyKey = lobbyKey;
            //aprilf.LobbyPlayerID = lobbyPlayerID;
            //aprilf.Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            //aprilf.Sprite = (new Random()).Next(1,185).ToString();
            //aprilf.Slot = 1;
            //aprilf.ID = lobbyKey + lobbyPlayerID + "aprf";
            //context.OnlineSprites.Add(aprilf);
            try
            {
                db.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error writing sprite:\n" + e);
            }

            List<OnlineItem> items = new();

            if (scenes.Count() > 0)
            {
                OnlineItem sceneEntity = new()
                {
                    LobbyKey = lobbyKey,
                    LobbyPlayerId = Convert.ToInt32(lobbyPlayerID),
                    Date = Convert.ToInt32(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()),
                    Slot = 0,
                    ItemType = "scene",
                    ItemId = scenes[0].Id,
                };
                items.Add(sceneEntity);
            }

            var rainbowSprites = BubbleWallet.GetMemberRainbowShifts(login);

            foreach (SpriteProperty slot in playersprites)
            {
                OnlineItem spriteEntity = new()
                {
                    LobbyKey = lobbyKey,
                    LobbyPlayerId = Convert.ToInt32(lobbyPlayerID),
                    Date = Convert.ToInt32(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()),
                    Slot = slot.Slot + 1,
                    ItemType = "sprite",
                    ItemId = slot is object ? slot.ID : 0
                };
                items.Add(spriteEntity);

                if(slot is object && rainbowSprites.ContainsKey(slot.ID))
                {
                    items.Add(new()
                    {
                        LobbyKey = lobbyKey,
                        LobbyPlayerId = Convert.ToInt32(lobbyPlayerID),
                        Date = Convert.ToInt32(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()),
                        Slot = slot.Slot + 1,
                        ItemType = "shift",
                        ItemId = rainbowSprites[slot.ID]
                    });
                }
            }

            db.OnlineItems.RemoveRange(db.OnlineItems.Where(o => o.LobbyKey == lobbyKey && Convert.ToInt32(lobbyPlayerID) == o.LobbyPlayerId && (o.ItemType == "scene" || o.ItemType == "sprite" || o.ItemType == "shift")));
            db.OnlineItems.AddRange(items);

            try
            {
                db.SaveChanges();
            }
            catch (Exception e) { Console.WriteLine("Error writing item:\n" + e); 
            }
            db.Dispose();
        }

        public static int GetRemainingEventDrops(string login, int eventDropID)
        {
            List<SpriteProperty> inv = GetInventory(login);
            int total = GetEventCredit(login, eventDropID);
            inv.ForEach(s =>
            {
                if (s.EventDropID == eventDropID) total -= s.Cost;
            });
            return total;
        }

        public static bool ChangeEventDropCredit(string login, int eventDropID, int difference)
        {
            PalantirContext db = new PalantirContext();
            EventCredit credit = db.EventCredits.FirstOrDefault(c => c.EventDropId == eventDropID && c.Login.ToString() == login);
            if(credit is object)
            {
                try
                {
                    credit.Credit += difference;
                    db.SaveChanges();
                    db.Dispose();
                }
                catch (Exception e)
                {
                    db.Dispose();
                    Console.WriteLine("Error changing credits for " + login + ":\n" + e);
                    return false;
                }
            }
            else if (difference > 0)
            {
                EventCredit newCredit = new();
                newCredit.Login = Convert.ToInt32(login);
                newCredit.EventDropId = eventDropID;
                newCredit.Credit = difference;
                try
                {
                    db.EventCredits.Add(newCredit);
                    db.SaveChanges();
                    db.Dispose();
                }
                catch (Exception e)
                {
                    db.Dispose();
                    Console.WriteLine("Error changing credits for " + login + ":\n" + e);
                    return false;
                }
            }
            
            return true;
        }

        public static SceneProperty GetSceneProperty(Scene scene, bool activated)
        {
            return new SceneProperty()
            {
                Activated = activated,
                Artist = scene.Artist,
                Color = scene.Color,
                Id = scene.Id,
                Name = scene.Name,
                Url = scene.Url,
                EventId = scene.EventId,
                Exclusive = scene.Exclusive
            };
        }

        public static SpriteProfile GetCurrentSpriteProfile(string login)
        {
            PalantirContext db = new PalantirContext();
            var member = db.Members.FirstOrDefault(m => m.Login.ToString() == login);

            db.Dispose();

            var sprites = BubbleWallet.ParseSpriteInventory(member.Sprites).Where(s => s.Activated).OrderBy(s => s.Slot).Select(s=>s.ID).ToList().ToDelimitedString(",");
            var scenes = BubbleWallet.GetSceneInventory(login, false, false).Where(s=>s.Activated).Select(s => s.Id).ToList().ToDelimitedString(",");
            var shifts = member.RainbowSprites;

            return new()
            {
                Name = "",
                Login = Convert.ToInt32(login),
                RainbowSprites = shifts,
                Combo = sprites,
                Scene = scenes
            };
        }

        public static void SaveSpriteProfile(SpriteProfile profile, bool delete = false)
        {
            PalantirContext db = new PalantirContext();
            db.SpriteProfiles.RemoveRange(db.SpriteProfiles.Where(p => p.Login == profile.Login && p.Name == profile.Name));
            if(!delete) db.SpriteProfiles.Add(profile);
            db.SaveChanges();
            db.Dispose();
        }

        public static List<SpriteProfile> GetSpriteProfiles(string login)
        {
            PalantirContext db = new PalantirContext();
            var profiles = db.SpriteProfiles.Where(p => p.Login.ToString() == login).ToList();
            db.Dispose();

            return profiles;
        }

        public static TimeSpan AwardPackCooldown(int login)
        {
            TimeSpan cooldown = new();
            PalantirContext db = new();
            var member = db.Members.FirstOrDefault(m => m.Login == login);
            var flags = new PermissionFlag(Convert.ToInt16(member.Flag));
            if (member.AwardPackOpened == null || flags.BotAdmin) cooldown = TimeSpan.FromSeconds(0);
            else
            {
                cooldown = TimeSpan.FromMilliseconds(TimeSpan.FromDays(flags.Patron ? 5 : 7).TotalMilliseconds - (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - Convert.ToDouble(member.AwardPackOpened)));
                if (cooldown.TotalSeconds < 0) cooldown = TimeSpan.FromSeconds(0);
            }
            db.Dispose();
            return cooldown;
        }

        public static AwardPackLevel GetAwardPackLevel(int login)
        {
            PalantirContext ctx = new();
            var lastTraces = ctx.BubbleTraces.Where(t => t.Login == login).Select(t => t.Bubbles).OrderByDescending(t => t).ToList();
            var startBubbles = lastTraces.Count() > 6 ? lastTraces.ElementAt(6) : 0;
            var endBubbles = ctx.Members.FirstOrDefault(m => m.Login == login).Bubbles;
            var bubbles = endBubbles - startBubbles;
            ctx.Dispose();

            var level = new AwardPackLevel();
            level.CollectedBubbles = bubbles;
            if (bubbles > 15000) level.Rarity = AwardRarity.Legendary;
            if (bubbles > 5000) level.Rarity = AwardRarity.Epic;
            if (bubbles > 2500) level.Rarity = AwardRarity.Special;
            else level.Rarity = AwardRarity.Common;
            return level;
        }

        public static List<MappedAwardInv> OpenAwardPack(int login, AwardPackLevel packLevel)
        {
            double[] range =  
                packLevel.Rarity == AwardRarity.Common ? new double[] { 0.55, 0.8, 0.97 } :
                packLevel.Rarity == AwardRarity.Special ? new double[] { 0.4, 0.7, 0.95 } :
                packLevel.Rarity == AwardRarity.Epic ? new double[] { 0.3, 0.5, 0.91 } :
                new double[] { 0.2, 0.4, 0.85 };

            var available = GetAwards();

            Award GetAward()
            {
                var result = new Random().NextDouble();

                AwardRarity awardRarity;
                if (result > range[2]) awardRarity = AwardRarity.Legendary;
                else if (result > range[1]) awardRarity = AwardRarity.Epic;
                else if (result > range[0]) awardRarity = AwardRarity.Special;
                else awardRarity = AwardRarity.Common;

                var awardResult = available.Where(a => a.Rarity == (int)awardRarity).RandomSubset(1).First();
                return awardResult;
            }

            var drawnAwards = (new List<Award>() { GetAward(), GetAward() }).ConvertAll(award =>
            {
                var awardee = new Awardee();
                awardee.Award = Convert.ToInt16(award.Id);
                awardee.OwnerLogin = login;

                var inv = new MappedAwardInv();
                inv.award = award;
                inv.inv = awardee;
                return inv;
            });

            var context = new PalantirContext();
            context.Awardees.AddRange(drawnAwards.ConvertAll(a => a.inv));
            context.Members.FirstOrDefault(m => m.Login == login).AwardPackOpened = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            context.SaveChanges();
            context.Dispose();

            return drawnAwards;
        }

        public static List<MappedAwardInv> GetAwardInventory(int login)
        {

            PalantirContext db = new PalantirContext();
            var inv = db.Awardees.Where(p => p.OwnerLogin == login && p.AwardeeLogin == null).ToList();
            db.Dispose();
            var awards = GetAwards();

            return inv.ConvertAll(i => new MappedAwardInv() { inv = i, award = awards.FirstOrDefault(a => a.Id == i.Award)});
        }

        public static List<Award> GetAwards()
        {

            PalantirContext db = new PalantirContext();
            var awards = db.Awards.ToList();
            db.Dispose();

            return awards;
        }

        public static List<MappedAwardInv> GetGivendAwards(int login)
        {

            PalantirContext db = new PalantirContext();
            var inv = db.Awardees.Where(p => p.OwnerLogin == login && p.AwardeeLogin != null).ToList();
            db.Dispose();
            var awards = GetAwards();

            return inv.ConvertAll(i => new MappedAwardInv() { inv = i, award = awards.FirstOrDefault(a => a.Id == i.Award) });
        }

        public static List<MappedAwardInv> GetReceivedAwards(int login)
        {

            PalantirContext db = new PalantirContext();
            var inv = db.Awardees.Where(p => p.AwardeeLogin == login).ToList();
            db.Dispose();
            var awards = GetAwards();

            return inv.ConvertAll(i => new MappedAwardInv() { inv = i, award = awards.FirstOrDefault(a => a.Id == i.Award) });
        }

        public static List<MappedAwardGalleryInv> GetAwardGallery(int login)
        {
            var awards = GetReceivedAwards(login);
            var imageIds = awards.Select(a => a.inv.ImageId);
            var ctx = new PalantirContext();
            var tags = ctx.CloudTags.Where(t => t.Owner == login && imageIds.Any(id => id == t.ImageId)).ToList();
            ctx.Dispose();

            return awards.ConvertAll(a => new MappedAwardGalleryInv() { inv= a.inv, award= a.award, image= tags.Find(t => t.ImageId == a.inv.ImageId) });
        }

        public static string GetRarityIcon(int rarity)
        {
            return new string[] {
                "<a:common_award:1175247351359737926>",
                "<a:special_award:1175247327309598730>",
                "<a:epic_award:1175247311660658709>",
                "<a:legendary_award:1175245828189859930>"
            } [rarity - 1];
        }

        public static string GetUsername(int login)
        {
            var ctx = new PalantirContext();
            var user = ctx.Members.FirstOrDefault(u => u.Login == login).Member1;
            ctx.Dispose();

            return JsonConvert.DeserializeObject<Member>(user).UserName;
        }
    }

    public class AwardPackLevel
    {
        public AwardRarity Rarity;
        public int CollectedBubbles;
    }

    public enum AwardRarity
    {
        Common = 1,
        Special = 2,
        Epic = 3,
        Legendary = 4
    }

    public class MappedAwardInv
    {
        public Awardee inv;
        public Award award;
    }

    public class MappedAwardGalleryInv : MappedAwardInv
    {
        public CloudTag image;
    }

    public class Sprite
    {
        public int ID;
        public string Name;
        public string URL;
        public int Cost;
        public bool Special;
        public bool Rainbow;
        public int EventDropID;
        public string Artist;
        public Sprite(string name, string url, int cost, int id, bool special, bool rainbow, int eventDropID, string artist)
        {
            Name = name;
            URL = url;
            Cost = cost;
            ID = id;
            Special = special;
            EventDropID = eventDropID;
            Artist = artist;
            Rainbow = rainbow;
        }
    }

    public class SpriteProperty : Sprite
    {
        public bool Activated;
        public int Slot;
        public SpriteProperty(string name, string url, int cost, int id, bool special, bool rainbow, int eventdropID, string artist, bool activated, int slot) : base(name,url,cost,id, special, rainbow, eventdropID, artist)
        {
            Activated = activated;
            Slot = activated ? slot : -1;
        }
    }

    public class SceneProperty : Scene
    {
        public bool Activated;
    }

    public class SplitReward : SplitCredit
    {
        public int ID;
        public int Value;
        public string Name;
        public string Description;
        public string CreateDate;
        public bool Expired;
    }
}
