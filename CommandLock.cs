using DSharpPlus.CommandsNext;
using Palantir.PalantirCommandModule;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir
{
    internal class CommandLock
    {
        ConcurrentDictionary<string, ConcurrentDictionary<ulong, bool>> commandLocks = new();

        public void LockCommand(CommandContext ctx)
        {
            var isSynchronized = ctx.Command.ExecutionChecks.Any(check => check is SynchronizedAttribute);
            if (isSynchronized)
            {
                var commandName = ctx.Command.Name;
                var userId = ctx.User.Id;

                var lockedUsers = commandLocks.GetOrAdd(commandName, new ConcurrentDictionary<ulong, bool>());
                var userIsLocked = false;
                lockedUsers.AddOrUpdate(userId, true, (key, value) => {
                    userIsLocked = value;
                    return true;
                });

                if (userIsLocked)
                {
                    throw new TaskCanceledException("User <@" + userId + "> is already executing the command `" + commandName + "`.");
                }
            }
        }

        public void UnlockCommand(CommandContext ctx)
        {
            var isSynchronized = ctx.Command.ExecutionChecks.Any(check => check is SynchronizedAttribute);
            if (isSynchronized)
            {
                var commandName = ctx.Command.Name;
                var userId = ctx.User.Id;

                var lockedUsers = commandLocks[commandName];

                if (lockedUsers != null)
                {
                    lockedUsers.Remove(userId, out var removedUser);
                    //if(lockedUsers.IsEmpty)
                    //{
                    //    commandLocks.Remove(commandName, out var removedCommand);
                    //}
                }
            }
        }
    }
}
