using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Linq;
using MoreLinq.Extensions;
using Newtonsoft.Json;
using DSharpPlus.Interactivity;
using System.Globalization;

namespace Palantir
{
    public class Commands : BaseCommandModule
    {
        
        [Command("manual")]
        [Description("Show the manual for bot usage")]
        public async Task Manual(CommandContext context)
        {
            string msg = "";
            msg += "**Visit the website:**\n";
            msg += "https://typo.rip\n";
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
            await context.RespondAsync("Updated header setting.");
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
            await context.RespondAsync("Updated idle setting.");
        }

        [Command("addwebhook")]
        [Description("Add a new webhook")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task AddWebhook(CommandContext context, [Description("Name of the webhook")] string name, [Description("URL of the webhook")] string url)
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());

            Tether target = Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString());
            if (target.PalantirEndpoint.Webhooks is null)target.PalantirEndpoint.Webhooks = new List<Webhook>();
            target.PalantirEndpoint.Webhooks.Add(new Webhook
            {
                Guild = target.PalantirEndpoint.GuildName,
                URL = url,
                Name = name
            });

            Program.Feanor.SavePalantiri(target.PalantirEndpoint);
            Program.Feanor.UpdateMemberGuilds();
            await context.RespondAsync("Webhook added.");
        }



        [Command("webhooks")]
        [Description("Show all webhooks for this server")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Webhooks(CommandContext context, [Description("True if all webhooks should be removed.")] bool clearAll = false)
        {
            if (!Program.Feanor.PalantirTethers.Any(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()))
            {
                await context.Message.RespondAsync("Set a channel before configuring the settings!");
                return;
            }


            Tether target = Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString());
            string hooks = "";
            if (target.PalantirEndpoint.Webhooks is null || target.PalantirEndpoint.Webhooks.Count < 1) hooks = "No webhooks added.";
            else target.PalantirEndpoint.Webhooks.ForEach(h =>
            {
                hooks += "- " + h.Name + ": " + h.URL + "\n";
            });
            await context.RespondAsync(hooks);

            if (clearAll)
            {
                target.PalantirEndpoint.Webhooks = null;
                Program.Feanor.SavePalantiri(target.PalantirEndpoint);
                Program.Feanor.UpdateMemberGuilds();
                await context.RespondAsync("Those webhooks were removed.");
            }
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
            await context.RespondAsync("Updated timezone setting.");
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
            await context.RespondAsync("Updated token visibility setting.");
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
            await context.RespondAsync("Updated refreshed visibility setting.");
        }

        [Command("animated")]
        [Description("Set whether the animated emojis should be displayed or not.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Animated(CommandContext context, [Description("State (on/off)")] string state)
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
            Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()).PalantirSettings.ShowAnimatedEmojis = state == "on";
            Program.Feanor.UpdatePalantirSettings(Program.Feanor.PalantirTethers.FirstOrDefault(t => t.PalantirEndpoint.GuildID == context.Guild.Id.ToString()));
            await context.RespondAsync("Updated animated emoji setting.");
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
            await context.Message.RespondAsync("Active lobbies will now be observed in " + context.Message.MentionedChannels[0].Mention + ".\nUsers need following token to connect the browser extension: ```fix\n" + token + "\n```Pin this message or save the token!\n\nFor further instructions, users can visit the website https://typo.rip.\nMaybe include the link in the bot message!");
            // save observed
            Program.Feanor.SavePalantiri(guild);
        }

        [Command("switch")]
        [Description("Set a channel where lobbies will be observed.")]
        [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
        [RequireGuild()]
        public async Task Switch(CommandContext context, [Description("Target channel (#channel)")]string channel)
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());
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
        [Aliases("spt","sprite")]
        public async Task Sprites(CommandContext context, [Description("The id of the sprite (eg '15')")] int sprite = 0)
        {
            List<Sprite> sprites = BubbleWallet.GetAvailableSprites();

            if (sprites.Any(s => s.ID == sprite))
            {
                Sprite s = BubbleWallet.GetSpriteByID(sprite);
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Color = DiscordColor.Magenta;
                embed.Title = s.Name + (s.EventDropID > 0 ? " (Event Sprite)" : "") ;
                embed.ImageUrl = s.URL;
                if (s.EventDropID <= 0)
                {
                    embed.Description = "**Costs:** " + s.Cost + " Bubbles\n\n**ID**: " + s.ID + (s.Special ? " :sparkles: " : "");
                }
                else
                {
                    EventDropEntity drop = Events.GetEventDrops().FirstOrDefault(d => d.EventDropID == s.EventDropID);
                    embed.Description = "**Event Drop Price:** " + s.Cost + " " + drop.Name + "\n\n**ID**: " + s.ID + (s.Special ? " :sparkles: " : "");
                    embed.WithThumbnail(drop.URL);
                }
                embed.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)\n[Try out the sprite](https://tobeh.host/Orthanc/sprites/cabin/?sprite=" + sprite + ")");
                await context.Channel.SendMessageAsync(embed: embed);
                return;
            }

            DiscordEmbedBuilder list = new DiscordEmbedBuilder();
            list.Color = DiscordColor.Magenta;
            list.Title = "🔮 3 random Sprites";
            list.Description = "Show one of the available Sprites with `>sprites [id]`";
            List<int> randoms = new List<int>();
            while(randoms.Count < 3)
            {
                int random = 0;
                while(randoms.Contains(random) || random == 0) random = (new Random()).Next(sprites.Count - 1) + 1;
                randoms.Add(random);
                list.AddField("**" + sprites[random].Name + "** ", "Costs: " + sprites[random].Cost + " Bubbles\nID: " + sprites[random].ID + (sprites[random].Special ? " :sparkles: " : ""),true);
            };
            list.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)");
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

            List<SpriteProperty> active = new List<SpriteProperty>();
            
            if(inventory.Count > 20)
            {
                string invList = ""; 
                inventory.OrderBy(s => s.ID).ToList().ForEach(s =>
                {
                    invList += "**#" + s.ID + "** - " + s.Name + " " + (s.Special ? ":sparkles:" : "") + "\n";
                    if (s.Activated) active.Add(s);
                });
                if(invList.Length < 1024) embed.AddField("All Sprites:", invList);
                else
                {
                    List<string> lines = invList.Split("\n").ToList();
                    lines.Batch(5).ForEach(b =>
                    {
                        string batch = b.ToDelimitedString("\n");
                        if(!String.IsNullOrWhiteSpace(batch)) embed.AddField("\u200b ", batch, true);
                    });
                }
            }
            else inventory.OrderBy(s => s.ID).ToList().ForEach(s =>
            {
                embed.AddField("**" + s.Name + "** " ,  "#" + s.ID + " |  Worth " + s.Cost + (s.EventDropID > 0 ? " Event Drops" : " Bubbles") + (s.Special ? " :sparkles: " : ""),true);
                if (s.Activated) active.Add(s);
            });

            string desc = "";

            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (perm.BubbleFarming) desc += "`🚩 Flagged as 'bubble farming'.`\n\n";
            if (perm.BotAdmin) desc += "`✔️ Verified cool guy.`\n\n";
            if (perm.RestartAndUpdate) desc += "`🛠️ Can restart & update Palantir.`\n\n";

            active.ForEach(sprite =>
            {
                if(active.Count <= 1)
                {
                    desc += "\n**Selected sprite:** " + sprite.Name;
                    embed.ImageUrl = sprite.URL;
                }
                else
                {
                    desc += "\n**Slot " + sprite.Slot + ":** " + sprite.Name;
                    if(sprite.Slot == 1) embed.ImageUrl = sprite.URL;
                }
            });
            int drops = BubbleWallet.GetDrops(login);
            if (inventory.Count <= 0) desc = "You haven't unlocked any sprites yet!";
            desc += "\n\n🔮 **" + BubbleWallet.CalculateCredit(login) + "** of "+ BubbleWallet.GetBubbles(login) + " collected Bubbles available.";
            desc += "\n\n💧 **" + drops + "** Drops collected.";
            if(drops >= 1000 || perm.BotAdmin) desc += "\n\n<a:chest:810521425156636682> **" + (perm.BotAdmin ? "∞" : (drops / 1000 + 1).ToString()) + " ** Sprite slots available.";

            embed.AddField("\u200b ", desc);

            if(inventory.Count < 5) embed.AddField("\u200b ", "Use `>use [id]` to select your Sprite!\n`>use 0` will set no Sprite.\nBuy a Sprite with `>buy [id]`.\nSpecial Sprites :sparkles: replace your whole avatar! ");
            embed.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)");
            await context.Channel.SendMessageAsync(embed:embed);
           
        }

        [Description("Choose your sprite.")]
        [Command("use")]
        public async Task Use(CommandContext context, [Description("The id of the sprite (eg '15')")] int sprite, [Description("The sprite-slot which will be set. Starts at slot 1.")] int slot = 1, [Description("A timeout in seconds when the action will be performed")] int timeoutSeconds = 0)
        {
            if (timeoutSeconds > 0)
            {
                await Program.SendEmbed(context.Channel, "Tick tock...", "The command will be executed in " + timeoutSeconds + "s.", "", DiscordColor.Green.Value);
                await Task.Delay(timeoutSeconds * 1000);
            }
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

            MemberEntity member = Program.Feanor.GetMemberByLogin(login);
            PermissionFlag perm = new PermissionFlag((byte)member.Flag);

            if (!perm.BotAdmin && (slot < 1 || slot > BubbleWallet.GetDrops(login) / 1000 + 1))
            {
                await Program.SendEmbed(context.Channel, "Out of your league.", "You can't use that sprite slot!\nFor each thousand collected drops, you get one extra slot.");
                return;
            }

            if (sprite == 0)
            {
                await Program.SendEmbed(context.Channel, "Minimalist, huh? Your sprite was disabled.", "");
                inventory.ForEach(i => i.Activated =false);
                BubbleWallet.SetInventory(inventory, login);
                return;
            }

            inventory.ForEach(i => { i.Activated = i.ID == sprite; i.Slot = i.Activated ? slot : -1; });
            BubbleWallet.SetInventory(inventory, login);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Your fancy sprite on slot " + slot + "was set to **" + BubbleWallet.GetSpriteByID(sprite).Name + "**";
            embed.ImageUrl = BubbleWallet.GetSpriteByID(sprite).URL;
            embed.Color = DiscordColor.Magenta;
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Buy a sprite.")]
        [Command("buy")]
        public async Task Buy(CommandContext context, [Description("The id of the sprite (eg '15')")]int sprite)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
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
            PermissionFlag perm = new PermissionFlag((byte)member.Flag);
            if (target.EventDropID <= 0)
            {
                if (credit < target.Cost && !perm.BotAdmin)
                {
                    await Program.SendEmbed(context.Channel, "Haha, nice try -.-", "That stuff is too expensive for you. \nSpend few more hours on skribbl.");
                    return;
                }
            }
            else
            {
                if (BubbleWallet.GetRemainingEventDrops(login, target.EventDropID) < target.Cost && !perm.BotAdmin)
                {
                    await Program.SendEmbed(context.Channel, "Haha, nice try -.-", "That stuff is too expensive for you. \nSpend few more hours on skribbl.");
                    return;
                }
            }

            inventory.Add(new SpriteProperty(target.Name, target.URL, target.Cost, target.ID, target.Special, target.EventDropID, false, -1));
            BubbleWallet.SetInventory(inventory, login);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Whee!";
            embed.Description = "You unlocked **" + target.Name + "**!\nActivate it with `>use " + target.ID + "`" ;
            embed.Color = DiscordColor.Magenta;
            embed.ImageUrl = target.URL;
            await context.Channel.SendMessageAsync(embed: embed);

            return;
        }

        [Description("See who's got the most bubbles.")]
        [Command("Leaderboard")]
        [Aliases("lbd","ldb")]
        public async Task Leaderboard(CommandContext context)
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());
            DiscordMessage leaderboard = await context.RespondAsync("Loding data...");
            var interactivity = context.Client.GetInteractivity();
            List<MemberEntity> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m=>m.Bubbles).Where(m=>m.Bubbles > 0).ToList();
            List<DiscordEmbedBuilder> embedPages = new List<DiscordEmbedBuilder>();
            IEnumerable<IEnumerable<MemberEntity>> memberBatches = members.Batch(5);
            int unranked = 0;
            foreach (IEnumerable<MemberEntity> memberBatch in memberBatches)
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Title = "🔮  Leaderboard of " + context.Guild.Name;
                embed.Color = DiscordColor.Magenta;

                foreach(MemberEntity member in memberBatch)
                {
                    string name = member.Bubbles.ToString();
                    PermissionFlag perm = new PermissionFlag((byte)member.Flag);
                    try { name=(await context.Guild.GetMemberAsync(Convert.ToUInt64(JsonConvert.DeserializeObject<Member>(member.Member).UserID))).Username; }
                    catch { };
                    if (perm.BubbleFarming)
                    {
                        unranked++;
                        embed.AddField("`🚩`" + " - " + name ," `This player has been flagged as *bubble farming*`.\n\u200b");
                    }
                    else embed.AddField("**#" + (members.IndexOf(member) + 1 - unranked).ToString() + " - " + name + "**" + (perm.BotAdmin ? " ` Admin`" : ""), BubbleWallet.GetBubbles(member.Login).ToString() + " Bubbles\n" + BubbleWallet.GetDrops(member.Login).ToString() + " Drops\n\u200b");
                }
                embed.WithFooter(context.Member.DisplayName +  " can react within 2 mins to show the next page.");
                embedPages.Add(embed);
            }

            DiscordEmoji next = DiscordEmoji.FromName(Program.Client, ":arrow_right:");
            await leaderboard.ModifyAsync(content: "", embed: embedPages[0].Build());
            await leaderboard.CreateReactionAsync(next);
            int page = 0;

            while (!(await interactivity.WaitForReactionAsync(reaction => reaction.Emoji == next, context.User, TimeSpan.FromMinutes(2))).TimedOut)
            {
                try 
                { 
                    await leaderboard.DeleteAllReactionsAsync();
                    await leaderboard.CreateReactionAsync(next);
                }
                catch { }
                page++;
                if (page >= embedPages.Count) page = 0;
                await leaderboard.ModifyAsync(embed: embedPages[page].Build());
            }

            try { await leaderboard.DeleteAllReactionsAsync(); }
            catch { }
            await leaderboard.CreateReactionAsync(DiscordEmoji.FromName(Program.Client, ":no_entry_sign:"));
        }

        [Description("Manual on how to use Bubbles")]
        [Command("Bubbles")]
        public async Task Bubbles(CommandContext context)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "🔮  How to Bubble ";
            embed.Color = DiscordColor.Magenta;
            embed.AddField("What are Bubbles?", "Bubbles are a fictional currency of the Palantir Bot.\nWhen you're connected to the Bot, you will be rewarded 1 Bubble every 10 seconds.\nBubbles are used to buy Sprites which other users of the Skribbl-Typo extension can see in your Skribbl avatar.\nOnce in a while, on the skribbl canvas will apear a drop icon - the player who clicks it first is rewarded a Drop to their inventory.\nA Drop is worth 50 Bubbles and adds up to your Bubble credit.");
            embed.AddField("Commands", "➜ `>inventory` List your Sprites and Bubble statistics.\n➜ `>sprites` Show all buyable Sprites.\n➜ `>sprites [id]` Show a specific Sprite.\n➜ `>buy [id]` Buy a Sprite.\n➜ `>use [id]` Select one of your Sprites.\n➜ `>leaderboard` Show your server's leaderboard.\n➜ `>calc` Calculate various things.\n➜ `>event` Show details for the current event.");

            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Show bubble gain statistics")]
        [Command("stat")]
        public async Task Stat(CommandContext context, [Description("Time span mode: 'day', 'week' or 'month'.")]string mode = "day")
        {
            CultureInfo iv = CultureInfo.InvariantCulture;
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            string msg = "```css\n";
            Tracer.BubbleTrace trace;
            if (mode == "week") 
            { 
                trace = new Tracer.BubbleTrace(login, 7 * 10); 
                trace.History = trace.History.Where(
                    t => t.Key.DayOfWeek == DayOfWeek.Monday || t.Key == trace.History.Keys.Min() || t.Key == trace.History.Keys.Max()
                    ).ToDictionary(); 
                msg += " Weekly"; }
            else if (mode == "month") 
            { 
                trace = new Tracer.BubbleTrace(login); 
                trace.History = trace.History.Where(
                     t => t.Key.Day == 1 || t.Key == trace.History.Keys.Min() || t.Key == trace.History.Keys.Max()
                     ).ToDictionary();
                msg += " Monthly"; 
            }
            else 
            { 
                trace = new Tracer.BubbleTrace(login, 30); 
                msg += " Daily"; 
            }

            msg += " Bubble-Gain from " + Convert.ToDateTime(trace.History.Keys.Min()).ToString("M", iv) + " to " + Convert.ToDateTime(trace.History.Keys.Max()).ToString("M", iv) + "\n\n";
            double offs = trace.History.Values.Min() * 0.8;
            double res = (trace.History.Values.Max()-offs) / 45;
            int prev = trace.History.Values.Min();

            trace.History.ForEach(t =>
            {
                msg += (Convert.ToDateTime(t.Key)).ToString("dd.MM") + " " +  
                new string('█', (int)Math.Round((t.Value-offs) / res, 0)) + 
                (t.Value - prev > 0 ? "    +" + (t.Value - prev) : "") + 
                ( trace.History.Keys.ToList().IndexOf(t.Key) == 0 || trace.History.Keys.ToList().IndexOf(t.Key) == trace.History.Count - 1 ? "    @" + t.Value : "") +
                "\n";
                prev = t.Value;
            });
            msg += "```\n";
            int diff = trace.History.Values.Max() - trace.History.Values.Min();
            int diffDays = (trace.History.Keys.Max() - trace.History.Keys.Min()).Days;
            msg += "> ➜ Total gained: `" + diff + " Bubbles` \n";
            double hours  = (double)diff / 360;
            msg += "> ➜ Equals `" + (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h "
                + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                + TimeSpan.FromHours(hours).Seconds.ToString() + "s` on skribbl.io\n";
            msg += "> ➜ Average `" + (diff / diffDays) + " Bubbles` per day";

            await context.Channel.SendMessageAsync(msg);
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
                    if(sprite.EventDropID > 0) { await Program.SendEmbed(context.Channel, "This is an event sprite!", "It can only be bought with event drops."); return; }
                    hours = ((double)sprite.Cost - BubbleWallet.CalculateCredit(login)) / 360;
                    await Program.SendEmbed(context.Channel, "🔮  Time to get " + sprite.Name + ":", 
                        (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h " 
                        + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                        + TimeSpan.FromHours(hours).Seconds.ToString() + "s on skribbl.io left.") ;
                    break;
                case "bubbles":
                    hours = (double)target / 360;
                    await Program.SendEmbed(context.Channel, "🔮  Time to get " + target + " more Bubbles:",
                        (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h "
                        + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                        + TimeSpan.FromHours(hours).Seconds.ToString() + "s on skribbl.io left.");
                    break;
                case "rank":
                    List<MemberEntity> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => m.Bubbles).Where(m => m.Bubbles > 0).ToList();
                    hours = ((double)members[Convert.ToInt32(target) - 1].Bubbles - BubbleWallet.GetBubbles(login)) / 360;
                    await Program.SendEmbed(context.Channel, "🔮  Time catch up #" + target + ":",
                        (TimeSpan.FromHours(hours).Days * 24 + TimeSpan.FromHours(hours).Hours).ToString() + "h "
                        + TimeSpan.FromHours(hours).Minutes.ToString() + "min "
                        + TimeSpan.FromHours(hours).Seconds.ToString() + "s on skribbl.io left.");
                    break;
                default:
                    await Program.SendEmbed(context.Channel, "🔮  Calculate following things:", "➜ `>calc sprite 1` Calculate remaining hours to get Sprite 1 depending on your actual Bubbles left.\n➜ `>calc bubbles 1000` Calculate remaining hours to get 1000 more bubbles.\n➜ `>calc rank 4` Calculate remaining hours to catch up the 4rd ranked member.");
                    break;
            }
        }

        [Description("Create a new seasonal event")]
        [Command("newevent")]
        public async Task CreateEvent(CommandContext context, [Description("The event name")] string name, [Description("The duration of the event in days")]int duration, [Description("The count of days when the event will start")]int validInDays, [Description("The event description")]params string[] description)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Ts ts...", "This command is only available for higher beings.\n||Some call them Bot-Admins ;))||");
                return;
            }

            PalantirDbContext dbcontext = new PalantirDbContext(); 

            EventEntity newEvent = new EventEntity();
            newEvent.EventName = name;
            newEvent.DayLength = duration;
            newEvent.ValidFrom = DateTime.Now.AddDays(validInDays).ToShortDateString();
            newEvent.Description = description.ToDelimitedString(" ");
            if (dbcontext.Events.Count() <= 0) newEvent.EventID = 0;
            else newEvent.EventID = dbcontext.Events.Max(e => e.EventID) + 1;

            if (dbcontext.Events.ToList().Any(otherEvent =>
                    !((Convert.ToDateTime(newEvent.ValidFrom) > Convert.ToDateTime(otherEvent.ValidFrom).AddDays(otherEvent.DayLength)) || // begin after end
                    (Convert.ToDateTime(otherEvent.ValidFrom) > Convert.ToDateTime(newEvent.ValidFrom).AddDays(newEvent.DayLength)))      // end before begin
                )
             )
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's already an event running in that timespan.\nCheck '>event'");
                return;
            }

            dbcontext.Events.Add(newEvent);
            dbcontext.SaveChanges();
            dbcontext.Dispose();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Event created: **" + newEvent.EventName + "**";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("The event lasts from  " + newEvent.ValidFrom + " to " + Convert.ToDateTime(newEvent.ValidFrom).AddDays(newEvent.DayLength).ToShortDateString());
            embed.AddField("Make the event fancy!", "➜ `>eventdrop " + newEvent.EventID + " coolname` Send this command with an attached gif to add a event drop.");

            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Add a seasonal drop to an event")]
        [Command("eventdrop")]
        public async Task CreateEventDrop(CommandContext context, [Description("The id of the event for the event drop")] int eventID, [Description("The name of the event drop")] string name)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Ts ts...", "This command is only available for higher beings.\n||Some call them Bot-Admins ;))||");
                return;
            }

            PalantirDbContext dbcontext = new PalantirDbContext();

            if(!dbcontext.Events.Any(e=>e.EventID == eventID))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no event with that id.\nCheck `>upcoming`");
                return;
            }
            if (context.Message.Attachments.Count <= 0 || !context.Message.Attachments[0].FileName.EndsWith(".gif"))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no valid gif attached.");
                return;
            }

            EventDropEntity newDrop = new EventDropEntity();
            newDrop.EventID = eventID;
            newDrop.Name = name.Replace("_"," ");
            newDrop.URL = context.Message.Attachments[0].Url;
            if (dbcontext.EventDrops.Count() <= 0) newDrop.EventDropID = 0;
            else newDrop.EventDropID = dbcontext.EventDrops.Max(e => e.EventDropID) + 1;

            dbcontext.EventDrops.Add(newDrop);
            dbcontext.SaveChanges();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Drop added to " + dbcontext.Events.FirstOrDefault(e=>e.EventID == eventID).EventName + ": **" + newDrop.Name + "**";
            embed.Color = DiscordColor.Magenta;
            embed.WithThumbnail(newDrop.URL);
            embed.WithDescription("The ID of the Drop is  " + newDrop.EventDropID + ".\nAdd a seasonal Sprite which can be bought with the event drops to make your event complete:\n" +
                "➜ `>eventsprite " + newDrop.EventDropID + " [name] [price]` with the sprite-gif attached.");

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Show upcoming events")]
        [Command("upcoming")]
        public async Task UpcomingEvents(CommandContext context)
        {
            List<EventEntity> events = Events.GetEvents(false);
            string eventsList = "";
            events = events.Where(e => Convert.ToDateTime(e.ValidFrom) >= DateTime.Now).OrderByDescending(e => Convert.ToDateTime(e.ValidFrom)).ToList();
            events.ForEach(e =>
            {
                eventsList += "➜ **" + e.EventName + "**: " + e.ValidFrom + " to " + Convert.ToDateTime(e.ValidFrom).AddDays(e.DayLength).ToShortDateString() + "\n";
                eventsList += e.Description + "\n\n";
            });
            if (eventsList == "") eventsList = "There are no upcoming events :( \nAsk a responsible person to create one!";

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Upcoming Events:";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription(eventsList);
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Show passed events")]
        [Command("passed")]
        public async Task PassedEvents(CommandContext context)
        {
            List<EventEntity> events = Events.GetEvents(false);
            string eventsList = "";
            events = events.Where(e => Convert.ToDateTime(e.ValidFrom).AddDays(e.DayLength) < DateTime.Now).OrderByDescending(e => Convert.ToDateTime(e.ValidFrom)).ToList();
            events.ForEach(e =>
            {
                eventsList += "➜ **" + e.EventName + "** [#" + e.EventID + "]: " + e.ValidFrom + " to " + Convert.ToDateTime(e.ValidFrom).AddDays(e.DayLength).ToShortDateString() + "\n";
                eventsList += e.Description + "\n\n";
            });
            if (eventsList == "") eventsList = "There have no events passed.";

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Passed Events:";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription(eventsList);
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Show event info")]
        [Command("event")]
        public async Task ShowEvent(CommandContext context, int eventID = 0)
        {
            List<EventEntity> events = Events.GetEvents(false);
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            List<SpriteProperty> inv = BubbleWallet.GetInventory(login);
            EventEntity evt;
            if (eventID < 1 || !events.Any(e => e.EventID == eventID)) evt = (Events.GetEvents().Count > 0 ? Events.GetEvents()[0] : null);
            else evt = events.FirstOrDefault(e => e.EventID == eventID);
            if (evt != null)
            {
                embed.Title = ":champagne: " + evt.EventName;
                embed.Color = DiscordColor.Magenta;
                embed.WithDescription(evt.Description + "\nLasts until " + Convert.ToDateTime(evt.ValidFrom).AddDays(evt.DayLength).ToString("MMMM dd") + "\n");

                string dropList = "";
                Events.GetEventDrops(new List<EventEntity> { evt }).ForEach(e =>
                {
                    List<SpritesEntity> sprites = Events.GetEventSprites(e.EventDropID);
                    sprites.ForEach(sprite =>
                    {
                        dropList += "➜ **" + sprite.Name + "** (#" + sprite.ID + ")\n" + BubbleWallet.GetEventCredit(login, e.EventDropID) + " / " + sprite.Cost + " " + e.Name + " Drops collected " + (inv.Any(s => s.ID == sprite.ID) ? ":package:" : "") + "\n\n";
                    });
                    
                });
                embed.AddField("Event Sprites", dropList == "" ? "No drops added yet." : dropList);
                embed.AddField("\u200b","Use `>sprite [id]` to see the event drop and sprite!");
            }
            else
            {
                embed.Title = ":champagne: No Event active :(";
                embed.Color = DiscordColor.Magenta;
                embed.WithDescription("Check all events with `>upcoming`.\nSee past events with `>passed [id]`.\nGift event drops with `>gift [@person] [amount of drops] [id of the sprite]`.\nBtw - I keep up to 50% of the gift for myself! ;)");
            }

            await context.Channel.SendMessageAsync(embed: embed);

        }

        [Description("Calulate random loss average")]
        [Command("loss")]
        public async Task Loss(CommandContext context, [Description("The amount of a single gift")] int amount, [Description("The total drop amount with repeated gifts of before specified amount")] int total = 0)
        {
            int sum = 0;
            for(int i = 0; i<100; i++) sum += (new Random()).Next(0, amount / 3 + 1);
            string totalres = "";
            if(total > amount)
            {
                int times = total / amount + (total % amount > 0 ? 1 : 0);
                totalres = "\nTo gift a total of " + total + " Drops " + times + " gifts of each " + amount + " Drops are required, which equals a loss of " + Math.Round((sum * times) / 100.0, 2) + " Drops.";
            }
            await Program.SendEmbed(context.Channel, "Such a nerd...", "With 100 random tries, an average of " + Math.Round(sum / 100.0, 2) + " Drops of " + amount + " gifted Drops is lost." + totalres);
        }

        [Description("Gift event drops")]
        [Command("gift")]
        public async Task Gift(CommandContext context, [Description("The gift receiver (@member)")] DiscordMember target, [Description("The amount of gifted event drops")] int amount, [Description("The id of the sprite which can be bought with the gifted event drops")] int eventSpriteID)
        {
            if (amount < 0)
            {
                await Program.SendEmbed(context.Channel, "LOL!", "Your'e tryna steal some stuff, huh?");
                return;
            }
            if (amount < 3)
            {
                await Program.SendEmbed(context.Channel, "That's all you got?", "The minimal gift amount is 3 event drops.");
                return;
            }
            List<Sprite> sprites = BubbleWallet.GetAvailableSprites();
            if(!sprites.Any(s=>s.ID == eventSpriteID && s.EventDropID != 0))
            {
                await Program.SendEmbed(context.Channel, "Hmmm...", "That sprite doesn't exist or is no event sprite.");
                return;
            }
            int eventDropID = sprites.FirstOrDefault(s => s.ID == eventSpriteID).EventDropID;
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            int credit = BubbleWallet.GetRemainingEventDrops(login, eventDropID);
            List<SpriteProperty> inv = BubbleWallet.GetInventory(login);

            List<EventDropEntity> drops = Events.GetEventDrops();
            string name = drops.FirstOrDefault(d => d.EventDropID == eventDropID).Name;
            if (credit - amount < 0)
            {
                await Program.SendEmbed(context.Channel, "You can't trick me!", "Your event credit is too few. You have only " + credit + " " + name + " left.");
                return;
            }
            int lost = (new Random()).Next(0, amount / 3 + 1);
            string targetLogin = BubbleWallet.GetLoginOfMember(target.Id.ToString());

            if(BubbleWallet.ChangeEventDropCredit(targetLogin, eventDropID, amount - lost))
                BubbleWallet.ChangeEventDropCredit(login, eventDropID, -amount);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne: Awww!";
            embed.WithDescription("You gifted " + target.DisplayName + " " + amount + " " + name + "!\nHowever, " + lost + " of them got lost in my pocket :(");
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Add a seasonal sprite to an event")]
        [Command("eventsprite")]
        public async Task CreateEventSprite(CommandContext context, [Description("The id of the event drop for the sprite")] int eventDropID, [Description("The name of the sprite")] string name, [Description("The event drop price")] int price, [Description("Any string if the sprite should replace the avatar")]string special = "")
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Ts ts...", "This command is only available for higher beings.\n||Some call them Bot-Admins ;))||");
                return;
            }

            PalantirDbContext dbcontext = new PalantirDbContext();

            if (!dbcontext.EventDrops.Any(e => e.EventDropID == eventDropID))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no event drop with that id.\nCheck `>upevent`");
                return;
            }
            if (context.Message.Attachments.Count <= 0 || !context.Message.Attachments[0].FileName.EndsWith(".gif"))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no valid gif attached.");
                return;
            }
            if (price < 10)
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "We don't gift sprites. The price is too low.");
                return;
            }
            if (String.IsNullOrWhiteSpace(name))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "Something went wrong with the name.");
                return;
            }

            // download sprite
            System.Net.WebClient client = new System.Net.WebClient();
            client.DownloadFile(context.Message.Attachments[0].Url, "/home/pi/Webroot/eventsprites/evd" + eventDropID + name + ".gif");

            Sprite eventsprite = new Sprite(
                name.Replace("_"," "), 
                "https://tobeh.host/eventsprites/evd" + eventDropID + name + ".gif", 
                price, 
                dbcontext.Sprites.Where(s => s.ID < 1000).Max(s => s.ID) + 1, 
                special != "", eventDropID);
            BubbleWallet.AddSprite(eventsprite);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Sprite added to " + dbcontext.EventDrops.FirstOrDefault(e => e.EventDropID == eventDropID).Name + ": **" + eventsprite.Name + "**";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("ID: " + eventsprite.ID + "\nYou can buy and view the sprite with the usual comands.");
            embed.WithThumbnail(eventsprite.URL);

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Set a member flag.")]
        [Command("flag")]
        public async Task Flag(CommandContext context, [Description("The id of the member to flag")] ulong id, [Description("The new flag")] int flag = -1)
        {
            if(flag == -1)
            {
                DiscordUser target = await Program.Client.GetUserAsync(id);
                PermissionFlag getperm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(target));
                string getDesc = "Flag[0] BubbleFarming - "
                    + getperm.BubbleFarming + "\nFlag[1] BotAdmin - "
                    + getperm.BotAdmin + "\nFlag[2] RestartAndUpdate - " + getperm.RestartAndUpdate;
                await Program.SendEmbed(context.Channel, "The flags of " + target.Username,getDesc);
                return;
            }
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Ts ts...", "This command is only available for higher beings.\n||Some call them Bot-Admins ;))||");
                return;
            }
            Program.Feanor.SetFlagByID(id.ToString(), flag);
            string name = (await Program.Client.GetUserAsync(id)).Mention;
            PermissionFlag newFlag = new PermissionFlag((byte)flag);
            string desc = "Flag[0] BubbleFarming - " 
                + newFlag.BubbleFarming + "\nFlag[1] BotAdmin - " 
                + newFlag.BotAdmin + "\nFlag[2] RestartAndUpdate - " + newFlag.RestartAndUpdate;
            await Program.SendEmbed(context.Channel, "*magic happened*","The flag of " + name + " was set to " + flag + "\n" + desc);
        }

        [Description("Reboots the bot & pulls from git.")]
        [Command("hardreboot")]
        public async Task Reboot(CommandContext context)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin && !perm.RestartAndUpdate)
            {
                await Program.SendEmbed(context.Channel, "Ts ts...", "This command is only available for higher beings.\n||Some call them Bot-Admins ;))||");
                return;
            }

            string upd = "git -C /home/pi/Palantir pull".Bash();
            upd += "\n\n Latest commit: " + ("git log --oneline -1".Bash());
            await Program.SendEmbed(context.Channel, "[literally dies...]", "You made me do this!!!\n\n**Update result:**\n"+upd);
            string op = "sudo service palantir restart".Bash();
            Environment.Exit(0);
        }

        [Description("Execute a bash command from the pi root")]
        [Command("bash")]
        public async Task Bash(CommandContext context, params string[] command)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Hands off there!", "This command is only available for higher beings.\n||Some call them Bot-Admins ;))||");
                return;
            }
            string commandDelimited = command.ToDelimitedString(" ");
            string res = ("cd /home/pi/ && " + commandDelimited).Bash();
            await Program.SendEmbed(context.Channel, "**pi@raspberrypi: ~ $** " +  commandDelimited, res != "" ? res : "Error.");
        }

        [Description("Execute a sql command in the palantir database")]
        [Command("sql")]
        public async Task Sql(CommandContext context, params string[] sql)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Hands off there!", "This command is only available for higher beings.\n||Some call them Bot-Admins ;))||");
                return;
            }
            string sqlDelimited = sql.ToDelimitedString(" ");
            string res = ("sqlite3 /home/pi/Database/palantir.db \"" + sqlDelimited + "\"").Bash();
            await Program.SendEmbed(context.Channel, sqlDelimited, res != "" ? res : "Error.");
        }

        [Description("Gets ping statistics.")]
        [Command("ping")]
        public async Task Ping(CommandContext context)
        {
            System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
            long discordRTT = ping.Send("discord.gg", 100).RoundtripTime;

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            
            embed.Title = "Latency results:";
            embed.AddField("`🗄️` Database singe read", Program.Feanor.DatabaseReadTime(context.User.Id.ToString(), 1) + "ms");
            embed.AddField("`🗂️` Database average for 100 reads", Program.Feanor.DatabaseReadTime(context.User.Id.ToString(), 100) + "ms");
            embed.AddField("`🌐` Discord API request", Program.Client.Ping + "ms");
            embed.AddField("`⌛` Discord.gg ping RTT", discordRTT + "ms");
            await context.RespondAsync(embed: embed);
        }

    }
}
