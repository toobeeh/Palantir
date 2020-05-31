using System;
using System.Collections.Generic;
using System.Text;

namespace Palantir
{
    class Lobby
    {
        public int ID { get; set; }
        public int Round { get; set; }
        public bool Private { get; set; }
        public string Host { get; set; }
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
    }

    public class ObservedGuild
    {
        public ulong GuildID { get; set; }
        public ulong ChannelID { get; set; }
        public ulong MessageID { get; set; }
        public string ObserveToken { get; set; }
        public string ServerName { get; set; }
    }
}
