using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using Palantir.Model;

namespace Palantir
{
    public class DataManager
    {
        public List<Tether> PalantirTethers;
        public List<Member> PalantirMembers;
        public Dictionary<string, string> PatronEmojis = new Dictionary<string, string>();
        public int PatronCount = 0;

        public DataManager()
        {
            LoadConnections();
            UpdateMemberGuilds();
        }

        public void LoadConnections()
        {
            PalantirContext Database = new PalantirContext();
            PalantirTethers = new List<Tether>();
            foreach (Palantiri palantirEntity in Database.Palantiris.ToList())
            {
                Tether tether;
                ObservedGuild guild = JsonConvert.DeserializeObject<ObservedGuild>(palantirEntity.Palantir);
                // if more than one member connected
                if(Database.Members.Count(
                    member => member.Member1.Contains(guild.GuildID.ToString())) > 0)
                {
                    if (Database.GuildSettings.Any(s => s.GuildId == guild.GuildID))
                        tether = new Tether(guild, JsonConvert.DeserializeObject<GuildSettings>(Database.GuildSettings.FirstOrDefault(s => s.GuildId == guild.GuildID).Settings));
                    else tether = new Tether(guild);
                    PalantirTethers.Add(tether);
                }
                else
                {
                    Console.WriteLine("Didn't add guild " + guild.GuildName);
                }

            }

            PalantirMembers = new List<Member>();
            foreach (Model.Member memberEntity in Database.Members)
            {
                PalantirMembers.Add(JsonConvert.DeserializeObject<Member>(memberEntity.Member1));
            }
            Database.Dispose();
        }

        public void RemovePalantiri(ObservedGuild guild)
        {
            // remove tether
            PalantirTethers.Remove(PalantirTethers.Find(t => t.PalantirEndpoint.ObserveToken == guild.ObserveToken));

            // remove palantir from db
            PalantirContext context = new PalantirContext();
            Palantiri e = context.Palantiris.FirstOrDefault(ptr => ptr.Token == guild.ObserveToken);
            context.Palantiris.Remove(e);
            try
            {
                context.SaveChanges();
            }
            catch(Exception ex) { Console.WriteLine(ex.ToString()); }
            context.Dispose();
            // restart string op = "sudo service palantir restart".Bash();
            //Environment.Exit(0);
            //UpdateMemberGuilds();
        }

        public void UpdatePalantirSettings(Tether tether)
        {
            PalantirContext context = new PalantirContext();
            Model.GuildSetting entity = context.GuildSettings.FirstOrDefault(s => s.GuildId ==tether.PalantirEndpoint.GuildID);

            if (entity != null)
            {
                entity.Settings = JsonConvert.SerializeObject(tether.PalantirSettings);
                context.SaveChanges();
            }
            else
            {
                entity = new Model.GuildSetting();
                entity.GuildId = tether.PalantirEndpoint.GuildID;
                entity.Settings = JsonConvert.SerializeObject(tether.PalantirSettings);
                context.GuildSettings.Add(entity);
                context.SaveChanges();
            }
            context.SaveChanges();
            context.Dispose();
        }

