using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class EventDrop
    {
        public int EventDropId { get; set; }
        public int EventId { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
    }
}
