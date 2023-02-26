using System;
using System.Collections.Generic;

namespace Palantir.Model;

public partial class Status
{
    public string SessionId { get; set; } = null!;

    public string Status1 { get; set; } = null!;

    public string Date { get; set; } = null!;
}
