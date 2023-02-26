using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class OnlineItem
{
    public string ItemType { get; set; } = null!;

    public int Slot { get; set; }

    public int ItemId { get; set; }

    public string LobbyKey { get; set; } = null!;

    public int LobbyPlayerId { get; set; }

    public long Date { get; set; }
}
