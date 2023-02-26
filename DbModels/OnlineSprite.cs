using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class OnlineSprite
{
    public string LobbyKey { get; set; } = null!;

    public int LobbyPlayerId { get; set; }

    public int Sprite { get; set; }

    public string Date { get; set; } = null!;

    public int Slot { get; set; }

    public string Id { get; set; } = null!;
}
