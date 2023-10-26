using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir.PalantirCommandModule
{
    internal class SynchronizedAttribute : DSharpPlus.CommandsNext.Attributes.CheckBaseAttribute
    {
        public SynchronizedAttribute()
        {
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
           return Task.FromResult(true);
        }
    }
}
