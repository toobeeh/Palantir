using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class Member
{
    public int Login { get; set; }

    public string Member1 { get; set; } = null!;

    public int Bubbles { get; set; }

    public string Sprites { get; set; } = null!;

    public int Drops { get; set; }

    public int Flag { get; set; }

    public string? Emoji { get; set; }

    public string? Patronize { get; set; }

    public string? Customcard { get; set; }

    public string? Scenes { get; set; }

    public string Streamcode { get; set; } = null!;

    public string? RainbowSprites { get; set; }
    public long? AwardPackOpened { get; set; }
}
