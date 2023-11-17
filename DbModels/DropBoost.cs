using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class DropBoost
    {
        public int Login { get; set; }
        public string StartUtcs { get; set; }
        public int DurationS { get; set; }
        public string Factor { get; set; }
        public int CooldownBonusS { get; set; }
    }
}
