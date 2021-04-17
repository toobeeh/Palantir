using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace Palantir
{
    public class DataManager
    {
        public List<Tether> PalantirTethers;
        public List<Member> PalantirMembers;

        public DataManager()
        {
            LoadConnections();
            UpdateMemberGuilds();
        }

        public void LoadConnections()
        {
            PalantirDbContext Database = new PalantirDbContext();
            PalantirTethers = new List<Tether>();
            foreach (PalantirEntity palantirEntity in Database.Palantiri)
            {
                Tether tether;
                ObservedGuild guild = JsonConvert.DeserializeObject<ObservedGuild>(palantirEntity.Palantir);
                if(Database.GuildSettings.Any(s => s.GuildID == guild.GuildID)) 
                    tether = new Tether(guild, JsonConvert.DeserializeObject<GuildSettings>(Database.GuildSettings.FirstOrDefault(s=>s.GuildID == guild.GuildID).Settings));
                else tether = new Tether(guild);
                PalantirTethers.Add(tether);
            }

            PalantirMembers = new List<Member>();
            foreach (MemberEntity memberEntity in Database.Members)
            {
                PalantirMembers.Add(JsonConvert.DeserializeObject<Member>(memberEntity.Member));
            }
            Database.Dispose();
        }

        public void RemovePalantiri(ObservedGuild guild)
        {
            // remove tether
            PalantirTethers.Remove(PalantirTethers.Find(t => t.PalantirEndpoint.ObserveToken == guild.ObserveToken));

            // remove palantir from db
            PalantirDbContext context = new PalantirDbContext();
            PalantirEntity e = context.Palantiri.FirstOrDefault(ptr => ptr.Token == guild.ObserveToken);
            context.Palantiri.Remove(e);
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
            PalantirDbContext context = new PalantirDbContext();
            GuildSettingsEntity entity = context.GuildSettings.FirstOrDefault(s => s.GuildID ==tether.PalantirEndpoint.GuildID);

            if (entity != null)
            {
                entity.Settings = JsonConvert.SerializeObject(tether.PalantirSettings);
                context.SaveChanges();
            }
            else
            {
                entity = new GuildSettingsEntity();
                entity.GuildID = tether.PalantirEndpoint.GuildID;
                entity.Settings = JsonConvert.SerializeObject(tether.PalantirSettings);
                context.GuildSettings.Add(entity);
                context.SaveChanges();
            }
            context.SaveChanges();
            context.Dispose();
        }

        public void UpdateMemberGuilds()
        {
            PalantirDbContext context = new PalantirDbContext();
            foreach(MemberEntity memberEntity in context.Members)
            {
                Member member = JsonConvert.DeserializeObject<Member>(memberEntity.Member);
                List<ObservedGuild> updatedGuilds = new List<ObservedGuild>();
                member.Guilds.ForEach((g) =>
                {
                    if (PalantirTethers.Count(t => t.PalantirEndpoint.ObserveToken == g.ObserveToken && t.PalantirEndpoint.GuildID == g.GuildID) > 0)
                        updatedGuilds.Add(
                            PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.ObserveToken == g.ObserveToken && t.PalantirEndpoint.GuildID == g.GuildID)
                            .PalantirEndpoint);
                });
                member.Guilds = updatedGuilds;
                if (updatedGuilds.Count > 0) memberEntity.Member = JsonConvert.SerializeObject(member);
                else
                {
                    //MemberEntity rem = new MemberEntity();
                    //rem.Login = member.UserLogin;
                    //context.Members.Remove(rem);
                    //Console.WriteLine("Member " + member.UserName + " was removed.");
                    memberEntity.Member = JsonConvert.SerializeObject(member);
                    Console.WriteLine("Member " + member.UserName + " has no verified guilds.");
                }
            }
            context.SaveChanges();
            context.Dispose();
        }
        public void SavePalantiri(ObservedGuild guild)
        {
            bool newGuild = true;
            PalantirDbContext Database = new PalantirDbContext();

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
                    Database.Palantiri.Remove(Database.Palantiri.FirstOrDefault(p => p.Token == oldToken));
                    PalantirEntity entity = new PalantirEntity();
                    entity.Palantir = JsonConvert.SerializeObject(guild);
                    entity.Token = guild.ObserveToken;
                    Database.Palantiri.Add(entity);
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
                PalantirEntity entity = new PalantirEntity();
                entity.Token = guild.ObserveToken;
                entity.Palantir = JsonConvert.SerializeObject(guild);
                Database.Palantiri.Add(entity);
                Database.SaveChanges();
            }
            Database.Dispose();
            UpdateMemberGuilds();
        }

        public void AddMember(Member member)
        {
            PalantirDbContext Database = new PalantirDbContext();
            PalantirMembers.Add(member);

            // add to db
            MemberEntity entity = new MemberEntity();
            entity.Login = member.UserLogin;
            entity.Member = JsonConvert.SerializeObject(member);
            entity.Bubbles = 0;
            entity.Sprites = "";
            Database.Members.Add(entity);
            Database.SaveChanges();
            Database.Dispose();
        }

        public List<MemberEntity> GetGuildMembers(string guildID)
        {
            PalantirDbContext context = new PalantirDbContext();
            List<MemberEntity> members = context.Members.Where(m => m.Member.Contains(guildID)).ToList();
            context.Dispose();
            return members;
        }
        public MemberEntity GetMemberByLogin(string login)
        {
            PalantirDbContext context = new PalantirDbContext();
            MemberEntity member = context.Members.FirstOrDefault(m => m.Login == login);
            context.Dispose();
            return member;
        }
        public int GetFlagByMember(DiscordUser user)
        {
            PalantirDbContext context = new PalantirDbContext();
            MemberEntity member = context.Members.FirstOrDefault(m => m.Member.Contains(user.Id.ToString()));
            context.Dispose();
            return member.Flag;
        }
        public void SetFlagByID(string id, int flag)
        {
            PalantirDbContext context = new PalantirDbContext();
            MemberEntity member = context.Members.FirstOrDefault(m => m.Member.Contains(id));
            member.Flag = flag;
            context.SaveChanges();
            context.Dispose();
        }
        public async Task<int> UpdatePatrons()
        {
            List<string> patrons = new List<string>();
            // collect ids of patron members 832744566905241610 779435254225698827
            DiscordGuild typotestground = await Program.Client.GetGuildAsync(779435254225698827);
            foreach (DiscordMember member in await typotestground.GetAllMembersAsync())
            {
                if (member.Roles.Any(role => role.Id == 832744566905241610)) patrons.Add(member.Id.ToString());
            };
            PalantirDbContext db = new PalantirDbContext();
            // iterate through palantir members and set flags
            await db.Members.ForEachAsync(member =>
            {
                PermissionFlag flag = new PermissionFlag((byte)member.Flag);
                flag.Patron = patrons.Any(patron => member.Member.Contains(patron));
                member.Flag = flag.CalculateFlag();
            });
            db.SaveChanges();
            db.Dispose();
            return patrons.Count;
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
                throw new Exception("This server isn't using Palantir yet!\nVisit https://typo.rip to learn how to set up Palantir.");
            }
        }

        public double DatabaseReadTime(string id, int reads)
        {
            PalantirDbContext context = new PalantirDbContext();
            DateTime now;
            double time = 0;
            for(int i = 0; i < reads; i++)
            {
                now = DateTime.Now;
                context.Members.FirstOrDefault(m => m.Member.Contains(id));
                time += (DateTime.Now - now).TotalMilliseconds;
            }
            context.Dispose();
            return time / reads;
        }
    }
}
