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
            for(int i=0; i<sprites.Length; i++)
            {
                bool own = false;
                if(sprites[i] == '.') { own = true; i++; }
                Sprite sprite;
                try { sprite = availableSprites.FirstOrDefault(s => s.ID == sprites[i]);
                    spriteInventory.Add(new SpriteProperty(sprite.Name, sprite.URL, sprite.Cost, sprite.ID, own));
                }
                catch(Exception e) {
                    Console.WriteLine(sprites[i] + " : " + i + ": " + e.ToString());
                    try
                    {
                        sprite = new Sprite("hi", "hi", 1, 1);
                        spriteInventory.Add(new SpriteProperty(sprite.Name, sprite.URL, sprite.Cost, sprite.ID, own));
                    }
                    catch (Exception f) { Console.WriteLine(sprites[i] + " :f " + i + ": " + f.ToString() + spriteInventory.ToString()); }
                }
            }
            return spriteInventory;
        }

        public static List<Sprite> GetAvailableSprites()
        {
            List<Sprite> sprites = new List<Sprite>();
            PalantirDbContext context = new PalantirDbContext();
            context.Sprites.ToList().ForEach(s => sprites.Add(new Sprite(s.Name, s.URL, s.Cost, s.ID)));
            context.SaveChanges();
            context.Dispose();
            return sprites;
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
    }

    public class Sprite
    {
        public int ID;
        public string Name;
        public string URL;
        public int Cost;
        public Sprite(string name, string url, int cost, int id)
        {
            Name = name;
            URL = url;
            Cost = cost;
            ID = id;
        }
    }

    public class SpriteProperty : Sprite
    {
        public bool Activated;
        public SpriteProperty(string name, string url, int cost, int id, bool activated) : base(name,url,cost,id)
        {
            Activated = activated;
        }
    }
}
