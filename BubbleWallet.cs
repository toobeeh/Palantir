using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;

namespace Palantir
{
    public class BubbleWallet
    {
        public static Dictionary<string, DateTime> Ticks = new Dictionary<string, DateTime>();
        public static void AddBubble(string login)
        {
            // Remove all ticks that passed the max tick interval
            Ticks.Where(tick => (tick.Value < DateTime.Now.AddSeconds(-10))).ToList().ForEach(tick => Ticks.Remove(tick.Key));

            if (Ticks.ContainsKey(login)) return;

            Ticks.Add(login, DateTime.Now);

            PalantirDbContext context = new PalantirDbContext();
            MemberEntity entity = context.Members.FirstOrDefault(s => s.Login == login);

            if (entity != null)
            {
                entity.Bubbles++;
                context.SaveChanges();
            }
            context.SaveChanges();
            context.Dispose();
        }

        public static int GetBubbles(string login)
        {
            PalantirDbContext context = new PalantirDbContext();
            MemberEntity entity = context.Members.FirstOrDefault(s => s.Login == login);
            int bubbles = 0;

            if (entity != null)
            {
                bubbles = entity.Bubbles;
            }
            context.SaveChanges();
            context.Dispose();

            return bubbles;
        }

        public static List<SpriteProperty> ParseSpriteInventory(string sprites)
        {
            List<Sprite> availableSprites = GetAvailableSprites();
            List<SpriteProperty> spriteInventory = new List<SpriteProperty>();
            List<string> spriteIds = sprites.Split(',').ToList();
            spriteIds.ForEach(i =>
            {
                bool own = false;
                if (i.StartsWith(".")) { own = true; i = i.Replace(".", ""); }
                availableSprites.ForEach(s =>
                {
                    try
                    {
                        int id;
                        if (int.TryParse(i,out id) && id == s.ID) spriteInventory.Add(new SpriteProperty(s.Name, s.URL, s.Cost, s.ID, s.Special, own));
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
            sprites.ForEach(s =>
            {
                if(available.Any(a=>a.ID == s.ID)) inv += (s.Activated ? "." : "") + s.ID + ",";
            });
            inv = inv.Remove(inv.Length - 1);
            if (inv[0] == '0') inv = inv.Substring(1);
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
            context.Sprites.ToList().ForEach(s => sprites.Add(new Sprite(s.Name, s.URL, s.Cost, s.ID, s.Special)));
            context.SaveChanges();
            context.Dispose();
            return sprites;
        }
        public static Sprite GetSpriteByID(int id)
        {
            return GetAvailableSprites().FirstOrDefault(s => s.ID == id);
        }

        public static int CalculateCredit(string login)
        {
            int total = GetBubbles(login);
            GetInventory(login).ForEach(s =>
            {
                total -= s.Cost;
            });
            return total;
        }

        public static List<SpriteProperty> GetInventory(string login)
        {
            PalantirDbContext context = new PalantirDbContext();
            string inventoryString = context.Members.FirstOrDefault(m => m.Login == login).Sprites;
            context.SaveChanges();
            context.Dispose();
            return ParseSpriteInventory(inventoryString);
        }

        public static string GetLoginOfMember(string id)
        {
            PalantirDbContext context = new PalantirDbContext();
            string login = context.Members.FirstOrDefault(m => m.Member.Contains(id)).Login;
            context.SaveChanges();
            context.Dispose();
            return login;
        }

        public static void SetOnlineSprite(string login, string lobbyKey, string lobbyPlayerID){
            Sprite playersprite = GetInventory(login).FirstOrDefault(i => i.Activated);
            PalantirDbContext context = new PalantirDbContext();

            context.OnlineSprites.RemoveRange(context.OnlineSprites.Where(o => o.LobbyKey == lobbyKey && lobbyPlayerID == o.LobbyPlayerID));
            try
            {
                context.SaveChanges();
            }
            catch (Exception e) { Console.WriteLine("Error deleting sprite:\n" + e); }

            OnlineSpritesEntity newsprite = new OnlineSpritesEntity();
            newsprite.LobbyKey = lobbyKey;
            newsprite.LobbyPlayerID = lobbyPlayerID;
            newsprite.Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            newsprite.Sprite = playersprite is object ? playersprite.ID.ToString() : "0";
            context.OnlineSprites.Add(newsprite);
           
            try
            {
                context.SaveChanges();
            }
            catch (Exception e) { Console.WriteLine("Error writing sprite:\n" + e); }
            context.Dispose();
        }

    }

    public class Sprite
    {
        public int ID;
        public string Name;
        public string URL;
        public int Cost;
        public bool Special;
        public Sprite(string name, string url, int cost, int id, bool special)
        {
            Name = name;
            URL = url;
            Cost = cost;
            ID = id;
            Special = special;
        }
    }

    public class SpriteProperty : Sprite
    {
        public bool Activated;
        public SpriteProperty(string name, string url, int cost, int id, bool special, bool activated) : base(name,url,cost,id, special)
        {
            Activated = activated;
        }
    }
}
