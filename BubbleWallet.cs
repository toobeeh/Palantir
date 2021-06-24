using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Quartz;

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

        public static int GetDrops(string login)
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

            return drops;
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
                        if (int.TryParse(i,out id) && id == s.ID) spriteInventory.Add(new SpriteProperty(s.Name, s.URL, s.Cost, s.ID, s.Special, s.EventDropID, s.Artist, activated, slot));
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
            context.Sprites.ToList().ForEach(s => sprites.Add(new Sprite(s.Name, s.URL, s.Cost, s.ID, s.Special, s.EventDropID, s.Artist)));
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

        public static int CalculateCredit(string login)
        {
            int total = GetBubbles(login);
            GetInventory(login).ForEach(s =>
            {
                if(s.EventDropID <= 0) total -= s.Cost;
            });
            total += GetDrops(login) * 50;
            return total;
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

        public static List<SpriteProperty> GetInventory(string login)
        {
            PalantirDbContext context = new PalantirDbContext();
            string inventoryString = context.Members.FirstOrDefault(m => m.Login == login).Sprites;
            context.Dispose();
            return ParseSpriteInventory(inventoryString);
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
            List<int> eventdrops = context.EventCredits.Where(credit => credit.Login == login).Select(credit => credit.EventDropID).Distinct().ToList();
            eventdrops.ForEach(drop =>
            {
                int eventid = context.EventDrops.FirstOrDefault(drop => drop.EventDropID == drop.EventDropID).EventID;
                if (!events.Contains(eventid)) events.Add(eventid);
            });
            context.Dispose();
            return events;
        }

        public static int GlobalRanking(string login, bool drops = false)
        {
            PalantirDbContext context = new PalantirDbContext();
            int index = context.Members.OrderByDescending(member => drops ? member.Drops : member.Bubbles).Select(member => member.Login).ToList().IndexOf(login);
            context.Dispose();
            return index;
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

        public static void SetOnlineSprite(string login, string lobbyKey, string lobbyPlayerID){
            List<SpriteProperty> playersprites = GetInventory(login).Where(i => i.Activated).ToList();
            PalantirDbContext context = new PalantirDbContext();

            context.OnlineSprites.RemoveRange(context.OnlineSprites.Where(o => o.LobbyKey == lobbyKey && lobbyPlayerID == o.LobbyPlayerID));
            try
            {
                context.SaveChanges();
            }
            catch (Exception e) { //Console.WriteLine("Error deleting sprite:\n" + e); 
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
            //aprilf.Sprite = (new Random()).Next(1,83).ToString();
            //aprilf.Slot = 1;
            //aprilf.ID = lobbyKey + lobbyPlayerID + "aprf";
            //context.OnlineSprites.Add(aprilf);
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

    }

    public class Sprite
    {
        public int ID;
        public string Name;
        public string URL;
        public int Cost;
        public bool Special;
        public int EventDropID;
        public string Artist;
        public Sprite(string name, string url, int cost, int id, bool special, int eventDropID, string artist)
        {
            Name = name;
            URL = url;
            Cost = cost;
            ID = id;
            Special = special;
            EventDropID = eventDropID;
            Artist = artist;
        }
    }

    public class SpriteProperty : Sprite
    {
        public bool Activated;
        public int Slot;
        public SpriteProperty(string name, string url, int cost, int id, bool special, int eventdropID, string artist, bool activated, int slot) : base(name,url,cost,id, special, eventdropID, artist)
        {
            Activated = activated;
            Slot = activated ? slot : -1;
        }
    }
}
