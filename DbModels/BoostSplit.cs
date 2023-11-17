using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class BoostSplit
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public int Value { get; set; }
    }
}
