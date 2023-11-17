using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.NewDbModels
{
    public partial class Awardee
    {
        public int Id { get; set; }
        public short Award { get; set; }
        public int OwnerLogin { get; set; }
        public int? AwardeeLogin { get; set; }
        public long? Date { get; set; }
        public long? ImageId { get; set; }
    }
}
