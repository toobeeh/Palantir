using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;

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
                Tether tether = new Tether(JsonConvert.DeserializeObject<ObservedGuild>(palantirEntity.Palantir));
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
            PalantirEntity e = new PalantirEntity();
            e.Token = guild.ObserveToken;
            context.Palantiri.Remove(e);
            context.SaveChanges();
            context.Dispose();
            UpdateMemberGuilds();
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
                        updatedGuilds.Add(g);
                });
                member.Guilds = updatedGuilds;
                if (updatedGuilds.Count > 0) memberEntity.Member = JsonConvert.SerializeObject(member);
                else context.Members.Remove(memberEntity);
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
                    // update tether
                    t.StopDataflow();
                    t.SetNewPalantirEndpoint(guild);
                    t.EstablishDataflow();
                    newGuild = false;

                    // update db entry
                    PalantirEntity entity = Database.Palantiri.FirstOrDefault(p => p.Token == t.PalantirEndpoint.ObserveToken);
                    entity.Palantir = JsonConvert.SerializeObject(guild);
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
            Database.Members.Add(entity);
            Database.SaveChanges();
            Database.Dispose();
        }

        public void ActivatePalantiri()
        {
            PalantirTethers.ForEach((t) => { t.EstablishDataflow(); });
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
    }
}
