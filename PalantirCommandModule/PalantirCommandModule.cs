using DSharpPlus.CommandsNext;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir.PalantirCommandModule
{
    public class PalantirCommandModule : BaseCommandModule
    {

        

        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            Program.CommandLock.LockCommand(ctx);

            return base.BeforeExecutionAsync(ctx);
        }

        public override Task AfterExecutionAsync(CommandContext ctx)
        {
            Program.CommandLock.UnlockCommand(ctx);

            return base.AfterExecutionAsync(ctx);
        }

    }
}