        public void UpdateMemberGuilds()
        {
            PalantirContext context = new PalantirContext();
            foreach(Model.Member memberEntity in context.Members)
            {
                Member member = JsonConvert.DeserializeObject<Member>(memberEntity.Member1);
                List<ObservedGuild> updatedGuilds = new List<ObservedGuild>();
                member.Guilds.ForEach((g) =>
                {
                    if (PalantirTethers.Count(t => t.PalantirEndpoint.ObserveToken == g.ObserveToken && t.PalantirEndpoint.GuildID == g.GuildID) > 0)
                        updatedGuilds.Add(
                            PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.ObserveToken == g.ObserveToken && t.PalantirEndpoint.GuildID == g.GuildID)
                            .PalantirEndpoint);
                });
                member.Guilds = updatedGuilds;
                if (updatedGuilds.Count > 0) memberEntity.Member1 = JsonConvert.SerializeObject(member);
                else
                {
                    //MemberEntity rem = new MemberEntity();
                    //rem.Login = member.UserLogin;
                    //context.Members.Remove(rem);
                    //Console.WriteLine("Member " + member.UserName + " was removed.");
                    memberEntity.Member1 = JsonConvert.SerializeObject(member);
                    Console.WriteLine("Member " + member.UserName + " has no verified guilds.");
                }
            }
            context.SaveChanges();
            context.Dispose();
        }
        public void SavePalantiri(ObservedGuild guild)
        {
            bool newGuild = true;
            PalantirContext Database = new PalantirContext();

            // If guild of new palantir has already an active palantir, close tether, replace palantir and reopen tether
            PalantirTethers.ForEach((t) => {
                if (t.PalantirEndpoint.GuildID == guild.GuildID)
                {
                    string oldToken = t.PalantirEndpoint.ObserveToken;
                    // update tether
                    t.StopDataflow();
                    t.SetNewPalantirEndpoint(guild);
                    t.EstablishDataflow();
                    newGuild = false;

                   // Console.WriteLine("Change token from " + oldToken + " to " + guild.ObserveToken);
                    // update db entry
                    Database.Palantiris.Remove(Database.Palantiris.FirstOrDefault(p => p.Token == oldToken));
                    Palantiri entity = new Palantiri();
                    entity.Palantir = JsonConvert.SerializeObject(guild);
                    entity.Token = guild.ObserveToken;
                    Database.Palantiris.Add(entity);
                    Database.SaveChanges();
                }
            });

            if (newGuild)
            {
                // add tether
                Tether tether = new Tether(guild);
                tether.EstablishDataflow();
                PalantirTethers.Add(tether);

                // Add db entry
                Palantiri entity = new Palantiri();
                entity.Token = guild.ObserveToken;
                entity.Palantir = JsonConvert.SerializeObject(guild);
                Database.Palantiris.Add(entity);
                Database.SaveChanges();
            }
            Database.Dispose();
            UpdateMemberGuilds();
        }

        public void AddMember(Member member)
        {
            PalantirContext Database = new PalantirContext();
            PalantirMembers.Add(member);

            // add to db
            Model.Member entity = new Model.Member();
            entity.Login = Convert.ToInt32(member.UserLogin);
            entity.Member1 = JsonConvert.SerializeObject(member);
            entity.Bubbles = 0;
            entity.Sprites = "";
            Database.Members.Add(entity);
            Database.SaveChanges();
            Database.Dispose();
        }

