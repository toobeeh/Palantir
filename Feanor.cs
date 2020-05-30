using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace Palantir
{
    public static class Feanor
    {
        //private const string jsonPath = @"C:\Users\Tobi\source\repos\toobeeh\Palantir\palantiri.json";
        private const string jsonPath = @"palantiri.json";
        public static List<Tether> PalantiriTethers;

        public static void LoadPalantiri()
        {
            List <ObservedGuild> palantiri = JsonConvert.DeserializeObject<List<ObservedGuild>>(File.ReadAllText(jsonPath));
            if(palantiri == null) palantiri = new List<ObservedGuild>();

            PalantiriTethers = new List<Tether>();
            palantiri.ForEach((p) =>
            {
                Tether tether = new Tether(p);
                PalantiriTethers.Add(tether);
            });
        }

        public static void SavePalantiri(ObservedGuild guild)
        {
            bool newGuild = true;

            // If guild of new palantir has already an active palantir, close tether, replace palantir and reopen tether
            PalantiriTethers.ForEach((t) => {
                if (t.PalantirEndpoint.GuildID == guild.GuildID)
                {
                    t.StopDataflow();
                    t.SetNewPalantirEndpoint(guild);
                    t.EstablishDataflow();
                    newGuild = false;
                }
            });
            if(newGuild)
            {
                Tether tether = new Tether(guild);
                tether.EstablishDataflow();
                PalantiriTethers.Add(tether);
            }

            // save current guild list to json
            List<ObservedGuild> palantiri = new List<ObservedGuild>();
            PalantiriTethers.ForEach((t) => { palantiri.Add(t.PalantirEndpoint); });
            string json = JsonConvert.SerializeObject(palantiri);
            File.WriteAllText(jsonPath, json);
        }

        public static void ActivatePalantiri()
        {
            PalantiriTethers.ForEach((t) => { t.EstablishDataflow(); });
        }

    }
}
