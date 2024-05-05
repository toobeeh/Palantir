using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class Member
    {
        public int Login { get; set; }
        public string Member1 { get; set; }
        public int Bubbles { get; set; }
        public string Sprites { get; set; }
        public double Drops { get; set; }
        public int Flag { get; set; }
        public string Emoji { get; set; }
        public string Patronize { get; set; }
        public string Customcard { get; set; }
        public string Scenes { get; set; }
        public string RainbowSprites { get; set; }
        public long? AwardPackOpened { get; set; }
    }
}
