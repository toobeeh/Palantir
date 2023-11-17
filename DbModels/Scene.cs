using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class Scene
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Color { get; set; }
        public string Url { get; set; }
        public string GuessedColor { get; set; }
        public int EventId { get; set; }
        public bool Exclusive { get; set; }
    }
}
