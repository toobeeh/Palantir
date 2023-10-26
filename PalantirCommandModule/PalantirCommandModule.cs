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

        ConcurrentDictionary<String, ConcurrentDictionary<ulong, bool>> commandLocks = new();

        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            base.BeforeExecutionAsync(ctx);
            var isSynchronized = ctx.Command.ExecutionChecks.Any(check => check is SynchronizedAttribute);
            if(isSynchronized)
            {
                var commandName = ctx.Command.Name;
                var userId = ctx.User.Id;

                var lockedUsers = commandLocks.GetOrAdd(commandName, new ConcurrentDictionary<ulong, bool>());
                var userIsLocked = lockedUsers.GetOrAdd(userId, false);

                if (userIsLocked)
                {
                    throw new Exception("User <@" + userId + "> is already executing the command `" + commandName + "`.");
                    ctx.RespondAsync("locked");
                }
            }

            return Task.Delay(0);
        }

        public override Task AfterExecutionAsync(CommandContext ctx)
        {
            base.AfterExecutionAsync(ctx);
            var isSynchronized = ctx.Command.ExecutionChecks.Any(check => check is SynchronizedAttribute);
            if (isSynchronized)
            {
                var commandName = ctx.Command.Name;
                var userId = ctx.User.Id;

                var lockedUsers = commandLocks[commandName];

                if(lockedUsers != null)
                {
                    bool removedUser;
                    lockedUsers.Remove(userId, out removedUser);
                    if(lockedUsers.IsEmpty)
                    {
                        ConcurrentDictionary<ulong, bool> removedCommand;
                        commandLocks.Remove(commandName, out removedCommand);
                    }
                }
            }

            return Task.Delay(0);
        }

    }
}
