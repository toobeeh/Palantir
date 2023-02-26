using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class Scene
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Artist { get; set; } = null!;

    public string Color { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string? GuessedColor { get; set; }

    public int EventId { get; set; }

    public bool Exclusive { get; set; }
}
