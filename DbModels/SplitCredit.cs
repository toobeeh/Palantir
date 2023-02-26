using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class SplitCredit
{
    public int Login { get; set; }

    public int Split { get; set; }

    public string RewardDate { get; set; } = null!;

    public string Comment { get; set; } = null!;

    public int ValueOverride { get; set; }
}
