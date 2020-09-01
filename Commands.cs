using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Linq;
using Newtonsoft.Json;

namespace Palantir
{
    public class Commands : BaseCommandModule
    {

        [Command("manual")]
        [Description("Show the manual for bot usage")]
        public async Task Manual(CommandContext context)
        {
            string msg = "";
            //msg += "**Bot configuration**\n";
            //msg += " This bot shows current skribbl lobbies in a message which is edited in an interval of a few seconds.\n";
            //msg += " The channel should be read-only to normal members, so the message always stays on top.\n\n";
            //msg += "Set channel: `>observe #channel`\n";
            //msg += "Change channel: `>observe #channel`\n";
            //msg += "Change channel, keep token: `>observe #channel keep`\n";
            //msg += "The channel has to be mentioned with a #.\n";
            //msg += "If the token isn't kept with *keep*, users need to verify the server again.\n";
            //msg += "For other configurations, see `>help`.\n\n\n";
            msg += "**Visit the website:**\n";
            msg += "https://www.tobeh.host/Orthanc/\n";
            msg += "**Connect to the bot**\n";
            msg += " - Message the bot in DM `>login`\n";
            msg += " - Copy the login number\n";
            msg += " - Enter the login number in the browser extension popup\n";
            msg += " - Copy the server token (from the bot message or ask your admin)\n";
            msg += " - Enter the server token in the browser extension popup\n\n";
            msg += " Now all your added servers will display when you're online. \n";
            msg += "https://media.giphy.com/media/UuviRqel88ryGaL8cg/giphy.gif";

            await context.RespondAsync(msg);
        }

