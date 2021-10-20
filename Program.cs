using System;
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

namespace Palantir
{
    class Program
    {
        public static DataManager Feanor;
        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        public static InteractivityExtension Interactivity;
        static async Task Main(string[] args)
        {
            //File.WriteAllText("/home/pi/palantirOutput.log", String.Empty);
            Console.WriteLine("Huh, it's " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " - lemme sleep!!\n");
            Console.WriteLine("Initializing Palantir:\n...");
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = File.ReadAllText("/home/pi/palantirToken.txt"),
                TokenType = TokenType.Bot
            });
            Commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { ">" },
                DmHelp = false,
                IgnoreExtraArguments = true,
                CaseSensitive = false
            });
            //Program.Client.UpdateCurrentUserAsync(avatar:)
            Console.WriteLine("Creating interactivity\n...");
            Interactivity = Client.UseInteractivity();
            Console.WriteLine("Adding handlers\n...");
            Client.GuildCreated += onjoin;
            Commands.CommandErrored += onCommandErrored;
            Console.WriteLine("Registering commands\n...");
            Commands.RegisterCommands<Commands>();
            Console.Write("Connecting Client...");
            await Client.ConnectAsync();
            Console.WriteLine("Initializig Connections...");
            Feanor = new DataManager();
            Console.WriteLine("Palantir ready. Do not uncover it.");
            Console.WriteLine("Stored guilds:");
            Feanor.PalantirTethers.ForEach((t) => { Console.WriteLine("- " + t.PalantirEndpoint.GuildID + " / " + t.PalantirEndpoint.GuildName); });
            //Console.WriteLine("Stored members:");
            //Feanor.PalantirMembers.ForEach((m) => { Console.WriteLine("- " + m.UserName); });
            Feanor.ActivatePalantiri();
            Console.WriteLine("Palantir activated. Fool of a Took!");

            // Initialize quartz jobs
            Console.WriteLine("Initializing jobs\n...");
            ISchedulerFactory schedFact = new StdSchedulerFactory();
            IScheduler scheduler = await schedFact.GetScheduler();
            await scheduler.Start();
            IJobDetail tracer = JobBuilder.Create<QuartzJobs.TracerJob>()
                .WithIdentity("Bubble Tracer")
                .Build();
            ITrigger tracerTrigger = TriggerBuilder.Create()
                .StartNow()
                .WithDailyTimeIntervalSchedule
                (t => t
                    .WithIntervalInHours(24)
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(23, 59))
                )
                .Build();
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
            IJobDetail bubbleCounter = JobBuilder.Create<BubbleCounter>()
                .WithIdentity("Bubble Counter")
                .Build();
            ITrigger bubbleTrigger = TriggerBuilder.Create()
                .StartNow()
                .WithDailyTimeIntervalSchedule
                (t => t
                    .WithIntervalInSeconds(10)
                )
                .Build();

            //Start bubble tracer job
            Console.WriteLine("Starting bubbletracer job\n...");
            await scheduler.ScheduleJob(tracer, tracerTrigger);

            // start status updating
            Console.WriteLine("Starting status updater job\n...");
            await scheduler.ScheduleJob(statusUpdater, statusTrigger);

            // start picture updating and immediately set image
            Console.WriteLine("Starting picture updater job\n...");
            await scheduler.ScheduleJob(pictureUpdater, pictureTrigger);
            await RefreshPicture();

            // start bubble counting
            Console.WriteLine("Starting bubble counter job\n...");
            await scheduler.ScheduleJob(bubbleCounter, bubbleTrigger);

            Drops.StartDropping();
            Console.WriteLine("Started dropping cool stuff!");

            Console.WriteLine("All done!");
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
                foreach(KeyValuePair<ulong, DiscordChannel> p in e.Guild.Channels)
                {
                    try {
                        await e.Guild.GetChannel(p.Key).SendMessageAsync("Hello there! <a:l33:721872925531308032>\nMy prefix is `>`.\nCheck out `>manual` or `>help`.\nhttps://gph.is/2s4rv0N");
                        return;
                    }
                    catch { }
                }
            }
        }

        private static async Task onCommandErrored(CommandsNextExtension commands, CommandErrorEventArgs e)
        {
            if (e.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException) return;
            if (e.Exception is DSharpPlus.CommandsNext.Exceptions.ChecksFailedException) {
                string checks = "";
                //((DSharpPlus.CommandsNext.Exceptions.ChecksFailedException)e.Exception).FailedChecks.ToList().ForEach(check => check.)
                e.Command.ExecutionChecks.ToList().ForEach(attr => checks += attr.GetType());
                await SendEmbed(e.Context.Channel, e.Command.Name + ": Not allowed", "Some commands require a specific role or being executed in a DM channel. \n\nThis command needs:\n" + checks, "", DiscordColor.Red.Value);
                return;
            }
            if (e.Exception.ToString().Contains("Could not find a suitable overload for the command"))
            {
                await SendEmbed(e.Context.Channel, e.Command.Name + ": Invalid arguments", "The given arguments for the command did not fit.\nCheck `>help " + e.Command.Name + "` to see the correct use!","",DiscordColor.Red.Value);
                return;
            }

            DiscordEmbedBuilder embedErr = new DiscordEmbedBuilder();
            embedErr.Title = "Error Executing " + e.Command;
            embedErr.Description = e.Exception.ToString();
            embedErr.Color = DiscordColor.Red;
            embedErr.WithFooter("If this error is persistent, message @tobeh#7437.");
            await e.Context.Channel.SendMessageAsync(embed: embedErr);
            return;
        }

        public static async Task SendEmbed(DiscordChannel channel, string title, string description, string footer="", int color = -1)
        {
            DiscordEmbedBuilder embedErr = new DiscordEmbedBuilder();
            embedErr.Title = title;
            embedErr.Description = description.Length > 2040 ? description.Substring(0,2040) : description;
            embedErr.Color = color >= 0 ? new DiscordColor(color) : DiscordColor.Magenta;
            if (footer != "") embedErr.WithFooter(footer);
            await channel.SendMessageAsync(embed: embedErr);
            return;
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
            catch(Exception e) { Console.WriteLine(e.ToString()); }
            
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

    }

    public class PermissionFlag
    {
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
        public PermissionFlag(byte flag)
        {
            BitArray flags = new BitArray(new byte[] { flag });
            BubbleFarming = flags[0];
            BotAdmin = flags[1];
            Moderator = flags[2];
            CloudUnlimited = flags[3];
            Patron = flags[4];
            Permanban = flags[5];
            Dropban = flags[6];
            Patronizer = flags[7];
        }

        public int CalculateFlag()
        {
            return (BubbleFarming ? 1 : 0)
                + (BotAdmin ? 2 : 0)
                + (Moderator ? 4 : 0)
                + (CloudUnlimited ? 8 : 0)
                + (Patron ? 16 : 0)
                +(Permanban ? 32 : 0)
                + (Dropban ? 64 : 0)
                +(Patronizer ? 128 : 0);
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
    
}
