using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class BubbleTrace
{
    public string Date { get; set; } = null!;

    public int Login { get; set; }

    public int Bubbles { get; set; }

    public int Id { get; set; }
}