        [Command("header")]
        [Description("Set the header text of the bot message.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Header(CommandContext context, [Description("Header text of the message")] params string[] header)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            string text = "";
            foreach (string s in header) text += s + " ";
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.Header = text;
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }

        [Command("idle")]
        [Description("Set the idle text of the bot message.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Idle(CommandContext context, [Description("Idle text of the message")] params string[] idle)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            string text = "";
            foreach (string s in idle) text += s + " ";
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.IdleMessage = text;
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }

        [Command("timezone")]
        [Description("Set the timezone UTC offset of the bot message.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Timezone(CommandContext context, [Description("Timezone offset (eg -5)")] int offset)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.Timezone = offset;
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }

        [Command("token")]
        [Description("Set whether the token should be displayed or not.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Token(CommandContext context, [Description("State (on/off)")] string state)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            if (state != "on" && state != "off")
            {
                await context.Message.RespondAsync("Invalid state.");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.ShowToken = state == "on";
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }

        [Command("refreshed")]
        [Description("Set whether the refreshed time should be displayed or not.")]
        [RequireGuild()]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        public async Task Refreshed(CommandContext context, [Description("State (on/off)")] string state)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }
            if (state != "on" && state != "off")
            {
                await context.Message.RespondAsync("Invalid state.");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.ShowRefreshed = state == "on";
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }

        [Command("animated")]
        [Description("Set whether the animated emojis should be displayed or not.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Animated(CommandContext context, [Description("State (on/off)")] string state)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel befor configuring the settings!");
                return;
            }
            if (state != "on" && state != "off")
            {
                await context.Message.RespondAsync("Invalid state.");
                return;
            }
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.ShowAnimatedEmojis = state == "on";
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
        }


        [Command("observe")]
        [Description("Set a channel where lobbies will be observed.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Observe(CommandContext context, [Description("Target channel (eg #channel)")] string channel)
        {
            if (context.Message.MentionedChannels.Count <1) { await context.Message.RespondAsync("Invalid channel!"); return; }

            // Create message in specified channel which later will be the static message to be continuously edited
            DiscordMessage msg = await context.Message.MentionedChannels[0].SendMessageAsync("Initializing...");
            ObservedGuild guild = new ObservedGuild();
            guild.GuildID = context.Guild.Id.ToString();
            guild.ChannelID = context.Message.MentionedChannels[0].Id.ToString();
            guild.MessageID = msg.Id.ToString();
            guild.GuildName = context.Guild.Name;

            string token;
            do
            {
                token = (new Random()).Next(100000000 - 1).ToString("D8");
                guild.ObserveToken = token;
            }
            while (Program.Feanor.PalantirTokenExists(token));
            await context.Message.RespondAsync("Active lobbies will now be observed in " + context.Message.MentionedChannels[0].Mention + ".\nUsers need following token to connect the browser extension: ```fix\n" + token + "\n```Pin this message or save the token!\n\nFor further instructions, users can visit the website https://www.tobeh.host/Orthanc/.\nMaybe include the link in the bot message!");
            // save observed
            Program.Feanor.SavePalantiri(guild);
            
        }

        [Command("switch")]
        [Description("Set a channel where lobbies will be observed.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Switch(CommandContext context, [Description("Target channel (#channel)")]string channel)
        {
            if (context.Message.MentionedChannels.Count < 1) { await context.Message.RespondAsync("Invalid channel!"); return; }

            // Create message in specified channel which later will be the static message to be continuously edited
            DiscordMessage msg = await context.Message.MentionedChannels[0].SendMessageAsync("Initializing...");
            ObservedGuild guild = new ObservedGuild();
            guild.GuildID = context.Guild.Id.ToString();
            guild.ChannelID = context.Message.MentionedChannels[0].Id.ToString();
            guild.MessageID = msg.Id.ToString();
            guild.GuildName = context.Guild.Name;

            string token = "";
            do
            {
                token = (new Random()).Next(100000000 - 1).ToString("D8");
                guild.ObserveToken = token;
            }
            while (Program.Feanor.PalantirTokenExists(token));

            bool valid = true;
            if ("keep" == "keep") // whelp change that 
            {
                string oldToken = "";
                Program.Feanor.PalantirTethers.ForEach((t) => {if (t.PalantirEndpoint.GuildID == guild.GuildID) oldToken = t.PalantirEndpoint.ObserveToken;});
                if (oldToken == "") valid = false;
                else { 
                    token = oldToken;
                    guild.ObserveToken = token;
                }

                if (valid)
                {
                    await context.Message.RespondAsync("The channel is now set to  " + context.Message.MentionedChannels[0].Mention + ".\nUsers won't need to re-enter their token." );
                    // save observed
                    Program.Feanor.SavePalantiri(guild);
                }
                else await context.Message.RespondAsync("There is no existing token.\nCheck >help for help.");
            }
            else await context.Message.RespondAsync("That's no valid command.\nCheck >help for help.");
        }

        [Description("Get your login data to connect the extension.")]
        [RequireDirectMessage()]
        [Command("login")]
        public async Task Login(CommandContext context)
        {
            DiscordDmChannel channel = (DiscordDmChannel)context.Channel;
            Member match = new Member { UserID = "0" };

            Program.Feanor.PalantirMembers.ForEach((m) =>
            {
                if (Convert.ToUInt64(m.UserID) == context.Message.Author.Id) match = m;
            });

            if (match.UserID != "0") await channel.SendMessageAsync("Forgot your login? \nHere it is: `" + match.UserLogin + "`");
            else
            {
                Member member = new Member();
                member.UserID = context.Message.Author.Id.ToString();
                member.UserName = context.Message.Author.Username;
                member.Guilds = new List<ObservedGuild>();
                do member.UserLogin = (new Random()).Next(99999999).ToString();
                while (Program.Feanor.PalantirMembers.Where(mem => mem.UserLogin == member.UserLogin).ToList().Count > 0);

                Program.Feanor.AddMember(member);

                await channel.SendMessageAsync("Hey " + context.Message.Author.Username + "!\nYou can now login to the bowser extension and use Palantir.\nClick the extension icon in your browser, enter your login and add you discord server's token! \nYour login is: `" + member.UserLogin + "`");
            }
        }

        [Description("Get a list of all sprites in the store.")]
        [Command("sprites")]
        [Aliases("spt")]
        public async Task Sprites(CommandContext context, int sprite = 0)
        {
            List<Sprite> sprites = BubbleWallet.GetAvailableSprites();

            if (sprites.Any(s => s.ID == sprite))
            {
                Sprite s = BubbleWallet.GetSpriteByID(sprite);
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Color = DiscordColor.Magenta;
                embed.Title = s.Name;
                embed.ImageUrl = s.URL;
                embed.Description = "**Costs:** " + s.Cost + " Bubbles\n\n**ID**: " + s.ID + (s.Special ? " :sparkles: " : "");
                embed.WithFooter("[View all Sprites here](https://tobeh.host/Orthanc/sprites/gif/)");
                await context.Channel.SendMessageAsync(embed: embed);
                return;
            }

            DiscordEmbedBuilder list = new DiscordEmbedBuilder();
            list.Color = DiscordColor.Magenta;
            list.Title = "🔮 Sprite Listing";
            list.Description = "Show one of the available Sprites with `>sprites [id]`";
            list.WithFooter("[View all Sprites here](https://tobeh.host/Orthanc/sprites/gif/)");

            foreach (Sprite s in sprites)
            {
                list.AddField("**" + s.Name + "** ", "Costs: " + s.Cost + " Bubbles\nID: " + s.ID + (s.Special ? " :sparkles: " : ""),true);
            };
            await context.Channel.SendMessageAsync(embed: list);
        }

        [Description("Get a overview of your inventory.")]
        [Command("inventory")]
        [Aliases("inv")]

        public async Task Inventory(CommandContext context)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            List<SpriteProperty> inventory;
            try
            {
                inventory = BubbleWallet.GetInventory(login);
            }
            catch(Exception e)
            {
                await Program.SendEmbed(context.Channel, "Error executing command", e.ToString());
                return;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Color = DiscordColor.Magenta;
            embed.Title = "🔮  " + context.Message.Author.Username + "s Inventory";

            SpriteProperty active = null;
            
            inventory.OrderBy(s => s.ID).ToList().ForEach(s =>
            {
                embed.AddField("**" + s.Name + "** " ,  "#" + s.ID + " |  Worth " + s.Cost + " Bubbles" + (s.Special ? " :sparkles: " : ""));
                if (s.Activated) active = s;
            });

            string desc = "";
            if (active is object)
            {
                desc += "\n**Selected sprite:** " + active.Name;
                embed.ImageUrl = active.URL;
            }
            if (inventory.Count <= 0) desc = "You haven't unlocked any sprites yet!";
            desc += "\n\n🔮 **" + BubbleWallet.CalculateCredit(login) + "** / "+ BubbleWallet.GetBubbles(login) + " total Bubbles available.";
            desc += "\n\n💧 **" + BubbleWallet.GetDrops(login) + "** Drops collected.";

            embed.AddField("\u200b ", desc);
            embed.AddField("\u200b ", "Use `>use [id]` to select your Sprite!\n`>use 0` will set no Sprite.\nBuy a Sprite with `>buy [id]`.\nSpecial Sprites :sparkles: replace your whole avatar! ");
            embed.WithFooter("[View all Sprites here](https://tobeh.host/Orthanc/sprites/gif/)");
            await context.Channel.SendMessageAsync(embed:embed);
           
        }

        [Description("Choose your sprite.")]
        [Command("sprite")]
        public async Task Sprite(CommandContext context, int sprite)
        {
            await Program.SendEmbed(context.Channel, "You did nothing wrong,", "but to avoid confusion with `>sprites` the command was renamed to `>use`.\nType `>use " + sprite + "` to activate your Sprite!");
        }

        [Description("Choose your sprite.")]
        [Command("use")]
        public async Task Use(CommandContext context, int sprite)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
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

            if (sprite !=0 && !inventory.Any(s=>s.ID == sprite))
            {
                await Program.SendEmbed(context.Channel, "Hold on!", "You don't own that. \nGet it first with `>buy " + sprite + "`.");
                return;
            }

            if (sprite == 0)
            {
                await Program.SendEmbed(context.Channel, "Minimalist, huh? You sprite was disabled.", "");
                inventory.ForEach(i => i.Activated =false);
                BubbleWallet.SetInventory(inventory, login);
                return;
            }

            inventory.ForEach(i => i.Activated = i.ID == sprite);
            BubbleWallet.SetInventory(inventory, login);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Your fancy sprite was set to **" + BubbleWallet.GetSpriteByID(sprite).Name + "**";
            embed.ImageUrl = BubbleWallet.GetSpriteByID(sprite).URL;
            embed.Color = DiscordColor.Magenta;
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Buy a sprite.")]
        [Command("buy")]
        public async Task Buy(CommandContext context, int sprite)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
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
                await Program.SendEmbed(context.Channel, "Woah!!", "Bubbles are precious. \nDon't pay for something you already own!");
                return;
            }

            if (!available.Any(s => s.ID == sprite))
            {
                await Program.SendEmbed(context.Channel, "Eh...?", "Can't find that sprite. \nChoose another one or keep your bubbles.");
                return;
            }

            Sprite target = available.FirstOrDefault(s => s.ID == sprite);
            int credit = BubbleWallet.CalculateCredit(login);
            if (credit < target.Cost)
            {
                await Program.SendEmbed(context.Channel, "Haha, nice try -.-", "That stuff is too expensive for you. \nSpend few more hours on skribbl.");
                return;
            }

            inventory.Add(new SpriteProperty(target.Name, target.URL, target.Cost, target.ID, target.Special, false));
            BubbleWallet.SetInventory(inventory, login);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Whee!";
            embed.Description = "You unlocked **" + target.Name + "**!\nActivate it with `>sprite " + target.ID + "`" ;
            embed.Color = DiscordColor.Magenta;
            embed.ImageUrl = target.URL;
            await context.Channel.SendMessageAsync(embed: embed);

            return;
        }

        [Description("See who's got the most bubbles.")]
        [Command("Leaderboard")]
        [Aliases("lbd")]
        public async Task Leaderboard(CommandContext context)
        {
            List<MemberEntity> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m=>m.Bubbles).Where(m=>m.Bubbles > 0).ToList();
            
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "🔮  Leaderboard of " + context.Guild.Name;
            embed.Color = DiscordColor.Magenta;

            members.ForEach(async m =>
            {
                string name = (await context.Guild.GetMemberAsync(Convert.ToUInt64(JsonConvert.DeserializeObject<Member>(m.Member).UserID))).Username;
                embed.AddField("**#" + (members.IndexOf(m) + 1).ToString() + " - " + name + "**", BubbleWallet.GetBubbles(m.Login).ToString() + " Bubbles - " + BubbleWallet.GetDrops(m.Login).ToString() + " Drops\n\u200b", true);
            });

            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Manual on how to use Bubbles")]
        [Command("Bubbles")]
        public async Task Bubbles(CommandContext context)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "🔮  How to Bubble ";
            embed.Color = DiscordColor.Magenta;
            embed.AddField("What are Bubbles?", "Bubbles are a fictional currency of the Palantir Bot.\nWhen you're connected to the Bot, you will be rewarded 1 Bubble every 10 seconds.\nBubbles are used to buy Sprites which other users of the Skribbl-Typo extension can see.\nOnce in a while, on the skribbl canvas will apear a drop icon - the player who clicks it first is rewarded a Drop to their inventory.\nA Drop is worth 50 Bubbles and adds up to your Bubble credit.");
            embed.AddField("Commands", "➜ `>inventory` List your Sprites and Bubble statistics.\n➜ `>sprites` Show all buyable Sprites.\n➜ `>sprites [id]` Show a specific Sprite.\n➜ `>buy [id]` Buy a Sprite.\n➜ `>use [id]` Select one of your Sprites.\n➜ `>leaderboard` Show your server's leaderboard.\n➜ `>calc` Calculate various things.");

            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Fancy calculation stuff")]
        [Command("calc")]
        public async Task Calc(CommandContext context, [Description("Calc mode: bubbles, rank or sprite")]string mode="", [Description("Whatever fits your mode.")] double target=0)
        {
            double hours = 0;

            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            switch (mode)
            {
                case "sprite":
                    List<Sprite> available = BubbleWallet.GetAvailableSprites();
                    Sprite sprite = available.FirstOrDefault(s => (ulong)s.ID == target);
                    hours = ((double)sprite.Cost - BubbleWallet.CalculateCredit(login)) / 360;
                    await Program.SendEmbed(context.Channel, "🔮  Time to get " + sprite.Name + ":", TimeSpan.FromHours(hours).ToString() + " hours on skribbl.io left.") ;
                    break;
                case "bubbles":
                    hours = (double)target / 360;
                    await Program.SendEmbed(context.Channel, "🔮  Time to get " + target + " more Bubbles:", TimeSpan.FromHours(hours).ToString() + " hours on skribbl.io left.");
                    break;
                case "rank":
                    List<MemberEntity> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => m.Bubbles).Where(m => m.Bubbles > 0).ToList();
                    hours = ((double)members[Convert.ToInt32(target) - 1].Bubbles - BubbleWallet.GetBubbles(login)) / 360;
                    await Program.SendEmbed(context.Channel, "🔮  Time catch up #" + target + ":", TimeSpan.FromHours(hours).ToString() + " hours on skribbl.io left.");
                    break;
                default:
                    await Program.SendEmbed(context.Channel, "🔮  Calculate following things:", "➜ `>calc sprite 1` Calculate remaining hours to get Sprite 1 depending on your actual Bubbles left.\n➜ `>calc bubbles 1000` Calculate remaining hours to get 1000 more bubbles.\n➜ `>calc rank 4` Calculate remaining hours to catch up the 4rd ranked member.");
                    break;
            }
        }
    }
}
