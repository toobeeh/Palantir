using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class AccessToken
    {
        public int Login { get; set; }
        public string AccessToken1 { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
