using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace Palantir
{
    public static class Feanor
    {
        private const string jsonPath = @"C:\Users\Tobi\source\repos\toobeeh\Palantir\palantiri.json";
        public static List<ObservedGuild> Palantiri{ get; private set; }

        public static void LoadPalantiri()
        {
            Palantiri = JsonConvert.DeserializeObject<List<ObservedGuild>>(File.ReadAllText(jsonPath));
            if(Palantiri == null) Palantiri = new List<ObservedGuild>();
        }

        public static void SavePalantiri(ObservedGuild guild)
        {
            List<ObservedGuild> newPalantiri = new List<ObservedGuild>();

            // If guild of new palantir has already an active palantir, kick that out
            Palantiri.ForEach((stone) => { if (stone.GuildID != guild.GuildID) newPalantiri.Add(stone); });
            Palantiri = newPalantiri;
            Palantiri.Add(guild);

            string json = JsonConvert.SerializeObject(Palantiri);
            File.WriteAllText(jsonPath, json);
        }

    }
}
