using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class OnlineSprite
    {
        public string LobbyKey { get; set; }
        public int LobbyPlayerId { get; set; }
        public int Sprite { get; set; }
        public string Date { get; set; }
        public int Slot { get; set; }
        public string Id { get; set; }
    }
}
