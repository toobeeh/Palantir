using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class Event
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public int DayLength { get; set; }
        public string Description { get; set; }
        public string ValidFrom { get; set; }
        public sbyte Progressive { get; set; }
    }
}
