using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Newtonsoft.Json;

namespace Palantir
{
    public static class BubbleWallet
    {
        public static Dictionary<string, DateTime> Ticks = new Dictionary<string, DateTime>();
        public static void AddBubble(string login)
        {
            // Remove all ticks that passed the max tick interval
            Ticks.Where(tick => (tick.Value < DateTime.Now.AddSeconds(-10))).ToList().ForEach(tick => { if (tick.Key != null) Ticks.Remove(tick.Key); });
            if (Ticks.TryAdd(login, DateTime.Now))
            {
                PalantirDbContext context = new PalantirDbContext();
                MemberEntity entity = context.Members.FirstOrDefault(s => s.Login == login);

                if (entity != null)
                {
                    entity.Bubbles++;
                    context.SaveChanges();
                }
                context.Dispose();
            } 
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
                bool own = false;
                if (i.StartsWith(".")) { own = true; i = i.Replace(".", ""); }
                availableSprites.ForEach(s =>
                {
                    try
                    {
                        int id;
                        if (int.TryParse(i,out id) && id == s.ID) spriteInventory.Add(new SpriteProperty(s.Name, s.URL, s.Cost, s.ID, s.Special, s.EventDropID, own));
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
            context.Sprites.ToList().ForEach(s => sprites.Add(new Sprite(s.Name, s.URL, s.Cost, s.ID, s.Special, s.EventDropID)));
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
            if (context.EventCredits.Any(c => c.EventDropID == eventDropID && c.Login == login))
            {
                credit = context.EventCredits.FirstOrDefault(c => c.EventDropID == eventDropID && c.Login == login).Credit;
            }
            
            context.Dispose();
            return credit;
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
            catch (Exception e) { //Console.WriteLine("Error deleting sprite:\n" + e); 
            }

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
            catch (Exception e) { //Console.WriteLine("Error writing sprite:\n" + e); 
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
        public Sprite(string name, string url, int cost, int id, bool special, int eventDropID)
        {
            Name = name;
            URL = url;
            Cost = cost;
            ID = id;
            Special = special;
            EventDropID = eventDropID;
        }
    }

    public class SpriteProperty : Sprite
    {
        public bool Activated;
        public SpriteProperty(string name, string url, int cost, int id, bool special, int eventID, bool activated) : base(name,url,cost,id, special, eventID)
        {
            Activated = activated;
        }
    }
}
