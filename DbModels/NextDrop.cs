using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class NextDrop
{
    public long DropId { get; set; }

    public string CaughtLobbyKey { get; set; } = null!;

    public string CaughtLobbyPlayerId { get; set; } = null!;

    public string ValidFrom { get; set; } = null!;

    public int EventDropId { get; set; }

    public int LeagueWeight { get; set; }
}
