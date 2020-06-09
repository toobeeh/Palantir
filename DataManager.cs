using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace Palantir
{
    public class DataManager
    {
        public List<Tether> PalantirTethers;
        public List<Member> PalantirMembers;
        public PalantirDbContext Database;

        public DataManager()
        {
            Database = new PalantirDbContext();
            LoadConnections();
        }

        public void LoadConnections()
        {
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
        }

        public void RemovePalantiri(ObservedGuild guild)
        {
            // remove tether
            PalantirTethers.Remove(PalantirTethers.Find(t => t.PalantirEndpoint.ObserveToken == guild.ObserveToken));

            // remove palantir from db
            Database.Palantiri.Remove(Database.Palantiri.Find(guild.ObserveToken));
            Database.SaveChanges();
        }

        public void SavePalantiri(ObservedGuild guild)
        {
            bool newGuild = true;

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
                    PalantirEntity entity = new PalantirEntity();
                    entity.Token = guild.ObserveToken;
                    entity.Palantir = JsonConvert.SerializeObject(guild);
                    Database.Palantiri.Attach(entity);
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
        }

        public void AddMember(Member member)
        {
            PalantirMembers.Add(member);

            // add to db
            MemberEntity entity = new MemberEntity();
            entity.Login = member.UserLogin;
            entity.Member = JsonConvert.SerializeObject(member);
            Database.Members.Add(entity);
            Database.SaveChanges();
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
