using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class Event
{
    public int EventId { get; set; }

    public string EventName { get; set; } = null!;

    public int DayLength { get; set; }

    public string Description { get; set; } = null!;

    public string ValidFrom { get; set; } = null!;
}
