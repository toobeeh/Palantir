using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.NewDbModels
{
    public partial class Award
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public sbyte Rarity { get; set; }
        public string Description { get; set; }
    }
}
