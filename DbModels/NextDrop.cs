using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class NextDrop
    {
        public long DropId { get; set; }
        public string CaughtLobbyKey { get; set; }
        public string CaughtLobbyPlayerId { get; set; }
        public string ValidFrom { get; set; }
        public int EventDropId { get; set; }
        public int LeagueWeight { get; set; }
    }
}
