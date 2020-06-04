using System;
using System.Collections.Generic;
using System.Text;

namespace Palantir
{
    class Lobby
    {
        public string ID { get; set; }
        public int Round { get; set; }
        public bool Private { get; set; }
        public string Host { get; set; }
        public string Language { get; set; }
        public string Link { get; set; }
        public ulong ServerID { get; set; }
        public string ObserveToken { get; set; }
        public IList<Player> Players { get; set; }
        public IList<Player> Kicked { get; set; }
    }

    class Player
    {
        public string Name { get; set; }
        public short Score { get; set; }
        public bool Drawing { get; set; }
        public bool Sender { get; set; }
        public string ID { get; set; }
        public string Status { get; set; }
        public string GuildID { get; set; }
    }

    public class ObservedGuild
    {
        public string GuildID { get; set; }
        public string ChannelID { get; set; }
        public string MessageID { get; set; }
        public string ObserveToken { get; set; }
        public string GuildName { get; set; }
    }
}
