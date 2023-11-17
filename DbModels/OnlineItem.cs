using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class OnlineItem
    {
        public string ItemType { get; set; }
        public int Slot { get; set; }
        public long ItemId { get; set; }
        public string LobbyKey { get; set; }
        public int LobbyPlayerId { get; set; }
        public int Date { get; set; }
    }
}
