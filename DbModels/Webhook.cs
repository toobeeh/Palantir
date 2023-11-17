using System;
using System.Collections.Generic;

#nullable disable

namespace Palantir.Model
{
    public partial class Webhook
    {
        public string ServerId { get; set; }
        public string Name { get; set; }
        public string WebhookUrl { get; set; }
    }
}
