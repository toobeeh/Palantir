using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class DropBoost
{
    public int Login { get; set; }

    public string StartUtcs { get; set; } = null!;

    public int DurationS { get; set; }

    public string Factor { get; set; } = null!;

    public int CooldownBonusS { get; set; }
}
