using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.SlashCommands;
using MoreLinq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantir.Slash
{
    internal class SpriteSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("buy", "Buy a sprite")]
        public async Task BuySprite(InteractionContext context, [Option("ID", "The sprite ID - find all sprites on https://typo.rip")] long sprite)
        {
            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
            MemberEntity member = Program.Feanor.GetMemberByLogin(login);
            List<SpriteProperty> inventory;
            try
            {
                inventory = BubbleWallet.GetInventory(login);
            }
            catch (Exception e)
            {
                await Program.SendEmbed(context.Channel, "Error executing command", e.ToString());
                return;
            }
            List<Sprite> available = BubbleWallet.GetAvailableSprites();

            if (inventory.Any(s => s.ID == sprite))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                     "Woah!!",
                     "Bubbles are precious. \nDon't pay for something you already own!"
                )));
                return;
            }

            if (!available.Any(s => s.ID == sprite))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                     "Eh...?",
                     "Can't find that sprite. \nChoose another one or keep your bubbles."
                )));
                return;
            }

            Sprite target = available.FirstOrDefault(s => s.ID == sprite);
            int credit = BubbleWallet.CalculateCredit(login, context.User.Id.ToString());
            PermissionFlag perm = new PermissionFlag((byte)member.Flag);
            if (target.ID == 1003)
            {
                if (!perm.Patron)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                         "Haha, nice try -.-",
                          "This sprite is exclusive for patrons!"
                    )));
                    return;
                }
            }
            else if (target.EventDropID == 0)
            {
                if (credit < target.Cost && !perm.BotAdmin)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                         "Haha, nice try -.-",
                          "That stuff is too expensive for you. \nSpend few more hours on skribbl."
                    )));
                    return;
                }
            }
            else
            {
                if (BubbleWallet.GetRemainingEventDrops(login, target.EventDropID) < target.Cost && !perm.BotAdmin)
                {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                         "Haha, nice try -.-",
                          "That stuff is too expensive for you. \nCatch a few more event drops."
                    )));
                    return;
                }
            }

            inventory.Add(new SpriteProperty(target.Name, target.URL, target.Cost, target.ID, target.Special, target.Rainbow, target.EventDropID, target.Artist, false, -1));
            BubbleWallet.SetInventory(inventory, login);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Whee!";
            embed.Description = "You unlocked **" + target.Name + "**!\nActivate it with `>use " + target.ID + "`";
            embed.Color = DiscordColor.Magenta;
            embed.ImageUrl = target.URL;
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("use", "Use a sprite on your avatar on skribbl")]
        public async Task UseSprite(InteractionContext context, [Option("ID", "The sprite ID - find all sprites on https://typo.rip")] long sprite, [Option("Slot", "The sprite slot")] long slot = 1)
        {
            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
            List<SpriteProperty> inventory;
            inventory = BubbleWallet.GetInventory(login);
            if (sprite != 0 && !inventory.Any(s => s.ID == sprite))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                         "Hold on!",
                         "You don't own that. \nGet it first with `/buy " + sprite + "`."
                )));
                return;
            }

            MemberEntity member = Program.Feanor.GetMemberByLogin(login);
            PermissionFlag perm = new PermissionFlag((byte)member.Flag);

            if (!perm.BotAdmin && (slot < 1 || slot > BubbleWallet.GetDrops(login, context.User.Id.ToString()) / 1000 + 1 + (perm.Patron ? 1 : 0)))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                         "Out of your league.",
                         "You can't use that sprite slot!\nFor each thousand collected drops, you get one extra slot."
                )));
                return;
            }

            if (sprite == 0)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                         "Minimalist, huh? Your sprite was disabled.",
                         " "
                )));
                inventory.ForEach(i =>
                {
                    if (i.Slot == slot) i.Activated = false;
                });
                BubbleWallet.SetInventory(inventory, login);
                return;
            }

            if (BubbleWallet.GetSpriteByID((int)sprite).Special && inventory.Any(active => active.Activated && active.Special && active.Slot != slot))
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Program.PalantirEmbed(
                         "Too overpowered!!",
                         "Only one of your sprite slots may have a special sprite."
                )));
                return;
            }

            inventory.ForEach(i =>
            {
                if (i.ID == sprite && i.Activated) i.Slot = (int)slot; // if sprite is already activated, activate on other slot
                else if (i.ID == sprite && !i.Activated) { i.Activated = true; i.Slot = (int)slot; } // if sprite is not activated, activate on slot
                else if (!(i.Activated && i.ID != sprite && i.Slot != slot)) { i.Activated = false; i.Slot = -1; }
                // if sprite ist not desired not activated on slot deactivate
            });
            BubbleWallet.SetInventory(inventory, login);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Your fancy sprite on slot " + slot + " was set to **`" + BubbleWallet.GetSpriteByID((int)sprite).Name + "`**";
            embed.ImageUrl = BubbleWallet.GetSpriteByID((int)sprite).URL;
            embed.Color = DiscordColor.Magenta;
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
    }
}
