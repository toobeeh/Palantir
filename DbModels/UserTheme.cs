using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class UserTheme
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
        public int Version { get; set; }
        public int Downloads { get; set; }
    }
}
