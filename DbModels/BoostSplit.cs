using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class BoostSplit
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Date { get; set; } = null!;

    public int Value { get; set; }
}
