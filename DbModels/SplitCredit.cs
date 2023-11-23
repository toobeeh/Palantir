using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class SplitCredit
    {
        public int Id { get; set; }
        public int Login { get; set; }
        public int Split { get; set; }
        public string RewardDate { get; set; }
        public string Comment { get; set; }
        public int ValueOverride { get; set; }
    }
}
