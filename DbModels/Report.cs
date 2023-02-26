using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class Report
{
    public string LobbyId { get; set; } = null!;

    public int ObserveToken { get; set; }

    public string Report1 { get; set; } = null!;

    public string Date { get; set; } = null!;
}
