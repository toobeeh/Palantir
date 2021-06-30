using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
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
        [RequireUserPermissions(DSharpPlus.Permissions.ManageWebhooks)]
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
                embed.Title = s.Name + (s.EventDropID > 0 ? " (Event Sprite)" : "");
                embed.ImageUrl = s.URL;
                embed.Description = "";
                if (!string.IsNullOrEmpty(s.Artist))
                {
                    embed.Description += "**Artist:** " + s.Artist + " \n";
                }
                if (s.EventDropID <= 0)
                {
                    embed.Description += "**Costs:** " + s.Cost + " Bubbles\n\n**ID**: " + s.ID + (s.Special ? " :sparkles: " : "");
                }
                else
                {
                    EventDropEntity drop = Events.GetEventDrops().FirstOrDefault(d => d.EventDropID == s.EventDropID);
                    embed.Description += "**Event Drop Price:** " + s.Cost + " " + drop.Name + "\n\n**ID**: " + s.ID + (s.Special ? " :sparkles: " : "");
                    embed.WithThumbnail(drop.URL);
                }
                embed.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)\n[Try out the sprite](https://tobeh.host/Orthanc/sprites/cabin/?sprite=" + sprite + ")");
                await context.Channel.SendMessageAsync(embed: embed);
            }
            else
            {
                DiscordEmbedBuilder list = new DiscordEmbedBuilder();
                list.Color = DiscordColor.Magenta;
                list.Title = "🔮 Top 10 Popular Sprites";
                list.Description = "Show one of the available Sprites with `>sprites [id]`";
                // get all bought sprites
                List<SpriteProperty> joined = new List<SpriteProperty>();
                PalantirDbContext db = new PalantirDbContext();
                db.Members.ForEach(member => {
                    string[] sprites = member.Sprites.Split(",");
                    sprites.ForEach(id =>
                    {
                        int indOfActive = id.ToString().LastIndexOf(".");
                        id = id.Substring(indOfActive < 0 ? 0 : indOfActive + 1);
                        int spriteid = 0;
                        if(Int32.TryParse(id, out spriteid))
                        {
                            joined.Add(new SpriteProperty("", "", 0, spriteid, false, 0, "", indOfActive >= 0, 0));
                        }
                    });
                });            
                db.Dispose();
                // calculate scores
                Dictionary<int, int[]> spriteScores = new Dictionary<int, int[]>();
                sprites.ForEach(sprite =>
                {
                    int score = 0;
                    int active = joined.Where(spriteprop => spriteprop.ID == sprite.ID && spriteprop.Activated).ToList().Count;
                    int bought = joined.Where(spriteprop => spriteprop.ID == sprite.ID && !spriteprop.Activated).ToList().Count;
                    score = active * 10 + bought;
                    int[] value = { score, active, bought };
                    spriteScores.Add(sprite.ID, value);
                });
                spriteScores = spriteScores.OrderByDescending(score => score.Value[0]).Slice(0, 10).ToDictionary();
                int rank = 1;
                spriteScores.ForEach(score =>
                {
                    Sprite spt = sprites.First(sprite => sprite.ID == score.Key);
                    list.AddField("**#" + rank + ": " + spt.Name + "** ", "ID: " + spt.ID + (spt.Special ? " :sparkles: " : "") + " - Active: " + score.Value[1] + ", Bought: " + score.Value[2]);
                    rank++;
                });
                list.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)");
                await context.Channel.SendMessageAsync(embed: list);
            }
        }

        [Description("Get a overview of your inventory. (old layout)")]
        [Command("oldinventory")]
        [Aliases("oldinv")]
        public async Task OldInventory(CommandContext context)
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
            if (perm.BubbleFarming) desc += "`🚩 Flagged as 'bubble farming'.`\n";
            if (perm.BotAdmin) desc += "`✔️ Verified cool guy aka Admin.`\n";
            if (perm.Moderator) desc += "`🛠️ Palantir Moderator.`\n";
            if (perm.CloudUnlimited || perm.Patron) desc += "`📦 Unlimited cloud storage.`\n";
            if (BubbleWallet.IsEarlyUser(login)) desc += "`💎 Early User.`\n";
            if (perm.Patron) desc += "`🎖️  Patron 💖`\n";

            List<int> spriteIDs = new List<int>();
            active.OrderBy(slot => slot.Slot).ForEach(sprite =>
            {
                spriteIDs.Add(sprite.ID);
                if(active.Count <= 1)
                {
                    desc += "\n**Selected sprite:** " + sprite.Name;
                }
                else
                {
                    desc += "\n**Slot " + sprite.Slot + ":** " + sprite.Name;
                }
            });
            if(spriteIDs.Count > 0)
            {
                string path = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(spriteIDs.ToArray()), "/home/pi/Webroot/files/combos/")
                    .Replace(@"/home/pi/Webroot/", "https://tobeh.host/");
                embed.ImageUrl = path;
            }

            int drops = BubbleWallet.GetDrops(login);
            if (inventory.Count <= 0) desc = "You haven't unlocked any sprites yet!";
            desc += "\n\n🔮 **" + BubbleWallet.CalculateCredit(login) + "** of "+ BubbleWallet.GetBubbles(login) + " collected Bubbles available.";
            desc += "\n\n💧 **" + drops + "** Drops collected.";
            if(drops >= 1000 || perm.BotAdmin || perm.Patron) desc += "\n\n<a:chest:810521425156636682> **" + (perm.BotAdmin ? "Infinite" : (drops / 1000 + 1 + (perm.Patron ? 1 : 0)).ToString()) + " ** Sprite slots available.";

            embed.AddField("\u200b ", desc);

            if(inventory.Count < 5) embed.AddField("\u200b ", "Use `>use [id]` to select your Sprite!\n`>use 0` will set no Sprite.\nBuy a Sprite with `>buy [id]`.\nSpecial Sprites :sparkles: replace your whole avatar! ");
            embed.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)");
            await context.Channel.SendMessageAsync(embed:embed);          
        }

        [Description("Get a overview of your inventory.")]
        [Command("inventory")]
        [Aliases("inv")]
        public async Task Inventory(CommandContext context, int batchsize = 7)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            int drops = BubbleWallet.GetDrops(login);
            int bubbles = BubbleWallet.GetBubbles(login);
            int credit = BubbleWallet.CalculateCredit(login);
            List<SpriteProperty> inventory = BubbleWallet.GetInventory(login).OrderBy(s => s.ID).ToList();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Magenta)
                .WithTitle("🔮  " + context.Message.Author.Username + "s Inventory");

            List<string> sprites = new List<string>();
            inventory.OrderBy(s => s.ID).ToList().ForEach(s =>
            {
                sprites.Add(
                    "**#" + s.ID + "** -" + s.Name
                    + (s.Special ? " :sparkles: " : ""));
            });

            embed.AddField("\u200b ",
                "🔮 **" + credit + " ** / " + bubbles + " Bubbles\n"
                + "💧 **" + drops + "** Drops caught");

            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            string flags = "";
            if (perm.BubbleFarming) flags += "`🚩 Bubble Farming`\n";
            if (perm.BotAdmin) flags += "`✔️ Verified cool guy aka Admin`\n";
            if (perm.Moderator) flags += "`🛠️ Palantir Moderator`\n";
            if (perm.CloudUnlimited || perm.Patron) flags += "`📦 Unlimited cloud storage`\n";
            if (BubbleWallet.IsEarlyUser(login)) flags += "`💎 Early User`\n";
            if (perm.Patron) flags += "`💖️ Patreon Subscriber`\n";
            if (perm.Patronizer) flags += "`🎁 Patronizer`\n";
            if(flags.Length > 0) embed.AddField("Flags:", flags);

            string selected = "";
            inventory.Where(spt => spt.Activated).OrderBy(slot => slot.Slot).ForEach(sprite =>
            {
                selected += "Slot " + sprite.Slot + ": " + sprite.Name + " (#" + sprite.ID + ")\n";
            });
            if (drops >= 1000 || perm.BotAdmin || perm.Patron) selected += "\n<a:chest:810521425156636682> **" + (perm.BotAdmin ? "Infinite" : (drops / 1000 + 1 + (perm.Patron ? 1 : 0)).ToString()) + " ** Sprite slots available.";
            embed.AddField("Selected Sprites:", selected.Length > 0 ? selected : "None");

            if (inventory.Where(spt => spt.Activated).Count() == 1) 
                embed.ImageUrl = inventory.FirstOrDefault(s => s.Activated).URL;
            if (inventory.Where(spt => spt.Activated).Count() > 1)
                embed.ImageUrl = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(
                    inventory.Where(s=>s.Activated).OrderBy(s=>s.Slot).Select(s=>s.ID).ToArray()), "/home/pi/Webroot/files/combos/")
                    .Replace(@"/home/pi/Webroot/", "https://tobeh.host/");

            DiscordEmbedField sleft = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            DiscordEmbedField smiddle = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            DiscordEmbedField sright = embed.AddField("\u200b ", "\u200b ", true).Fields.Last();
            var spritebatches = sprites.Batch(batchsize * 3);
            
            if (inventory.Count < 5) embed.AddField("Command help: ", "Use `>use [id]` to select your Sprite!\n`>use 0` will set no Sprite.\nBuy a Sprite with `>buy [id]`.\nSpecial Sprites :sparkles: replace your whole avatar! ");
            embed.AddField("\u200b", "[View all Sprites](https://typo.rip/#sprites)");
            DiscordMessageBuilder response = new DiscordMessageBuilder();
            DiscordButtonComponent prev = new DiscordButtonComponent(ButtonStyle.Secondary, "last", "Previous");
            DiscordButtonComponent next = new DiscordButtonComponent(ButtonStyle.Primary, "next", "Next");
            DiscordButtonComponent nav = new DiscordButtonComponent(ButtonStyle.Secondary, "nav", "Navigate Sprites", true);
            response.AddComponents(prev, nav, next);
            DiscordMessage sent = null;
            DSharpPlus.Interactivity.InteractivityResult<DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs> result;
            int direction = 0;
            do
            {
                // rotate batch so relevant is always first index 1 2 3 4 5 6 7 8 9 10 11 
                spritebatches = direction == 0 ? spritebatches : direction > 0 ?
                    spritebatches.Skip(1).Concat(spritebatches.Take(1)) :
                    Enumerable.TakeLast(spritebatches, 1).Concat(Enumerable.SkipLast(spritebatches, 1));

                //List<string>[] SplitListIntoEqualSized(int listcount, IEnumerable<string> source) 
                //{
                //    List<string>[] ret = new List<string>[listcount];
                //    ret[0] = source.ToList();
                //    for(int i = 1; i < listcount; i++)
                //    {
                //        ret[i] = (List<string>)Enumerable.TakeLast(ret[i - 1], source.Count() / listcount);
                //        ret[i-1] = (List<string>)Enumerable.SkipLast(ret[i - 1], source.Count() / listcount);
                //    }
                //    return ret;
                //}
                var firstbatch = spritebatches.Count() > 0 ? spritebatches.First() : Enumerable.Empty<string>();
                List<string>[] fielded = new List<string>[3];
                fielded[0] = firstbatch.ToList();
                for (int i = 1; i < 3; i++)
                {
                    fielded[i] = (List<string>)Enumerable.TakeLast(fielded[i - 1], firstbatch.Count() / 3);
                    fielded[i - 1] = (List<string>)Enumerable.SkipLast(fielded[i - 1], firstbatch.Count() / 3);
                }
                sleft.Value = fielded[0].Count() > 0 ? fielded[0].ToDelimitedString("\n") : "\u200b ";
                smiddle.Value = fielded[1].Count() > 0 ? fielded[1].ToDelimitedString("\n") : "\u200b ";
                sright.Value = fielded[2].Count() > 0 ? fielded[2].ToDelimitedString("\n") : "\u200b ";

                //var firstbatch = spritebatches.Count() > 0 ? spritebatches.First() : Enumerable.Empty<string>();
                //int size = firstbatch.Count();
                //sleft.Value = size > 0 ? firstbatch.Take(size / 3).ToDelimitedString("\n") : "\u200b ";
                //smiddle.Value = size > size / 3 ? 
                //    firstbatch.Skip(size/3).Take(size/3).ToDelimitedString("\n") : "\u200b ";
                //sright.Value = size > 2 * (size / 3) ? 
                //    firstbatch.Skip(2 * (size / 3)).ToDelimitedString("\n") : "\u200b ";
                nav.Label = "Navigate Sprites (" + firstbatch.Count() + "/" + spritebatches.Flatten().Count() + ")";
                response.Embed = embed.Build();
                sent = sent is null ? await response.SendAsync(context.Channel) : await sent.ModifyAsync(response);
                result = await Program.Interactivity.WaitForButtonAsync(sent, context.User, TimeSpan.FromMinutes(2));
                if (!result.TimedOut)
                {
                    await result.Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage);
                    direction = result.Result.Id == "next" ? 1 : -1;
                }
            }
            while(!result.TimedOut);
            prev.Disabled = next.Disabled = true;
            await sent.ModifyAsync(response);
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
            inventory = BubbleWallet.GetInventory(login);
            if (sprite !=0 && !inventory.Any(s=>s.ID == sprite))
            {
                await Program.SendEmbed(context.Channel, "Hold on!", "You don't own that. \nGet it first with `>buy " + sprite + "`.");
                return;
            }

            MemberEntity member = Program.Feanor.GetMemberByLogin(login);
            PermissionFlag perm = new PermissionFlag((byte)member.Flag);

            if (!perm.BotAdmin && (slot < 1 || slot > BubbleWallet.GetDrops(login) / 1000 + 1 + (perm.Patron ? 1 : 0)))
            {
                await Program.SendEmbed(context.Channel, "Out of your league.", "You can't use that sprite slot!\nFor each thousand collected drops, you get one extra slot.");
                return;
            }

            if (sprite == 0)
            {
                await Program.SendEmbed(context.Channel, "Minimalist, huh? Your sprite was disabled.", "");
                inventory.ForEach(i => {
                    if (i.Slot == slot) i.Activated = false;
                });
                BubbleWallet.SetInventory(inventory, login);
                return;
            }

            if (BubbleWallet.GetSpriteByID(sprite).Special && inventory.Any(active => active.Activated && active.Special && active.Slot != slot)){
                await Program.SendEmbed(context.Channel, "Too overpowered!!", "Only one of your sprite slots may have a special sprite.");
                return;
            }

            inventory.ForEach(i => {
                if (i.ID == sprite && i.Activated) i.Slot = slot; // if sprite is already activated, activate on other slot
                else if (i.ID == sprite && !i.Activated) { i.Activated = true; i.Slot = slot; } // if sprite is not activated, activate on slot
                else if (!(i.Activated && i.ID != sprite && i.Slot != slot)) {i.Activated = false; i.Slot = -1; } 
                // if sprite ist not desired not activated on slot deactivate
            });
            BubbleWallet.SetInventory(inventory, login);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Your fancy sprite on slot " + slot + " was set to **`" + BubbleWallet.GetSpriteByID(sprite).Name + "`**";
            embed.ImageUrl = BubbleWallet.GetSpriteByID(sprite).URL;
            embed.Color = DiscordColor.Magenta;
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Activate sprite slot combo.")]
        [Command("combo")]
        public async Task Combo(CommandContext context, [Description("The id of the sprites (eg '15 0 16 17')")] params int[] sprites)
        {
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            List<SpriteProperty> inventory = BubbleWallet.GetInventory(login);
            if (sprites.Any(sprite => sprite != 0 && !inventory.Any(item => item.ID == sprite)))
            {
                await Program.SendEmbed(context.Channel, "Gonna stop you right there.", "You don't own all sprites from this combo.");
                return;
            }

            MemberEntity member = Program.Feanor.GetMemberByLogin(login);
            PermissionFlag perm = new PermissionFlag((byte)member.Flag);

            if (!perm.BotAdmin && (sprites.Length < 1 || sprites.Length > BubbleWallet.GetDrops(login) / 1000 + 1 + (perm.Patron ? 1 : 0)))
            {
                await Program.SendEmbed(context.Channel, "Gotcha!", "You can't use that many sprite slots!\nFor each thousand collected drops, you get one extra slot.");
                return;
            }
            if (sprites.Where(sprite => !(BubbleWallet.GetSpriteByID(sprite) is null) && BubbleWallet.GetSpriteByID(sprite).Special).Count() > 1)
            {
                await Program.SendEmbed(context.Channel, "Too overpowered!!", "Only one of your sprite slots may have a special sprite.");
                return;
            }

            inventory.ForEach(item => {
                item.Activated = false;
                item.Slot = 0;
            });
            List<int> slots = sprites.ToList();
            slots.ForEach(slot =>
            {
                if (slot > 0) {
                    inventory.Find(item => item.ID == slot).Activated = true;
                    inventory.Find(item => item.ID == slot).Slot = slots.IndexOf(slot) + 1;
                }
            });
            BubbleWallet.SetInventory(inventory, login);

            sprites = sprites.Where(id => id > 0).ToArray();

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = "Your epic sprite combo was activated!";
            embed.Color = DiscordColor.Magenta;
            if (sprites.Length > 0)
            {
                string path = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(sprites), "/home/pi/Webroot/files/combos/")
                    .Replace(@"/home/pi/Webroot/", "https://tobeh.host/");
                embed.ImageUrl = path;
            }
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
            if(target.ID == 1003)
            {
                if (!perm.Patron)
                {
                    await Program.SendEmbed(context.Channel, "Haha, nice try -.-", "This sprite is exclusive for patrons!");
                    return;
                }
            }
            else if (target.EventDropID <= 0)
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

            inventory.Add(new SpriteProperty(target.Name, target.URL, target.Cost, target.ID, target.Special, target.EventDropID, target.Artist, false, -1));
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
        [Command("Old-Leaderboard")]
        [Aliases("oldlbd", "oldldb")]
        public async Task Leaderboard(CommandContext context, string mode = "bubbles")
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());
            Program.Feanor.UpdateMemberGuilds();
            //if (!Program.Feanor.PalantirMembers.FirstOrDefault(member => member.UserID == context.User.Id.ToString())
            //    .Guilds.Any(guild => guild.GuildID == context.Guild.Id.ToString()))
            //{
            //    await Program.SendEmbed(context.Channel, "Uh oh, caught you stalking!", "Connect to this discord server to use this command here.");
            //    return;
            //}
            DiscordMessage leaderboard = await context.RespondAsync("`⏱️` Loading members of `" + context.Guild.Name + "`...");
            var interactivity = context.Client.GetInteractivity();
            List<MemberEntity> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m=>(mode == "drops" ? m.Drops : m.Bubbles)).Where(m=>m.Bubbles > 0).ToList();
            List<IEnumerable<MemberEntity>> memberBatches = members.Batch(9).ToList();
            int unranked = 0;

            DiscordEmoji down = await (await Program.Client.GetGuildAsync(779435254225698827)).GetEmojiAsync(790349869138968596);
            int page = 0;
            do
            {
                try
                {
                    await leaderboard.DeleteAllReactionsAsync();
                }
                catch(Exception e) {}
                try
                {
                    await leaderboard.CreateReactionAsync(down);
                }
                catch (Exception e) { }
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Title = "🔮  Leaderboard of " + context.Guild.Name;
                embed.Color = DiscordColor.Magenta;
                IEnumerable<MemberEntity> memberBatch = memberBatches[page];
                foreach (MemberEntity member in memberBatch)
                {
                    string name = "<@" + JsonConvert.DeserializeObject<Member>(member.Member).UserID + ">";
                    PermissionFlag perm = new PermissionFlag((byte)member.Flag);
                    if (perm.BubbleFarming)
                    {
                        unranked++;
                        embed.AddField("\u200b", "**`🚩` - " + name + "**\n `This player has been flagged as *bubble farming*`.",true);
                    }
                    else embed.AddField("\u200b", "**#" + (members.IndexOf(member) + 1 - unranked).ToString() + " - " + name + "**" + (perm.BotAdmin ? " ` Admin` " : "") + (perm.Patron ? " ` 🎖️ Patron` " : "") + "\n🔮 " + BubbleWallet.GetBubbles(member.Login).ToString() + " Bubbles\n💧 " + BubbleWallet.GetDrops(member.Login).ToString() + " Drops", true);
                }
                embed.WithFooter(context.Member.DisplayName + " can react within 2 mins to show the next page.");
                await leaderboard.ModifyAsync(embed: embed.Build(), content: "");
                page++;
                if (page >= memberBatches.Count) { page = 0; unranked = 0; }
            }
            while (!(await interactivity.WaitForReactionAsync(reaction => reaction.Emoji == down, context.User, TimeSpan.FromMinutes(2))).TimedOut);
            try { await leaderboard.DeleteAllReactionsAsync(); }
            catch { }
            await leaderboard.CreateReactionAsync(DiscordEmoji.FromName(Program.Client, ":no_entry_sign:"));
        }

        [Description("See who's got the most bubbles.")]
        [Command("Leaderboard")]
        [Aliases("lbd", "ldb")]
        public async Task NewLeaderboard(CommandContext context, string mode = "bubbles")
        {
            Program.Feanor.ValidateGuildPalantir(context.Guild.Id.ToString());
            Program.Feanor.UpdateMemberGuilds();
            //DiscordMessage leaderboard = await context.RespondAsync("`⏱️` Loading members of `" + context.Guild.Name + "`...");
            var interactivity = context.Client.GetInteractivity();
            List<MemberEntity> members = Program.Feanor.GetGuildMembers(context.Guild.Id.ToString()).OrderByDescending(m => (mode == "drops" ? m.Drops : m.Bubbles)).Where(m => m.Bubbles > 0).ToList();
            List<IEnumerable<MemberEntity>> memberBatches = members.Batch(9).ToList();
            List<string> ranks = new List<string>();
            members.ForEach(member => {
                if (!(new PermissionFlag((byte)member.Flag)).BubbleFarming) ranks.Add(member.Login);
            });
            int page = 0;

            DiscordMessageBuilder leaderboard = new DiscordMessageBuilder();
            DiscordButtonComponent btnnext, btnprev;
            btnnext = new DiscordButtonComponent(ButtonStyle.Primary, "lbdnext", "Next Page");
            btnprev = new DiscordButtonComponent(ButtonStyle.Secondary, "lbdprev", "Previous Page");
            leaderboard.WithContent("`⏱️` Loading members of `" + context.Guild.Name + "`...").AddComponents(btnprev, btnnext);
            DiscordMessage msg = await leaderboard.SendAsync(context.Channel);

            InteractivityResult<DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs> press;
            do
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.Title = "🔮  Leaderboard of " + context.Guild.Name;
                embed.Color = DiscordColor.Magenta;
                IEnumerable<MemberEntity> memberBatch = memberBatches[page];
                foreach (MemberEntity member in memberBatch)
                {
                    string name = "<@" + JsonConvert.DeserializeObject<Member>(member.Member).UserID + ">";
                    PermissionFlag perm = new PermissionFlag((byte)member.Flag);
                    if (perm.BubbleFarming)
                    {
                        embed.AddField("\u200b", "**`🚩` - " + name + "**\n `This player has been flagged as *bubble farming*`.", true);
                    }
                    else embed.AddField("\u200b", "**#" + (ranks.IndexOf(member.Login) +1) + " - " + name + "**" + (perm.BotAdmin ? " \n`Admin` " : "") + (perm.Patron ? " \n`🎖️ Patron` " : "") + (perm.Patronizer ? " \n`🎁 Patronizer` " : "") + "\n🔮 " + BubbleWallet.GetBubbles(member.Login).ToString() + " Bubbles\n💧 " + BubbleWallet.GetDrops(member.Login).ToString() + " Drops", true);
                }
                embed.WithFooter(context.Member.DisplayName + " can react within 2 mins to show the next page.");

                leaderboard.Embed = embed.Build();
                leaderboard.Content = "";
                await leaderboard.ModifyAsync(msg);

                press = await interactivity.WaitForButtonAsync(msg, context.Message.Author, TimeSpan.FromMinutes(2));
                if (!press.TimedOut)
                {
                    if (press.Result.Id == "lbdprev") page--;
                    else if (press.Result.Id == "lbdnext") page++;
                    if (page >= memberBatches.Count) page = 0;
                    else if (page < 0) page = memberBatches.Count - 1;
                    await press.Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage);
                }
            }
            while (!press.TimedOut);

            btnnext.Disabled = true;
            btnprev.Disabled = true;
            await leaderboard.ModifyAsync(msg);
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
            QuartzJobs.BubbleTrace trace;
            if (mode == "week") 
            { 
                trace = new QuartzJobs.BubbleTrace(login, 7 * 10); 
                trace.History = trace.History.Where(
                    t => t.Key.DayOfWeek == DayOfWeek.Monday || t.Key == trace.History.Keys.Min() || t.Key == trace.History.Keys.Max()
                    ).ToDictionary(); 
                msg += " Weekly"; }
            else if (mode == "month") 
            { 
                trace = new QuartzJobs.BubbleTrace(login); 
                trace.History = trace.History.Where(
                     t => t.Key.Day == 1 || t.Key == trace.History.Keys.Min() || t.Key == trace.History.Keys.Max()
                     ).ToDictionary();
                msg += " Monthly"; 
            }
            else 
            { 
                trace = new QuartzJobs.BubbleTrace(login, 30); 
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
            if (!perm.Moderator && !perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Ts ts...", "This command is only available for higher beings.\n||Some call them Bot-Admins ;))||");
                return;
            }

            PalantirDbContext dbcontext = new PalantirDbContext(); 

            EventEntity newEvent = new EventEntity();
            newEvent.EventName = name.Replace("_"," ");
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
            if (!perm.Moderator && !perm.BotAdmin)
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
                List<SpritesEntity> eventsprites = new List<SpritesEntity>();
                List<EventDropEntity> eventdrops = Events.GetEventDrops(new List<EventEntity> { evt });
                eventdrops.ForEach(e =>
                {
                    List<SpritesEntity> sprites = Events.GetEventSprites(e.EventDropID);
                    sprites.OrderBy(sprite => sprite.ID).ForEach(sprite => eventsprites.Add(sprite));
                });
                eventsprites.OrderBy(sprite => sprite.ID).ForEach(sprite =>
                {
                    dropList += "➜ **" + sprite.Name + "** (#" + sprite.ID + ")\n" + BubbleWallet.GetEventCredit(login, sprite.EventDropID) + " / " + sprite.Cost + " " + eventdrops.FirstOrDefault(drop => drop.EventDropID == sprite.EventDropID).Name + " Drops " + (inv.Any(s => s.ID == sprite.ID) ? ":package:" : "") + "\n\n";
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
            List<Sprite> sprites = BubbleWallet.GetAvailableSprites();
            if(!sprites.Any(s=>s.ID == eventSpriteID && s.EventDropID != 0))
            {
                await Program.SendEmbed(context.Channel, "Hmmm...", "That sprite doesn't exist or is no event sprite.");
                return;
            }
            int eventDropID = sprites.FirstOrDefault(s => s.ID == eventSpriteID).EventDropID;
            string login = BubbleWallet.GetLoginOfMember(context.Message.Author.Id.ToString());
            int credit = BubbleWallet.GetRemainingEventDrops(login, eventDropID);
            int total = BubbleWallet.GetEventCredit(login, eventDropID);
            if (amount < 3 && total >= 3)
            {
                await Program.SendEmbed(context.Channel, "That's all you got?", "With more than 3 drops collected, the minimal gift amount is 3 event drops.");
                return;
            }
            List<SpriteProperty> inv = BubbleWallet.GetInventory(login);

            List<EventDropEntity> drops = Events.GetEventDrops();
            string name = drops.FirstOrDefault(d => d.EventDropID == eventDropID).Name;
            if (credit - amount < 0)
            {
                await Program.SendEmbed(context.Channel, "You can't trick me!", "Your event credit is too few. You have only " + credit + " " + name + " left.");
                return;
            }
            int lost = amount >= 3 ? (new Random()).Next(0, amount / 3 + 1) : (new Random()).Next(0, 2);
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
        public async Task CreateEventSprite(CommandContext context, [Description("The id of the event drop for the sprite")] int eventDropID, [Description("The name of the sprite")] string name, [Description("The event drop price")] int price, [Description("Any string except '-' if the sprite should replace the avatar")]string special = "", [Description("Any string except '-' to set the sprite artist")]string artist = "")
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.Moderator && !perm.BotAdmin)
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
            if (price < 5)
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
            client.DownloadFile(context.Message.Attachments[0].Url, "/home/pi/Webroot/eventsprites/evd" + eventDropID + name.Replace("'", "-") + ".gif");

            Sprite eventsprite = new Sprite(
                name.Replace("_"," "), 
                "https://tobeh.host/eventsprites/evd" + eventDropID + name.Replace("'", "-") + ".gif", 
                price, 
                dbcontext.Sprites.Where(s => s.ID < 1000).Max(s => s.ID) + 1, 
                special != "-" && special != "", 
                eventDropID, 
                artist == "" || artist == "-" ? null : artist 
            );
            BubbleWallet.AddSprite(eventsprite);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Sprite added to " + dbcontext.EventDrops.FirstOrDefault(e => e.EventDropID == eventDropID).Name + ": **" + eventsprite.Name + "**";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("ID: " + eventsprite.ID + "\nYou can buy and view the sprite with the usual comands.");
            embed.WithThumbnail(eventsprite.URL);

            dbcontext.Dispose();
            await context.Channel.SendMessageAsync(embed: embed);
        }

        [Description("Add a sprite")]
        [Command("addsprite")]
        public async Task AddSprite(CommandContext context, [Description("The name of the sprite")] string name, [Description("The bubble price")] int price, [Description("Any string except '-' if the sprite should replace the avatar")]string special = "", [Description("Any string except '-' to set the sprite artist")]string artist = "")
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.Moderator && !perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Ts ts...", "This command is only available for higher beings.\n||Some call them Bot-Admins ;))||");
                return;
            }

            PalantirDbContext dbcontext = new PalantirDbContext();
            if (context.Message.Attachments.Count <= 0 || !context.Message.Attachments[0].FileName.EndsWith(".gif"))
            {
                await Program.SendEmbed(context.Channel, "Hmm...", "There's no valid gif attached.");
                return;
            }
            if (price < 500)
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
            client.DownloadFile(context.Message.Attachments[0].Url, "/home/pi/Webroot/regsprites/spt" + name.Replace("'", "-") + ".gif");

            Sprite sprite = new Sprite(
                name.Replace("_", " "),
                "https://tobeh.host/regsprites/spt" + name.Replace("'", "-") + ".gif",
                price,
                dbcontext.Sprites.Where(s => s.ID < 1000).Max(s => s.ID) + 1,
                special != "-" && special != "",
                0,
                (artist == "" || artist == "-") ? null : artist
            );
            BubbleWallet.AddSprite(sprite);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            embed.Title = ":champagne:  Sprite **" + name + "** with ID " + sprite.ID + " was added!";
            embed.Color = DiscordColor.Magenta;
            embed.WithDescription("ID: " + sprite.ID + "\nYou can buy and view the sprite with the usual comands.");
            embed.WithThumbnail(sprite.URL);

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
                string getDesc = "Flag[0] Bubble Farming - "
                    + getperm.BubbleFarming + "\nFlag[1] Bot Admin - "
                    + getperm.BotAdmin + "\nFlag[2] Moderator - "
                    + getperm.Moderator + "\nFlag[3] Unlimited Cloud - "
                    + getperm.CloudUnlimited + "\nFlag[4] Patron - " 
                    + getperm.Patron + "\nFlag[5] Permanent Ban - "
                    + getperm.Permanban + "\nFlag[6] Drop Ban - "
                    + getperm.Dropban + "\nFlag[7] Patronizer - "
                    + getperm.Patronizer;
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
            string desc = "Flag[0] Bubble Farming - "
                    + newFlag.BubbleFarming + "\nFlag[1] Bot Admin - "
                    + newFlag.BotAdmin + "\nFlag[2] Moderator - "
                    + newFlag.Moderator + "\nFlag[3] Unlimited Cloud - "
                    + newFlag.CloudUnlimited + "\nFlag[4] Patron - "
                    + newFlag.Patron + "\nFlag[5] Permanent Ban - "
                    + newFlag.Permanban + "\nFlag[6] Drop Ban - "
                    + newFlag.Dropban + "\nFlag[7] Patronizer - "
                    + newFlag.Patronizer;
            await Program.SendEmbed(context.Channel, "*magic happened*","The flag of " + name + " was set to " + flag + "\n" + desc);
        }

        [Description("Reboots the bot & pulls from git.")]
        [Command("hardreboot")]
        public async Task Reboot(CommandContext context)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin && !perm.Moderator)
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

        [Description("Set your patron emoji")]
        [Command("patronemoji")]
        public async Task Patronemoji(CommandContext context, string emoji)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.Patron && !perm.BotAdmin)
            {
                await Program.SendEmbed(context.Channel, "Hey cutie!", "Youre no Patron yet... :smirk:\nJoin on https://www.patreon.com/skribbltypo");
                return;
            }
            PalantirDbContext db = new PalantirDbContext();
            string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
            string regexEmoji = "(?:0\x20E3|1\x20E3|2\x20E3|3\x20E3|4\x20E3|5\x20E3|6\x20E3|7\x20E3|8\x20E3|9\x20E3|#\x20E3|\\*\x20E3|\xD83C(?:\xDDE6\xD83C(?:\xDDE8|\xDDE9|\xDDEA|\xDDEB|\xDDEC|\xDDEE|\xDDF1|\xDDF2|\xDDF4|\xDDF6|\xDDF7|\xDDF8|\xDDF9|\xDDFA|\xDDFC|\xDDFD|\xDDFF)|\xDDE7\xD83C(?:\xDDE6|\xDDE7|\xDDE9|\xDDEA|\xDDEB|\xDDEC|\xDDED|\xDDEE|\xDDEF|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF6|\xDDF7|\xDDF8|\xDDF9|\xDDFB|\xDDFC|\xDDFE|\xDDFF)|\xDDE8\xD83C(?:\xDDE6|\xDDE8|\xDDE9|\xDDEB|\xDDEC|\xDDED|\xDDEE|\xDDF0|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF5|\xDDF7|\xDDFA|\xDDFB|\xDDFC|\xDDFD|\xDDFE|\xDDFF)|\xDDE9\xD83C(?:\xDDEA|\xDDEC|\xDDEF|\xDDF0|\xDDF2|\xDDF4|\xDDFF)|\xDDEA\xD83C(?:\xDDE6|\xDDE8|\xDDEA|\xDDEC|\xDDED|\xDDF7|\xDDF8|\xDDF9|\xDDFA)|\xDDEB\xD83C(?:\xDDEE|\xDDEF|\xDDF0|\xDDF2|\xDDF4|\xDDF7)|\xDDEC\xD83C(?:\xDDE6|\xDDE7|\xDDE9|\xDDEA|\xDDEB|\xDDEC|\xDDED|\xDDEE|\xDDF1|\xDDF2|\xDDF3|\xDDF5|\xDDF6|\xDDF7|\xDDF8|\xDDF9|\xDDFA|\xDDFC|\xDDFE)|\xDDED\xD83C(?:\xDDF0|\xDDF2|\xDDF3|\xDDF7|\xDDF9|\xDDFA)|\xDDEE\xD83C(?:\xDDE8|\xDDE9|\xDDEA|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF6|\xDDF7|\xDDF8|\xDDF9)|\xDDEF\xD83C(?:\xDDEA|\xDDF2|\xDDF4|\xDDF5)|\xDDF0\xD83C(?:\xDDEA|\xDDEC|\xDDED|\xDDEE|\xDDF2|\xDDF3|\xDDF5|\xDDF7|\xDDFC|\xDDFE|\xDDFF)|\xDDF1\xD83C(?:\xDDE6|\xDDE7|\xDDE8|\xDDEE|\xDDF0|\xDDF7|\xDDF8|\xDDF9|\xDDFA|\xDDFB|\xDDFE)|\xDDF2\xD83C(?:\xDDE6|\xDDE8|\xDDE9|\xDDEA|\xDDEB|\xDDEC|\xDDED|\xDDF0|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF5|\xDDF6|\xDDF7|\xDDF8|\xDDF9|\xDDFA|\xDDFB|\xDDFC|\xDDFD|\xDDFE|\xDDFF)|\xDDF3\xD83C(?:\xDDE6|\xDDE8|\xDDEA|\xDDEB|\xDDEC|\xDDEE|\xDDF1|\xDDF4|\xDDF5|\xDDF7|\xDDFA|\xDDFF)|\xDDF4\xD83C\xDDF2|\xDDF5\xD83C(?:\xDDE6|\xDDEA|\xDDEB|\xDDEC|\xDDED|\xDDF0|\xDDF1|\xDDF2|\xDDF3|\xDDF7|\xDDF8|\xDDF9|\xDDFC|\xDDFE)|\xDDF6\xD83C\xDDE6|\xDDF7\xD83C(?:\xDDEA|\xDDF4|\xDDF8|\xDDFA|\xDDFC)|\xDDF8\xD83C(?:\xDDE6|\xDDE7|\xDDE8|\xDDE9|\xDDEA|\xDDEC|\xDDED|\xDDEE|\xDDEF|\xDDF0|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF7|\xDDF8|\xDDF9|\xDDFB|\xDDFD|\xDDFE|\xDDFF)|\xDDF9\xD83C(?:\xDDE6|\xDDE8|\xDDE9|\xDDEB|\xDDEC|\xDDED|\xDDEF|\xDDF0|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF7|\xDDF9|\xDDFB|\xDDFC|\xDDFF)|\xDDFA\xD83C(?:\xDDE6|\xDDEC|\xDDF2|\xDDF8|\xDDFE|\xDDFF)|\xDDFB\xD83C(?:\xDDE6|\xDDE8|\xDDEA|\xDDEC|\xDDEE|\xDDF3|\xDDFA)|\xDDFC\xD83C(?:\xDDEB|\xDDF8)|\xDDFD\xD83C\xDDF0|\xDDFE\xD83C(?:\xDDEA|\xDDF9)|\xDDFF\xD83C(?:\xDDE6|\xDDF2|\xDDFC)))|[\xA9\xAE\x203C\x2049\x2122\x2139\x2194-\x2199\x21A9\x21AA\x231A\x231B\x2328\x23CF\x23E9-\x23F3\x23F8-\x23FA\x24C2\x25AA\x25AB\x25B6\x25C0\x25FB-\x25FE\x2600-\x2604\x260E\x2611\x2614\x2615\x2618\x261D\x2620\x2622\x2623\x2626\x262A\x262E\x262F\x2638-\x263A\x2648-\x2653\x2660\x2663\x2665\x2666\x2668\x267B\x267F\x2692-\x2694\x2696\x2697\x2699\x269B\x269C\x26A0\x26A1\x26AA\x26AB\x26B0\x26B1\x26BD\x26BE\x26C4\x26C5\x26C8\x26CE\x26CF\x26D1\x26D3\x26D4\x26E9\x26EA\x26F0-\x26F5\x26F7-\x26FA\x26FD\x2702\x2705\x2708-\x270D\x270F\x2712\x2714\x2716\x271D\x2721\x2728\x2733\x2734\x2744\x2747\x274C\x274E\x2753-\x2755\x2757\x2763\x2764\x2795-\x2797\x27A1\x27B0\x27BF\x2934\x2935\x2B05-\x2B07\x2B1B\x2B1C\x2B50\x2B55\x3030\x303D\x3297\x3299]|\xD83C[\xDC04\xDCCF\xDD70\xDD71\xDD7E\xDD7F\xDD8E\xDD91-\xDD9A\xDE01\xDE02\xDE1A\xDE2F\xDE32-\xDE3A\xDE50\xDE51\xDF00-\xDF21\xDF24-\xDF93\xDF96\xDF97\xDF99-\xDF9B\xDF9E-\xDFF0\xDFF3-\xDFF5\xDFF7-\xDFFF]|\xD83D[\xDC00-\xDCFD\xDCFF-\xDD3D\xDD49-\xDD4E\xDD50-\xDD67\xDD6F\xDD70\xDD73-\xDD79\xDD87\xDD8A-\xDD8D\xDD90\xDD95\xDD96\xDDA5\xDDA8\xDDB1\xDDB2\xDDBC\xDDC2-\xDDC4\xDDD1-\xDDD3\xDDDC-\xDDDE\xDDE1\xDDE3\xDDEF\xDDF3\xDDFA-\xDE4F\xDE80-\xDEC5\xDECB-\xDED0\xDEE0-\xDEE5\xDEE9\xDEEB\xDEEC\xDEF0\xDEF3]|\xD83E[\xDD10-\xDD18\xDD80-\xDD84\xDDC0]";
            Match emojimatch = Regex.Match(emoji, regexEmoji);
            db.Members.FirstOrDefault(member => member.Login == login).Emoji = emojimatch.Value;
            db.SaveChanges();
            db.Dispose();
            await Program.SendEmbed(context.Channel, "Emoji set to: `" + emojimatch.Value + "`", "Disable it with the same command without emoji.");
        }
        [Description("Gift patronage to a friend")]
        [Command("patronize")]
        public async Task Patronize(CommandContext context, string gift_id = "")
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.Patronizer)
            {
                await Program.SendEmbed(context.Channel, "Gifts are beautiful!", "Looking to get Palantir & Typo Patron perks, but however can't pay a Patreon patronge?\n\nAsk a friend with the `Patronizer Package` subscription on Patreon to patronize you!\nYour friend just has to use the command `>patronize " + context.User.Id + "`.");
            }
            else if (gift_id == "")
            {
                await Program.SendEmbed(context.Channel, "Oh, a patronizer! :o", "To gift patreon perks to a friend, use the command `>patronize id`, where id is the User-ID of your friend.\nYour friend can use `>patronize` to get their id!");
            }
            else if(gift_id == "none")
            {
                PalantirDbContext db = new PalantirDbContext();
                string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
                MemberEntity patronizer = db.Members.FirstOrDefault(member => member.Login == login);
                if (patronizer.Patronize is not null && DateTime.Now - DateTime.ParseExact(patronizer.Patronize.Split("#")[1], "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture) < TimeSpan.FromDays(5))
                    await Program.SendEmbed(context.Channel, "Sorry...", "You'll have to wait five days from the date of the gift (" + patronizer.Patronize.Split("#")[1] + ") to remove it!");
                else
                {
                    patronizer.Patronize = null;
                    await Program.SendEmbed(context.Channel, "Well, okay", "The gift was removed.\nMaybe choose someone else? <3");
                }
                db.SaveChanges();
                db.Dispose();
            }
            else
            {
                DiscordUser patronized = await Program.Client.GetUserAsync(Convert.ToUInt64(gift_id));
                if (patronized is null)
                    await Program.SendEmbed(context.Channel, "Sorry...", "I don't know this user ID :(\nYour friend has to use the `>patronize` command and tell you his ID!");
                else
                {
                    PalantirDbContext db = new PalantirDbContext();
                    string login = BubbleWallet.GetLoginOfMember(context.User.Id.ToString());
                    MemberEntity patronizer = db.Members.FirstOrDefault(member => member.Login == login);
                    if(patronizer.Patronize is not null && DateTime.Now - DateTime.ParseExact(patronizer.Patronize.Split("#")[1], "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture) < TimeSpan.FromDays(5))
                        await Program.SendEmbed(context.Channel, "Sorry...", "You'll have to wait five days from the date of the gift (" + patronizer.Patronize.Split("#")[1] + ") to change the receiver!");
                    else
                    {
                        patronizer.Patronize = gift_id + "#" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                        await Program.SendEmbed(context.Channel, "You're awesome!!", "You just gifted " + patronized.Username + " patron perks as long as you have the patronizer subscription!\nAfter a cooldown of five days, you can change the receiver with the same command or revoke it with `>patronize none`.");
                    }
                    db.SaveChanges();
                    db.Dispose();
                }
            }

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
        [Description("Show some nice information about the bot.")]
        [Command("about")]
        public async Task About(CommandContext context)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

            embed.Title = "About me";
            embed.AddField("`🤖` Hi!", "I'm Palantir - I integrate skribbl typo into Discord. \nMy main task is to sell sprites and show you information what's going on with typo.");
            embed.AddField("`👪` ", "Currently **" + Program.Feanor.PalantirTethers.Count + " ** servers are using me to show skribbl lobbies.");
            int avgMembers = 0;
            Program.Client.Guilds.ForEach(guild => avgMembers += guild.Value.MemberCount / Program.Client.Guilds.Count);
            embed.AddField("`🗄️` ", "Overall **" + Program.Client.Guilds.Count + " ** servers invited me to join.\nIn average, these servers have " + avgMembers + " members.");
            PalantirDbContext cont = new PalantirDbContext();
            int members = cont.Members.Count();
            cont.Dispose();
            embed.AddField("`👥` ", "**" +  members + " ** people have registered on Palantir.");
            embed.AddField("`❤️` ", "**" + Program.Feanor.PatronEmojis.Count + " ** Patrons are supporting Typo on Patreon.");
            await context.RespondAsync(embed: embed);
        }
        [Description("Creates a new theme ticket which can be used by anyone to add a new theme to typo.")]
        [Command("themeticket")]
        public async Task CreateThemeTicket(CommandContext context)
        {
            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin && !perm.Moderator)
            {
                await Program.SendEmbed(context.Channel, "Ts ts...", "This command is only available for Palantir Mods.");
                return;
            }

            TypoThemeEntity empty = new TypoThemeEntity();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const int length = 6;
            Random random = new Random();
            PalantirDbContext dbcontext = new PalantirDbContext();
            do
            {
                empty.Ticket = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            }
            while (dbcontext.Themes.Any(theme => theme.Ticket == empty.Ticket));
            dbcontext.Themes.Add(empty);
            dbcontext.SaveChanges();
            dbcontext.Dispose();
            string response = "Successfully created theme ticket!\nUse it with the command `>addtheme`.\nTicket: `" + empty.Ticket + "`";
            if (context.Channel.IsPrivate) await context.RespondAsync(response);
            else await context.Member.SendMessageAsync(response);
        }

        [Description("Adds a new theme. You'll need to have a valid theme ticket.")]
        [Command("addtheme")]
        public async Task AddTheme(CommandContext context)
        {
            var interactivity = Program.Client.GetInteractivity();
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within one minute with your theme ticket.\nThe ticket is a 6-digit code which Palantir moderators can generate.");
            InteractivityResult<DiscordMessage> msgTicket = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(1));
            TypoThemeEntity ticket = new TypoThemeEntity();
            ticket.Ticket = msgTicket.TimedOut ? "0" : msgTicket.Result.Content;
            PalantirDbContext dbcontext = new PalantirDbContext();
            ticket = dbcontext.Themes.FirstOrDefault(theme => theme.Ticket == ticket.Ticket && theme.Theme != " ");
            dbcontext.Dispose();
            if (ticket is null)
            {
                await Program.SendEmbed(context.Channel, "Invalid theme ticket :(","");
                return;
            }
            ticket.Author = context.User.Username;
            //get name
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with the theme Name.");
            InteractivityResult<DiscordMessage> msgName = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
            if (msgName.TimedOut) {
                await Program.SendEmbed(context.Channel, "Timed out :(", "");
                return;
            }
            ticket.Name = msgName.Result.Content;
            // get theme
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with the theme text.");
            InteractivityResult<DiscordMessage> msgTheme = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
            if (msgTheme.TimedOut)
            {
                await Program.SendEmbed(context.Channel, "Timed out :(", "");
                return;
            }
            ticket.Theme = msgTheme.Result.Content;
            // get description
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with the theme description.");
            InteractivityResult<DiscordMessage> msgDesc = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
            if (msgDesc.TimedOut)
            {
                await Program.SendEmbed(context.Channel, "Timed out :(", "");
                return;
            }
            ticket.Description = msgDesc.Result.Content;
            // get thumbnail landing
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with a screenshot from the skribbl landing page.\nRespond without attachment to skip.");
            InteractivityResult<DiscordMessage> msgLanding = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
            if (msgLanding.TimedOut)
            {
                await Program.SendEmbed(context.Channel, "Timed out :(", "");
                return;
            }
            if(msgLanding.Result.Attachments.Count > 0) ticket.ThumbnailLanding = msgLanding.Result.Attachments[0].Url;
            // get thumbnail game
            await Program.SendEmbed(context.Channel, "Add a theme", "Respond within five minutes with a screenshot from skribbl in-game.\nRespond without attachment to skip.");
            InteractivityResult<DiscordMessage> msgGame = await interactivity.WaitForMessageAsync(message => message.Author == context.User, TimeSpan.FromMinutes(5));
            if (msgGame.TimedOut)
            {
                await Program.SendEmbed(context.Channel, "Timed out :(", "");
                return;
            }
            if (msgGame.Result.Attachments.Count > 0) ticket.ThumbnailGame = msgGame.Result.Attachments[0].Url;

            dbcontext = new PalantirDbContext();
            dbcontext.Themes.Update(ticket);
            dbcontext.SaveChanges();
            dbcontext.Dispose();
            await Program.SendEmbed(context.Channel, "Theme successfully added!", "You can now use following link to instantly share your theme:");
            await context.RespondAsync("https://typo.rip/t?ticket=" + ticket.Ticket);
        }

        [Description("Gets a png of a sprite combo.")]
        [Command("combopng")]
        public async Task Combopng(CommandContext context, [Description("The id of the sprites (eg '15 0 16 17')")] params int[] sprites)
        {
            string path = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(sprites), "/home/pi/tmpGen/");
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                var msg = await new DiscordMessageBuilder()
                    .WithFiles(new Dictionary<string, System.IO.Stream>() { { "combo.png", fs } })
                    .SendAsync(context.Channel);
            }
        }

        [Description("Generates a card of your profile")]
        [Command("card")]
        public async Task Combopng(CommandContext context, string color = "black")
        {
            DiscordMember dMember = context.Member;
            DiscordUser dUser = context.User;
            if (context.Message.ReferencedMessage is not null) {
                dUser = context.Message.ReferencedMessage.Author;
                dMember = null;
            }

            PermissionFlag perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(context.User));
            if (!perm.BotAdmin && !perm.Patron)
            {
                await Program.SendEmbed(context.Channel, "Ha, PAYWALL!", "This command is only available for Patreon Subscriber.\nLet's join them! \nhttps://www.patreon.com/skribbltypo");
                return;
            }
            perm = new PermissionFlag((byte)Program.Feanor.GetFlagByMember(dUser));

            DiscordMessage response = await context.RespondAsync(">  \n>  \n>   <a:working:857610439588053023> **Building your card afap!!**\n> _ _ \n> _ _ ");
            string login = BubbleWallet.GetLoginOfMember(dUser.Id.ToString());
            MemberEntity member = Program.Feanor.GetMemberByLogin(login);
            Member memberDetail = JsonConvert.DeserializeObject<Member>(member.Member);

            //string url = context.Message.Attachments[0].Url;
            System.Net.WebClient client = new System.Net.WebClient();
            //string content = client.DownloadString(url);
            string content = Palantir.Properties.Resources.SVGcard;

            int[] sprites = BubbleWallet.GetInventory(login).Where(spt => spt.Activated).OrderBy(spt=>spt.Slot).Select(spt=>spt.ID).ToArray();

            string profilebase64 = Convert.ToBase64String(client.DownloadData(dUser.AvatarUrl));
            string combopath = SpriteComboImage.GenerateImage(SpriteComboImage.GetSpriteSources(sprites),"/home/pi/tmpGen/");
            string spritebase64 = Convert.ToBase64String(System.IO.File.ReadAllBytes(combopath));
            System.IO.File.Delete(combopath);

            SpriteComboImage.FillPlaceholders(ref content, profilebase64, spritebase64, color, dMember is not null ? dMember.DisplayName : dUser.Username, member.Bubbles.ToString(), member.Drops.ToString(), Math.Round((double)member.Drops / (member.Bubbles / 1000),1),
                BubbleWallet.FirstTrace(login), BubbleWallet.GetInventory(login).Count.ToString(), BubbleWallet.ParticipatedEvents(login).Count.ToString(), Math.Round((double)member.Bubbles * 10 / 3600).ToString(),
                BubbleWallet.GlobalRanking(login).ToString(), BubbleWallet.GlobalRanking(login, true).ToString(), memberDetail.Guilds.Count.ToString(), perm.Patron, BubbleWallet.IsEarlyUser(login), perm.Moderator);

            string path = SpriteComboImage.SVGtoPNG(content, "/home/pi/Webroot/files/combos/");
            await response.ModifyAsync(content: path.Replace(@"/home/pi/Webroot/", "https://tobeh.host/"));
            
        }

    }
}