        public List<Model.Member> GetGuildMembers(string guildID)
        {
            PalantirContext context = new PalantirContext();
            List<Model.Member> members = context.Members.Where(m => m.Member1.Contains(guildID)).ToList();
            context.Dispose();
            return members;
        }
        public Model.Member GetMemberByLogin(string login)
        {
            PalantirContext context = new PalantirContext();
            Model.Member member = context.Members.FirstOrDefault(m => m.Login.ToString() == login);
            context.Dispose();
            return member;
        }
        public Int16 GetFlagByMember(DiscordUser user)
        {
            PalantirContext context = new PalantirContext();
            Model.Member member = context.Members.FirstOrDefault(m => m.Member1.Contains(user.Id.ToString()));
            context.Dispose();
            return Convert.ToInt16(member.Flag);
        }
        public void SetFlagByID(string id, int flag)
        {
            PalantirContext context = new PalantirContext();
            Model.Member member = context.Members.FirstOrDefault(m => m.Member1.Contains(id));
            member.Flag = flag;
            context.SaveChanges();
            context.Dispose();
        }
        public async Task<int> UpdatePatrons(DSharpPlus.DiscordClient client)
        {
            List<string> patrons = new List<string>();
            List<string> boosters = new List<string>();
            List<string> patronizer = new List<string>();
            List<string> patronized = new List<string>();
            Dictionary<string, string> emojis = new Dictionary<string, string>();
            // collect ids of patron members 
            DiscordGuild typotestground = await client.GetGuildAsync(779435254225698827);
            foreach (DiscordMember member in await typotestground.GetAllMembersAsync())
            {
                if (member.Roles.Any(role => role.Id == 832744566905241610)) patrons.Add(member.Id.ToString());
                if (member.Roles.Any(role => role.Id == 859100010184572938)) patronizer.Add(member.Id.ToString());
                if (member.Roles.Any(role => role.Id == 983922288208531466)) boosters.Add(member.Id.ToString());
            };
            PatronCount = patrons.Count();
            PalantirContext db = new PalantirContext();
            // iterate through palantir members and set flags
            await db.Members.ForEachAsync(member =>
            {
                PermissionFlag flag = new PermissionFlag(Convert.ToInt16(member.Flag));
                flag.Patron = patrons.Any(patron => member.Member1.Contains(patron));
                flag.Booster = boosters.Any(booster => member.Member1.Contains(booster));
                if (patronizer.Any(id => member.Member1.Contains(id)))
                {
                    flag.Patronizer = true;
                    if (member.Patronize is not null && member.Patronize != "") patronized.Add(member.Patronize.Split("#")[0]);
                }
                else flag.Patronizer = false;
                string emoji = String.IsNullOrEmpty(member.Emoji) ? "" : member.Emoji;
                if(flag.Patron || flag.BotAdmin) emojis.Add(member.Login.ToString(), emoji);
                member.Flag = flag.CalculateFlag();
            });
            try
            {
                // set flags of patronized members
                patronized.ForEach(id =>
                {
                    if (db.Members.Any(member => member.Member1.Contains(id)))
                    {
                        Model.Member member = db.Members.FirstOrDefault(member => member.Member1.Contains(id));
                        if (!patrons.Contains(member.Login.ToString()))
                        {
                            PermissionFlag flag = new PermissionFlag(Convert.ToInt16(member.Flag));
                            flag.Patron = true;
                            string emoji = String.IsNullOrEmpty(member.Emoji) ? "" : member.Emoji;
                            if (!emojis.ContainsKey(member.Login.ToString())) emojis.Add(member.Login.ToString(), emoji);
                            member.Flag = flag.CalculateFlag();
                        }
                    }
                });

                PatronEmojis = emojis;

                db.SaveChanges();
                db.Dispose();
                return patrons.Count;
            }
            catch(Exception e)
            {
                Console.WriteLine("Error updating patrons:"+ e);
                return patrons.Count;
            }
        }

        public void ActivatePalantiri()
        {
            PalantirTethers.ForEach((t) => { t.EstablishDataflow(); Console.WriteLine("Started " + t.PalantirEndpoint.GuildName); });
        }

        public bool PalantirTokenExists(string token)
        {
            bool exists = false;
            PalantirTethers.ForEach((t) =>
            {
                if (t.PalantirEndpoint.ObserveToken == token) exists = true;
            });
            return exists;
        }

        public void ValidateGuildPalantir(string guildid)
        {
            if(!PalantirTethers.Any(p => p.PalantirEndpoint.GuildID == guildid)){
                throw new Exception("This server isn't using Palantir yet!\nVisit https://www.typo.rip/help/palantir to learn how to set up Palantir.");
            }
        }

        public double DatabaseReadTime(string id, int reads)
        {
            PalantirContext context = new PalantirContext();
            DateTime now;
            double time = 0;
            for(int i = 0; i < reads; i++)
            {
                now = DateTime.Now;
                context.Members.FirstOrDefault(m => m.Member1.Contains(id));
                time += (DateTime.Now - now).TotalMilliseconds;
            }
            context.Dispose();
            return time / reads;
        }
    }
}
