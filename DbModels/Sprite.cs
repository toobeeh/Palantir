using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class Sprite
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Url { get; set; } = null!;

    public int Cost { get; set; }

    public bool Special { get; set; }

    public int EventDropId { get; set; }

    public string? Artist { get; set; }

    public int Rainbow { get; set; }
}
