﻿using System;
using System.Collections;
using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Quartz;
using Quartz.Impl;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System.Globalization;
using Palantir.PalantirCommandModule;
using Palantir.Model;
using DSharpPlus.Exceptions;

namespace Palantir
{
    class Program
    {
        public static DataManager Feanor;
        public static DiscordGuild TypoTestground;
        public static DiscordClient Client { get; private set; }
        public static CommandLock CommandLock { get; private set; }
        public static DiscordClient Servant { get; private set; }
        public static S3Handler S3 { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        public static SlashCommandsExtension Slash { get; private set; }
        public static InteractivityExtension Interactivity;
        public static readonly string PalantirToken = Environment.GetEnvironmentVariable("PALANTIR_TOKEN");
        public static readonly string? BabyPalantirToken = Environment.GetEnvironmentVariable("BABY_PALANTIR_TOKEN");
        public static readonly string ServantToken = Environment.GetEnvironmentVariable("SERVANT_TOKEN");
        public static string DatabaseHost = Environment.GetEnvironmentVariable("DB_HOST");
        public static readonly string DatabaseUser = Environment.GetEnvironmentVariable("DB_USER");
        public static readonly string StaticDataPath = Environment.GetEnvironmentVariable("STATIC_DATA_PATH");
        public static readonly string CacheDataPath = Environment.GetEnvironmentVariable("CACHE_DATA_PATH");
        public static readonly string S3AccessKey = Environment.GetEnvironmentVariable("S3_ACCESS_KEY");
        public static readonly string S3SecretKey = Environment.GetEnvironmentVariable("S3_SECRET_KEY");
        public static readonly string GithubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        public static readonly string DataRepoUrl = Environment.GetEnvironmentVariable("DATA_REPO_URL");

        static async Task Main(string[] args)
        {
            CultureInfo culture = new CultureInfo("de-AT");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Directory.CreateDirectory(Path.Combine(Program.CacheDataPath, "sprite-sources"));
            Directory.CreateDirectory(Path.Combine(Program.CacheDataPath, "scene-sources"));

            //Console.WriteLine($"Initialized with:\n- Palantir Token: {PalantirToken}\n- Servant Token: {ServantToken}\n- Database Host: {DatabaseHost}\n- Database User: {DatabaseUser}\n- Static Data Path: {StaticDataPath}\n- Cache Data Path: {CacheDataPath}\n- S3 Access Key: {S3AccessKey}\n- S3 Secret Key: {S3SecretKey}\n");

            S3 = new S3Handler();
            
            Console.WriteLine(BabyPalantirToken);
            if (!String.IsNullOrWhiteSpace(BabyPalantirToken)) await RunBabyPalantir();

            Console.WriteLine("Huh, it's " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " - let me sleep!\n");
            Console.WriteLine("Initializing servant bot client...\n");
            Servant = new DiscordClient(new DiscordConfiguration
            {
                Token = Program.ServantToken,
                TokenType = TokenType.Bot
            });
            await Servant.ConnectAsync();

            Console.WriteLine("Initializing Palantir:\n...");
            CommandLock = new CommandLock();
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = Program.PalantirToken,
                TokenType = TokenType.Bot,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information
            });
            Commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { ">" },
                DmHelp = false,
                IgnoreExtraArguments = true,
                CaseSensitive = false
            });
            Slash = Client.UseSlashCommands();

            Console.WriteLine("Creating interactivity\n...");
            Interactivity = Client.UseInteractivity();

            Console.WriteLine("Adding handlers\n...");
            Client.GuildCreated += onjoin;
            Commands.CommandErrored += OnCommandErrored;


            Console.WriteLine("Registering commands\n...");
            Commands.RegisterCommands<Palantir.Commands.MiscCommands>();
            Commands.RegisterCommands<Palantir.Commands.EventCommands>();
            Commands.RegisterCommands<Palantir.Commands.ManagementCommands>();
            Commands.RegisterCommands<Palantir.Commands.SpriteCommands>();

            //var commandsServant = Servant.UseCommandsNext(new CommandsNextConfiguration
            //{
            //    StringPrefixes = new string[] { "rip~" },
            //    DmHelp = false,
            //    IgnoreExtraArguments = true,
            //    CaseSensitive = false
            //});
            //commandsServant.RegisterCommands<Palantir.Commands.ManagementCommands>();

            Console.Write("Connecting Client...");
            await Client.ConnectAsync();

            Console.WriteLine("Initializig Connections...");
            Feanor = new DataManager();

            Console.WriteLine("Palantir ready. Do not uncover it.");

            Console.WriteLine("Stored guilds:");
            Feanor.PalantirTethers.ForEach((t) => { Console.WriteLine("- " + t.PalantirEndpoint.GuildID + " / " + t.PalantirEndpoint.GuildName); });
            Feanor.ActivatePalantiri();
            Console.WriteLine("Palantir activated. Fool of a Took!");

            // Initialize quartz jobs
            Console.WriteLine("Initializing jobs\n...");
            ISchedulerFactory schedFact = new StdSchedulerFactory();
            IScheduler scheduler = await schedFact.GetScheduler();
            await scheduler.Start();
            /*IJobDetail tracer = JobBuilder.Create<QuartzJobs.TracerJob>()
                .WithIdentity("Bubble Tracer")
                .Build();
            ITrigger tracerTrigger = TriggerBuilder.Create()
                .StartNow()
                .WithDailyTimeIntervalSchedule
                (t => t
                    .WithIntervalInHours(24)
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(23, 59))
                )
                .Build();*/
            IJobDetail statusUpdater = JobBuilder.Create<QuartzJobs.StatusUpdaterJob>()
                .WithIdentity("Status Updater")
                .Build();
            ITrigger statusTrigger = TriggerBuilder.Create()
                .StartNow()
                .WithDailyTimeIntervalSchedule
                (t => t
                    .WithIntervalInSeconds(10)
                )
                .Build();
            IJobDetail pictureUpdater = JobBuilder.Create<QuartzJobs.PictureUpdaterJob>()
                .WithIdentity("Picture Updater")
                .Build();
            ITrigger pictureTrigger = TriggerBuilder.Create()
                .StartNow().WithCronSchedule("0 0 8,20 ? * * *")
                .Build();
            /*IJobDetail bubbleCounter = JobBuilder.Create<BubbleCounter>()
                .WithIdentity("Bubble Counter")
                .Build();*/
            /*ITrigger bubbleTrigger = TriggerBuilder.Create()
                .StartNow()
                .WithDailyTimeIntervalSchedule
                (t => t
                    .WithIntervalInSeconds(10)
                )
                .Build();*/

            //Start bubble tracer job
            /*Console.WriteLine("Starting bubbletracer job\n...");
            await scheduler.ScheduleJob(tracer, tracerTrigger);*/

            // start status updating
            Console.WriteLine("Starting status updater job\n...");
            await scheduler.ScheduleJob(statusUpdater, statusTrigger);

            // start picture updating and immediately set image
            Console.WriteLine("Starting picture updater job\n...");
            await scheduler.ScheduleJob(pictureUpdater, pictureTrigger);
            await RefreshPicture();

            // start bubble counting
            /*Console.WriteLine("Starting bubble counter job\n...");
            await scheduler.ScheduleJob(bubbleCounter, bubbleTrigger);*/

            //Drops.StartDropping();
            Console.WriteLine("Started dropping cool stuff!");

            TypoTestground = await Client.GetGuildAsync(779435254225698827);

            Console.WriteLine("All done!");

            await Task.Delay(-1);
        }

        public static async Task SendNewPalantirInformation(CommandContext context, string newCommand, bool terminate = false)
        {
            var embed = new DiscordEmbedBuilder()
                .WithAuthor("Palantir is getting old... 😴")
                .WithTitle("`⚠️` Outdated command")
                .WithDescription("Good news! Palantir is getting an upgrade - the new version is already available!\n" +
                                 (terminate
                                     ? "This command cannot be used anymore."
                                     : "This command will work for a few more days.\n" +
                                       "In the meantime, make sure to [click here & add the new bot to your server!](https://discord.com/oauth2/authorize?client_id=1071142417987813376&scope=bot&permissions=2147747840)\n") +
                                 "If you have any questions about the new bot, [join the Typo Discord server.](https://discord.com/invite/pAapmUmWAM)");

            embed.AddField("New Bot Command",
                "You can use the following command in the new bot - it is also available as slash command:\n" +
                $"`{newCommand}`");

            embed.Color = DiscordColor.IndianRed;
            await context.RespondAsync(embed);

            if (terminate) throw new Exception("Command is not available for use anymore.");
        }

        private static async Task RunBabyPalantir()
        {
            var client = new DiscordClient(new DiscordConfiguration
            {
                Token = BabyPalantirToken,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All
            });
            await client.ConnectAsync();
            Interactivity = client.UseInteractivity();
            CommandLock = new CommandLock();
            Commands = client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { "admincmd" },
                DmHelp = false,
                IgnoreExtraArguments = true,
                CaseSensitive = false,
            });
            Commands.CommandErrored += OnCommandErrored;
            //TypoTestground = await client.GetGuildAsync(779435254225698827);
            Feanor = new();
            
            Commands.RegisterCommands<Palantir.Commands.MiscCommands>();
            Commands.RegisterCommands<Palantir.Commands.EventCommands>();
            Commands.RegisterCommands<Palantir.Commands.ManagementCommands>();
            Commands.RegisterCommands<Palantir.Commands.SpriteCommands>();
            await Task.Delay(-1);
        }

        private static async Task onjoin(DiscordClient client, GuildCreateEventArgs e)
        {
            try
            {
                await e.Guild.SystemChannel.SendMessageAsync("Hello there! <a:l33:721872925531308032>\nMy prefix is `>`.\nCheck out `>manual` or `>help`.\nhttps://gph.is/2s4rv0N");
            }
            catch
            {
                foreach (KeyValuePair<ulong, DiscordChannel> p in e.Guild.Channels)
                {
                    try
                    {
                        await e.Guild.GetChannel(p.Key).SendMessageAsync("Hello there! <a:l33:721872925531308032>\nMy prefix is `>`.\nCheck out `>manual` or `>help`.\nhttps://gph.is/2s4rv0N");
                        return;
                    }
                    catch { }
                }
            }
        }

        private static async Task OnCommandErrored(CommandsNextExtension commands, CommandErrorEventArgs e)
        {
            if (e.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException) return;

            if (e.Command.Module.ModuleType.IsSubclassOf(typeof(PalantirCommandModule.PalantirCommandModule)))
            {
                Program.CommandLock.UnlockCommand(e.Context);
            }

            if (e.Exception is DSharpPlus.Exceptions.BadRequestException)
            {
                string msg = ((BadRequestException)e.Exception).Errors;
                LogError(msg, e.Exception);
            }

            if (e.Exception is DSharpPlus.CommandsNext.Exceptions.ChecksFailedException)
            {
                string checks = "";
                e.Command.ExecutionChecks.ToList().ForEach(attr =>
                {
                    if (!(attr.GetType() == typeof(RequireBeta) || attr.GetType() == typeof(RequirePermissionFlag))) checks += attr.GetType();
                });
                if (checks != "") await SendEmbed(e.Context.Channel, e.Command.Name + ": Not allowed", "Some commands require a specific role or being executed in a DM channel. \n\nThis command needs:\n" + checks, "", DiscordColor.Red.Value);
                return;
            }
            if (e.Exception.ToString().Contains("Could not find a suitable overload for the command"))
            {
                await SendEmbed(e.Context.Channel, e.Command.Name + ": Invalid arguments", "The given arguments for the command did not fit.\nCheck `>help " + e.Command.Name + "` to see the correct use!", "", DiscordColor.Red.Value);
                return;
            }

            Console.WriteLine(e.Exception.ToString());
            DiscordEmbedBuilder embedErr = new DiscordEmbedBuilder();
            embedErr.Title = "Error Executing " + e.Command;
            embedErr.Description = e.Exception.ToString();
            embedErr.Color = DiscordColor.Red;
            embedErr.WithFooter("If this error is persistent, message @tobeh");
            await e.Context.Channel.SendMessageAsync(embed: embedErr);
            return;
        }

        public static async Task SendEmbed(DiscordChannel channel, string title, string description, string footer = "", int color = -1)
        {
            DiscordEmbedBuilder embedErr = new DiscordEmbedBuilder();
            embedErr.Title = title;
            embedErr.Description = description.Length > 2040 ? description.Substring(0, 2040) : description;
            embedErr.Color = color >= 0 ? new DiscordColor(color) : DiscordColor.Magenta;
            if (footer != "") embedErr.WithFooter(footer);
            await channel.SendMessageAsync(embed: embedErr);
            return;
        }

        public static DiscordEmbed PalantirEmbed(string title, string message)
        {
            return new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Magenta)
                .WithTitle(title)
                .WithDescription(message);
        }

        public static void LogError(string message, Exception e)
        {
            int line = new StackTrace(e, true).GetFrame(0).GetFileLineNumber();
            Client.Logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, -1, message + " @" + line, e, (state, e) => state + e.ToString());
        }

        public static async Task RefreshPicture()
        {
            try
            {
                string avatar;
                if (DateTime.Now.Hour is < 8 or > 20) avatar = "https://cdn.discordapp.com/attachments/334696834322661376/857269025100136468/tucpbptr.png";
                else avatar = "https://cdn.discordapp.com/attachments/334696834322661376/857269026841690123/skypbptr.png";
                System.Net.WebClient client = new System.Net.WebClient();
                byte[] data = client.DownloadData(avatar);
                MemoryStream str = new MemoryStream(data);
                await Program.Client.UpdateCurrentUserAsync(avatar: str);
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }

        }

        public static List<string> StringToGlyphs(string input)
        {
            List<string> glyphs = new List<string>();
            for (int i = 0; i < input.Length; i++)
            {
                string glyph = Convert.ToChar((int)input[i]).ToString();
                if (i + 1 < input.Length && (int)input[i] is > 55295 and < 57344) glyph += Convert.ToChar((int)input[++i]);
                glyphs.Add(glyph);
            }
            return glyphs;
        }

        public static List<List<int>> SplitCodepointsToEmojis(List<List<int>> glyphs)
        {
            List<List<int>> emojis = new List<List<int>>();
            for (int i = 0; i < glyphs.Count; i++)
            {
                List<int> codepoints = glyphs[i]; // codepoints of next glyph
                if (i == 0) // if no emojis yet, MUST be a new emoji
                {
                    emojis.Add(codepoints);
                }
                else
                {
                    if ((codepoints.Count == 1 && codepoints[0] == 8205) // if glyph is ZWJ or Skin Tone Mod, add to last emoji
                        || (codepoints.Count == 2 && codepoints[0] == 55356 && codepoints[1] is >= 57339 and <= 57344))
                        emojis.Last().AddRange(codepoints);
                    else
                    {
                        List<int> prevglyph = glyphs[i - 1];
                        if ((prevglyph.Count == 1 && prevglyph[0] == 8205) // if last emoji has ZWJ or STM as last glyph, add to last emoji
                            || (prevglyph.Count == 2 && prevglyph[0] == 55356 && prevglyph[1] is >= 57339 and <= 57344))
                            emojis.Last().AddRange(codepoints);
                        else emojis.Add(codepoints); // else it's a new emoji
                    }
                }
            }
            return emojis;
        }

        public static DateTime ParseDateAsUtc(string date)
        {
            return DateTime.SpecifyKind(Convert.ToDateTime(date), DateTimeKind.Utc);
        }

        public static string DateTimeToStamp(DateTime date, string format)
        {
            return $"<t:{new DateTimeOffset(date).ToUnixTimeSeconds()}:{format}>";
        }

        public static string DateTimeToStamp(string date, string format)
        {
            return DateTimeToStamp(ParseDateAsUtc(date), format);
        }

    }

    public class PermissionFlag
    {

        public const byte BUBBLEFARM = 1, ADMIN = 2, MOD = 4, CLOUD = 8, PATRON = 16, PERMABAN = 32, DROPBAN = 64, PATRONIZER = 128;
        // Flag schema:
        // M... Mod - A... Admin - F... Farming - T... Full Typo Cloud Access
        // F =  P T M A F
        // 0 =  0 0 0 0 0
        // 1 =  0 0 0 0 1
        // 2 =  0 0 0 1 0
        // 4 =  0 0 1 0 0
        // 8 =  0 1 0 0 0
        // 16 =  1 0 0 0 0

        public bool BotAdmin { get; set; }
        public bool BubbleFarming { get; set; }
        public bool Moderator { get; set; }
        public bool CloudUnlimited { get; set; }
        public bool Patron { get; set; }
        public bool Permanban { get; set; }
        public bool Dropban { get; set; }
        public bool Patronizer { get; set; }
        public bool Booster { get; set; }
        public bool Beta { get; set; }
        public PermissionFlag(Int16 flag)
        {
            var flags = ConvertInt16ToBitArray(flag);
            BubbleFarming = flags[0];
            BotAdmin = flags[1];
            Moderator = flags[2];
            CloudUnlimited = flags[3];
            Patron = flags[4];
            Permanban = flags[5];
            Dropban = flags[6];
            Patronizer = flags[7];
            Booster = flags[8];
            Beta = flags[9];
        }
        private bool[] ConvertInt16ToBitArray(Int16 value)
        {
            bool[] bitArray = new bool[16];

            for (int i = 0; i < 16; i++)
            {
                bitArray[i] = (value & (1 << i)) != 0;
            }

            return bitArray;
        }

        public int CalculateFlag()
        {
            return (BubbleFarming ? 1 : 0)
                + (BotAdmin ? 2 : 0)
                + (Moderator ? 4 : 0)
                + (CloudUnlimited ? 8 : 0)
                + (Patron ? 16 : 0)
                + (Permanban ? 32 : 0)
                + (Dropban ? 64 : 0)
                + (Patronizer ? 128 : 0)
                + (Booster ? 256 : 0)
                + (Beta ? 512 : 0);
        }

        public bool CheckForPermissionByte(byte permission)
        {
            permission = PermissionFlag.ADMIN;
            BitArray presentFlags = new BitArray(new byte[] { (byte)CalculateFlag() });
            BitArray checkFlags = new BitArray(new byte[] { permission });
            bool allMatch = true;
            for (int flagIndex = 0; flagIndex < checkFlags.Length; flagIndex++)
            {
                if (checkFlags[flagIndex] && (presentFlags.Length - 1 < flagIndex || !presentFlags[flagIndex])) allMatch = false;
            }
            return allMatch;
        }
    }

    // credits: https://loune.net/2017/06/running-shell-bash-commands-in-net-core/
    public static class ShellHelper
    {
        public static string Bash(this string cmd)
        {
            var escapedArgs = cmd;//.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }

    public sealed class RequireBeta : DSharpPlus.CommandsNext.Attributes.CheckBaseAttribute
    {
        public RequireBeta()
        {
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return new RequirePermissionFlag(PermissionFlag.ADMIN).ExecuteCheckAsync(ctx, help);
        }
    }
    public sealed class RequirePermissionFlag : DSharpPlus.CommandsNext.Attributes.CheckBaseAttribute
    {
        private byte FlagToCheck;
        public RequirePermissionFlag(byte flagToCheck)
        {
            FlagToCheck = flagToCheck;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            PermissionFlag userFlag = new PermissionFlag(Program.Feanor.GetFlagByMemberId(ctx.User.Id.ToString()));
            PermissionFlag requiredFlag = new PermissionFlag(FlagToCheck);
            bool result = userFlag.BotAdmin || userFlag.CheckForPermissionByte(FlagToCheck);
            if (!result && !help)
            {
                // Send responses if permissions dont match
                if (requiredFlag.Patron && !userFlag.Patron)
                    Program.SendEmbed(ctx.Channel, "HA, Paywall!!", "This command is only available for Typo Patrons :/\nhttps://patreon.com/skribbltypo").GetAwaiter();
                else if (requiredFlag.BotAdmin && !userFlag.BotAdmin)
                    Program.SendEmbed(ctx.Channel, "Hands off there :o", "Only Bot Admins may use this command.").GetAwaiter();
                else if (requiredFlag.Moderator && !userFlag.Moderator)
                    Program.SendEmbed(ctx.Channel, "Ts ts..", "This command is only available for Bot Moderators.").GetAwaiter();
            }
            return Task.FromResult(result);
        }
    }

}
