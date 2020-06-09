using System;
using System.Collections.Generic;
using System.Text;

namespace Palantir
{
    public class Lobby
    {
        public string ID { get; set; }
        public string Key { get; set; }
        public int Round { get; set; }
        public bool Private { get; set; }
        public string Host { get; set; }
        public string Language { get; set; }
        public string Link { get; set; }
        public string GuildID { get; set; }
        public string ObserveToken { get; set; }
        public IList<Player> Players { get; set; }
        public IList<Player> Kicked { get; set; }
    }

    public class Player
    {
        public string Name { get; set; }
        public short Score { get; set; }
        public bool Drawing { get; set; }
        public bool Sender { get; set; }
        public string ID { get; set; }
        public string LobbyPlayerID { get; set; }
    }

    public class ObservedGuild
    {
        public string GuildID { get; set; }
        public string ChannelID { get; set; }
        public string MessageID { get; set; }
        public string ObserveToken { get; set; }
        public string GuildName { get; set; }
    }

    public class Member
    {
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string UserLogin { get; set; }
        public List<ObservedGuild> Guilds { get; set; }
    }

    public class PlayerStatus
    {
        public Member PlayerMember { get; set; }
        public string Status { get; set; }
        public string LobbyID { get; set; }
        public string LobbyPlayerID { get; set; }
    }
}
