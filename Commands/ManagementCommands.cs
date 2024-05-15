using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Linq;
using MoreLinq.Extensions;
using Newtonsoft.Json;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System.Globalization;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using Palantir.Model;
using Palantir.PalantirCommandModule;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Palantir.Commands
{
    public class ManagementCommands : PalantirCommandModule.PalantirCommandModule
    {
        
        [Description("Set a member flag.")]
        [Command("flag")]
        public async Task Flag(CommandContext context, [Description("The id of the member to flag")] ulong id, [Description("The new flag")] int flag = -1)
        {
            if (flag == -1)
            {
                DiscordUser target = await Program.Client.GetUserAsync(id);
                PermissionFlag getperm = new PermissionFlag(Program.Feanor.GetFlagByMemberId(target.Id.ToString()));
                string getDesc = "Flag[0] Bubble Farming - "
                                 + getperm.BubbleFarming + "\nFlag[1] Bot Admin - "
                                 + getperm.BotAdmin + "\nFlag[2] Moderator - "
                                 + getperm.Moderator + "\nFlag[3] Unlimited Cloud - "
                                 + getperm.CloudUnlimited + "\nFlag[4] Patron - "
                                 + getperm.Patron + "\nFlag[5] Permanent Ban - "
                                 + getperm.Permanban + "\nFlag[6] Drop Ban - "
                                 + getperm.Dropban + "\nFlag[7] Patronizer - "
                                 + getperm.Patronizer + "\nFlag[8] Booster - "
                                 + getperm.Booster + "\nFlag[9] Beta - "
                                 + getperm.Beta;
                await Program.SendEmbed(context.Channel, "The flags of " + target.Username, getDesc);
                return;
            }
            PermissionFlag perm = new PermissionFlag(Program.Feanor.GetFlagByMemberId(context.User.Id.ToString()));
            if (!perm.BotAdmin && !perm.Moderator)
            {
                await Program.SendEmbed(context.Channel, "o_o", "You can't set other's flags!");
                return;
            }

            PermissionFlag newFlag = new PermissionFlag(Convert.ToInt16(flag));
            newFlag.BotAdmin = newFlag.BotAdmin && perm.BotAdmin;
            newFlag.Patronizer = newFlag.Patronizer && perm.BotAdmin;
            newFlag.Patron = newFlag.Patron && perm.BotAdmin;

            Program.Feanor.SetFlagByID(id.ToString(), newFlag.CalculateFlag());
            string name = (await Program.Client.GetUserAsync(id)).Mention;
            string desc = "Flag[0] Bubble Farming - "
                    + newFlag.BubbleFarming + "\nFlag[1] Bot Admin - "
                    + newFlag.BotAdmin + "\nFlag[2] Moderator - "
                    + newFlag.Moderator + "\nFlag[3] Unlimited Cloud - "
                    + newFlag.CloudUnlimited + "\nFlag[4] Patron - "
                    + newFlag.Patron + "\nFlag[5] Permanent Ban - "
                    + newFlag.Permanban + "\nFlag[6] Drop Ban - "
                    + newFlag.Dropban + "\nFlag[7] Patronizer - "
                    + newFlag.Patronizer + "\nFlag[8] Booster - "
                    + newFlag.Booster + "\nFlag[9] Beta - "
                    + newFlag.Beta;
            await Program.SendEmbed(context.Channel, "*magic happened*", "The flag of " + name + " was set to " + flag + "\n" + desc);
        }

        
        [Description("Start a reaction giveaway.")]
        [Command("giveaway")]
        [RequirePermissionFlag(PermissionFlag.MOD)]
        public async Task StartGiveaway(CommandContext context, ulong channelID, ulong messageID, DiscordEmoji reactionEmoji, int timeoutMilliSec, int winners, string giveawayname)
        {
            await Program.Servant.SendMessageAsync(context.Channel,
                "**Starting the " + giveawayname + "!**\n\nPeople will be eliminated once in " + (timeoutMilliSec / 1000 / 60) + " minutes, the last " + winners + " participants are the winners.");

            var msg = await(await Program.Client.GetChannelAsync(channelID)).GetMessageAsync(messageID);
            var reactions = await msg.GetReactionsAsync(reactionEmoji, 100);

            while(reactions.Count > winners)
            {
                reactions = reactions.Shuffle().ToList();
                var eliminate = reactions.First();
                reactions = reactions.Skip(1).ToList();

                var mentions = new System.Collections.Generic.List<IMention>();
                var eliminateState = new DiscordMessageBuilder()
                    .WithAllowedMentions(mentions)
                    .WithContent(eliminate.Mention + "** was eliminated :(** " + reactions.Count + " people left.");

                await Program.Servant.SendMessageAsync(context.Channel, eliminateState);

                if(reactions.Count == 10)
                {
                    var pool = new DiscordMessageBuilder()
                        .WithAllowedMentions(mentions)
                        .WithContent("\nThe remaining participant pool is: \n" + reactions.Select(r => r.Mention).ToDelimitedString(";"));
                    await Program.Servant.SendMessageAsync(context.Channel, pool);
                }

                await Task.Delay(timeoutMilliSec);
            }

            await Program.Servant.SendMessageAsync(context.Channel, "**The winners of the " + giveawayname + " are " + reactions.Select(rc => rc.Mention).ToDelimitedString(" and ") + "!**");
        }
    }
}
