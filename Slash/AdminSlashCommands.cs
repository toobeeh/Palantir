using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir.Slash
{
    [SlashCommandGroup(">", "Commands for Palantir")]
    internal class AdminSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("hardreboot", "Update and reboot Palantir")]
        [RequireSlashPermissionFlag(PermissionFlag.MOD)]
        public async Task Reboot(InteractionContext context)
        {
            string upd = "git -C /home/pi/Palantir pull".Bash();
            upd += "\n\n Latest commit: " + ("git log --oneline -1".Bash());

            await Program.SendEmbed(context.Channel, "[literally dies...]", "You made me do this!!!\n\n**Update result:**\n" + upd);
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                 "[literally dies...]",
                 "You made me do this!!!\n\n**Update result:**\n" + upd
            )));

            "sudo rm /home/pi/palantirOutput.log".Bash();
            string op = "sudo service palantir restart".Bash();
            Environment.Exit(0);
        }


    }
}
