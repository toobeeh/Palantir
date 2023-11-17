using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class CloudTag
    {
        public int Owner { get; set; }
        public long ImageId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public bool Own { get; set; }
        public long Date { get; set; }
        public string Language { get; set; }
        public bool Private { get; set; }
    }
}
