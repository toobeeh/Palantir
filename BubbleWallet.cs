using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Quartz;
using MoreLinq;

namespace Palantir
{

    public class BubbleCounter : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            string[] logins = BubbleWallet.loginBubbleTicks.Distinct().ToArray();
            BubbleWallet.loginBubbleTicks = new List<string>();
            Dictionary<string, int> cache = new Dictionary<string, int>();
            PalantirDbContext dbcontext = new PalantirDbContext();
            try
            {
                foreach (string login in logins)
                {
                    MemberEntity member = dbcontext.Members.FirstOrDefault(s => s.Login == login);
                    if(JsonConvert.DeserializeObject<Member>(member.Member).Guilds.Count > 0) member.Bubbles++;
                }
            }
            catch(Exception e) { Console.WriteLine(e.ToString()); }
            await dbcontext.SaveChangesAsync();
            foreach(MemberEntity member in dbcontext.Members)
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
            BubbleCache.TryGetValue(login, out bubbles);
            return bubbles;
        }

        public static int GetDrops(string login, string userid)
        {
            PalantirDbContext context = new PalantirDbContext();
            MemberEntity entity = context.Members.FirstOrDefault(s => s.Login == login);
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
            PalantirDbContext db = new();
            List<BubbleTraceEntity> bubbleTraces = db.BubbleTraces.Where(trace => trace.Login == login).ToList();
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
            PalantirDbContext context = new PalantirDbContext();
            context.Members.FirstOrDefault(m => m.Login == login).Sprites = "0," + inv;
            context.SaveChanges();
            context.Dispose();
            return inv;
        }

        public static List<Sprite> GetAvailableSprites()
        {
            List<Sprite> sprites = new List<Sprite>();
            PalantirDbContext context = new PalantirDbContext();
            context.Sprites.ToList().ForEach(s => sprites.Add(new Sprite(s.Name, s.URL, s.Cost, s.ID, s.Special, s.Rainbow, s.EventDropID, s.Artist)));
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
            PalantirDbContext context = new PalantirDbContext();
            SpritesEntity s = new SpritesEntity();
            s.ID = sprite.ID;
            s.Name = sprite.Name;
            s.Special = sprite.Special;
            s.URL = sprite.URL;
            s.EventDropID = sprite.EventDropID;
            s.Cost = sprite.Cost;
            s.Artist = sprite.Artist;
            context.Sprites.Add(s);
            context.SaveChanges();
            context.Dispose();
        }

        public static SceneEntity AddScene(string name, string color, string guessedColor, string artist, string url, int eventID)
        {
            PalantirDbContext context = new PalantirDbContext();
            int id = context.Scenes.Count() > 0 ? context.Scenes.Select(s => s.ID).Max() + 1 : 1;
            SceneEntity scene = new SceneEntity()
            {
                Artist = artist,
                Color = color,
                GuessedColor = guessedColor,
                URL = url,
                Name = name,
                ID = id,
                EventID = eventID
            };
            context.Scenes.Add(scene);
            context.SaveChanges();
            context.Dispose();
            return scene;
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
                total -= nextPrice;
                nextPrice *= ScenePriceFactor;
            }
            return total;
        }

        public static void BuyScene(string login, int id)
        {
            PalantirDbContext db = new();
            MemberEntity member = db.Members.FirstOrDefault(member => member.Login == login);
            string sceneInv = GetSceneInventory(login, false, false).ConvertAll(scene => (scene.Activated ? "." : "") + scene.ID.ToString()).ToDelimitedString(",");
            sceneInv += (sceneInv.Length > 0 ? "," : "") + id.ToString();
            member.Scenes = sceneInv;
            db.SaveChanges();
            db.Dispose();
        }

        public static void SetSceneInventory(string login, List<SceneProperty> inv)
        {
            PalantirDbContext db = new();
            db.Members.FirstOrDefault(member => member.Login == login).Scenes = inv
                .ConvertAll(scene => (scene.Activated ? "." : "") + scene.ID.ToString())
                .ToDelimitedString(",");
            db.SaveChanges();
            db.Dispose();
        }

        public static int GetEventCredit(string login, int eventDropID)
        {
            PalantirDbContext context = new PalantirDbContext();
            int credit = 0;
            try { 
                credit = context.EventCredits.FirstOrDefault(c => c.EventDropID == eventDropID && c.Login == login).Credit;
            }
            catch { }
            
            context.Dispose();
            return credit;
        }
        public static List<SceneEntity> GetAvailableScenes()
        {
            PalantirDbContext db = new PalantirDbContext();
            List<SceneEntity> scenes = db.Scenes.ToList();
            db.Dispose();
            return scenes;
        }

        public static List<SpriteProperty> GetInventory(string login)
        {
            PalantirDbContext context = new PalantirDbContext();
            string inventoryString = context.Members.FirstOrDefault(m => m.Login == login).Sprites;
            context.Dispose();
            return ParseSpriteInventory(inventoryString);
        }

        public static List<SceneProperty> GetSceneInventory(string login, bool onlyActive = false, bool noEventScenes = true)
        {
            PalantirDbContext context = new PalantirDbContext();
            string inventoryString = context.Members.FirstOrDefault(m => m.Login == login).Scenes;
            if (String.IsNullOrEmpty(inventoryString)) return new List<SceneProperty>();
            List<SceneProperty> inv = inventoryString.Split(",")
                .Where(id => !onlyActive || onlyActive && id.Contains("."))
                .ToList()
                .ConvertAll(id => GetSceneProperty(
                    context.Scenes.ToList().FirstOrDefault(scene => scene.ID.ToString() == id.Replace(".", "")), id.Contains('.')
                    ));
            context.Dispose();
            if (noEventScenes) inv = inv.Where(s => s.EventID == 0).ToList();
            return inv;
        }

        public static bool IsEarlyUser(string login)
        {
            PalantirDbContext context = new PalantirDbContext();
            bool exists = context.BubbleTraces.Any(trace => trace.Date == "31/08/2020" && trace.Login == login);
            context.Dispose();
            return exists;
        }
        public static string FirstTrace(string login)
        {
            PalantirDbContext context = new PalantirDbContext();
            List<string> dates = context.BubbleTraces.Where(trace => trace.Login == login).Select(trace => trace.Date).ToList();
            context.Dispose();
            return dates.Min(date => DateTime.Parse(date)).ToShortDateString();
        }
        public static List<int> ParticipatedEvents(string login)
        {
            List<int> events = new List<int>();
            PalantirDbContext context = new PalantirDbContext();
            List<int> eventdrops = context.EventCredits.Where(credit => credit.Login == login).Select(credit => credit.EventDropID).ToList();
            eventdrops.ForEach(caughtdrop =>
            {
                int eventid = context.EventDrops.FirstOrDefault(drop => caughtdrop == drop.EventDropID).EventID;
                if (!events.Contains(eventid)) events.Add(eventid);
            });
            context.Dispose();
            return events;
        }
        public static int CaughtEventdrops(string discordID)
        {
            PalantirDbContext context = new PalantirDbContext();
            int caught = context.PastDrops.Where(drop => drop.CaughtLobbyPlayerID == discordID && drop.EventDropID > 0 && drop.LeagueWeight == 0).Count();
            context.Dispose();
            return caught;
        }

        public static int GlobalRanking(string login, bool drops = false)
        {
            PalantirDbContext context = new PalantirDbContext();
            int index = context.Members.ToList().Where(member=> !(new PermissionFlag((byte)member.Flag).BubbleFarming)).OrderByDescending(member => drops ? member.Drops : member.Bubbles).Select(member => member.Login).ToList().IndexOf(login) + 1;
            context.Dispose();
            return index;
        }

        public static Dictionary<int, int[]> SpriteScoreboard()
        {
            List<Sprite> sprites = BubbleWallet.GetAvailableSprites();
            // get all bought sprites
            List<SpriteProperty> joined = new List<SpriteProperty>();
            PalantirDbContext db = new PalantirDbContext();
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
            PalantirDbContext context = new PalantirDbContext();
            string login;
            try
            {
                login = context.Members.First(m => m.Member.Contains(id)).Login;
            }
            catch
            {
                throw new Exception("There is no palantir account connected with this discord account.\nCreate one by messaging Palantir `>login` in DM!");
            }
            context.Dispose();
            return login;
        }

        public static List<SplitReward> GetMemberSplits(int login, PermissionFlag flags)
        {
            PalantirDbContext context = new PalantirDbContext();
            List<BoostSplitEntity> sources = context.BoostSplits.ToList();
            List<SplitCreditEntity> splits = context.SplitCredits.Where(s => s.Login == login).ToList();
            context.Dispose();
            List<SplitReward> rewards = splits.ConvertAll(split =>
            {
                SplitReward reward = new SplitReward();
                BoostSplitEntity boostSplit = sources.Find(s => s.ID == split.Split);
                reward.Login = login;
                reward.Split = split.Split;
                reward.ID = boostSplit.ID;
                reward.Value = split.ValueOverride >= 0 ? split.ValueOverride : boostSplit.Value;
                reward.RewardDate = split.RewardDate;
                reward.Name = boostSplit.Name;
                reward.CreateDate = boostSplit.Date;
                reward.Description = boostSplit.Description;
                reward.Comment = split.Comment;

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
                    Comment = ""
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
                    Comment = ""
                });
            }

            return rewards;
        }

        public static void SetOnlineSprite(string login, string lobbyKey, string lobbyPlayerID){
            List<SpriteProperty> playersprites = GetInventory(login).Where(i => i.Activated).ToList();
            List<SceneProperty> scenes = GetSceneInventory(login, true, false);
            PalantirDbContext context = new PalantirDbContext();

            context.OnlineSprites.RemoveRange(context.OnlineSprites.Where(o => o.LobbyKey == lobbyKey && lobbyPlayerID == o.LobbyPlayerID));
            try
            {
                context.SaveChanges();
            }
            catch (Exception e) { //Console.WriteLine("Error deleting sprite:\n" + e); 
            }

            if(scenes.Count() > 0)
            {
                OnlineSpritesEntity sceneEntity = new OnlineSpritesEntity()
                {
                    LobbyKey = lobbyKey,
                    LobbyPlayerID = lobbyPlayerID,
                    Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Slot = -1,
                    ID = lobbyKey + lobbyPlayerID + "scene",
                    Sprite = scenes[0].ID.ToString(),
                };
                context.OnlineSprites.Add(sceneEntity);
            }

            foreach(SpriteProperty slot in playersprites)
            {
                OnlineSpritesEntity newsprite = new OnlineSpritesEntity();
                newsprite.LobbyKey = lobbyKey;
                newsprite.LobbyPlayerID = lobbyPlayerID;
                newsprite.Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                newsprite.Sprite = slot is object ? slot.ID.ToString() : "0";
                newsprite.Slot = slot.Slot + 1;
                newsprite.ID = lobbyKey + lobbyPlayerID + slot.Slot.ToString();
                context.OnlineSprites.Add(newsprite);
            }
            //OnlineSpritesEntity aprilf = new OnlineSpritesEntity();
            //aprilf.LobbyKey = lobbyKey;
            //aprilf.LobbyPlayerID = lobbyPlayerID;
            //aprilf.Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            //aprilf.Sprite = (new Random()).Next(1,185).ToString();
            //aprilf.Slot = 1;
            //aprilf.ID = lobbyKey + lobbyPlayerID + "aprf";
            //context.OnlineSprites.Add(aprilf);

            // now the new table
            context.OnlineItems.RemoveRange(context.OnlineItems.Where(o => o.LobbyKey == lobbyKey && lobbyPlayerID == o.LobbyPlayerID));

            try
            {
                context.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error writing sprite:\n" + e);
            }

            if (scenes.Count() > 0)
            {
                OnlineItemsEntity sceneEntity = new()
                {
                    LobbyKey = lobbyKey,
                    LobbyPlayerID = lobbyPlayerID,
                    Date = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                    Slot = 0,
                    ItemType = "scene",
                    ItemID = scenes[0].ID,
                };
                context.OnlineItems.Add(sceneEntity);
            }

            foreach (SpriteProperty slot in playersprites)
            {
                OnlineItemsEntity spriteEntity = new()
                {
                    LobbyKey = lobbyKey,
                    LobbyPlayerID = lobbyPlayerID,
                    Date = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                    Slot = slot.Slot + 1,
                    ItemType = "sprite",
                    ItemID = slot is object ? slot.ID : 0
                };
                context.OnlineItems.Add(spriteEntity);
            }

            try
            {
                context.SaveChanges();
            }
            catch (Exception e) { Console.WriteLine("Error writing sprite:\n" + e); 
            }
            context.Dispose();
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
            PalantirDbContext context = new PalantirDbContext();
            EventCreditEntity credit = context.EventCredits.FirstOrDefault(c => c.EventDropID == eventDropID && c.Login == login);
            if(credit is object)
            {
                try
                {
                    credit.Credit += difference;
                    context.SaveChanges();
                    context.Dispose();
                }
                catch (Exception e)
                {
                    context.Dispose();
                    Console.WriteLine("Error changing credits for " + login + ":\n" + e);
                    return false;
                }
            }
            else if (difference > 0)
            {
                EventCreditEntity newCredit = new EventCreditEntity();
                newCredit.Login = login;
                newCredit.EventDropID = eventDropID;
                newCredit.Credit = difference;
                try
                {
                    context.EventCredits.Add(newCredit);
                    context.SaveChanges();
                    context.Dispose();
                }
                catch (Exception e)
                {
                    context.Dispose();
                    Console.WriteLine("Error changing credits for " + login + ":\n" + e);
                    return false;
                }
            }
            
            return true;
        }

        public static SceneProperty GetSceneProperty(SceneEntity scene, bool activated)
        {
            return new SceneProperty()
            {
                Activated = activated,
                Artist = scene.Artist,
                Color = scene.Color,
                ID = scene.ID,
                Name = scene.Name,
                URL = scene.URL,
                EventID = scene.EventID
            };
        }

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

    public class SceneProperty : SceneEntity
    {
        public bool Activated;
    }

    public class SplitReward : SplitCreditEntity
    {
        public int ID;
        public int Value;
        public string Name;
        public string Description;
        public string CreateDate;
    }
}
