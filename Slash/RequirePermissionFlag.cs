using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir.Slash
{
    internal class RequireSlashPermissionFlag : SlashCheckBaseAttribute
    {
        private byte FlagToCheck;
        public RequireSlashPermissionFlag(byte flagToCheck)
        {
            FlagToCheck = flagToCheck;
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            PermissionFlag userFlag = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(ctx.User));
            PermissionFlag requiredFlag = new PermissionFlag(FlagToCheck);
            bool result = userFlag.BotAdmin || userFlag.CheckForPermissionByte(FlagToCheck);
            return result;
        }
    }
}
