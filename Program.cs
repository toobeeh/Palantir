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

namespace Palantir
{
    class Program
    {
        public static DataManager Feanor;
        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        static InteractivityExtension Interactivity;
        static async Task Main(string[] args)
        {
            Console.WriteLine("Huh, it's " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + " - lemme sleep!!\n");
            Console.WriteLine("Initializing Palantir\n...");

            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = File.ReadAllText("/home/pi/palantirToken.txt"),
                TokenType = TokenType.Bot
            });
            Commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { ">" },
                DmHelp = false,
                IgnoreExtraArguments = true
            });
            Interactivity = Client.UseInteractivity(new InteractivityConfiguration { 
            });
            Client.GuildCreated += onjoin;
            Commands.CommandErrored += onCommandErrored;
            Commands.RegisterCommands<Commands>();
            await Client.ConnectAsync();
            Feanor = new DataManager();

            Console.WriteLine("Palantir ready. Do not uncover it.");
            Console.WriteLine("Stored guilds:");
            Feanor.PalantirTethers.ForEach((t) => { Console.WriteLine("- " + t.PalantirEndpoint.GuildID + " / " + t.PalantirEndpoint.GuildName); });
            Console.WriteLine("Stored members:");
            Feanor.PalantirMembers.ForEach((m) => { Console.WriteLine("- " + m.UserName); });
            Feanor.ActivatePalantiri();
            Console.WriteLine("Palantir activated. Fool of a Took!");

            // Initialize bubble tracer job
            Console.WriteLine("Initializing bubbletracer job\n...");
            ISchedulerFactory schedFact = new StdSchedulerFactory();
            IScheduler scheduler = await schedFact.GetScheduler();
            await scheduler.Start();
            IJobDetail tracer = JobBuilder.Create<Tracer.TracerJob>()
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
            IJobDetail statusUpdater = JobBuilder.Create<Tracer.UpdaterJob>()
                .WithIdentity("Status Updater")
                .Build();
            ITrigger statusTrigger = TriggerBuilder.Create()
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

            Drops.StartDropping();
            Console.WriteLine("Started dropping cool stuff!");

            Console.WriteLine("All done!");
            await Task.Delay(-1);
        }

        private static async Task onjoin(GuildCreateEventArgs e)
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

        private static async Task onCommandErrored(CommandErrorEventArgs e)
        {
            if (e.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException) return;
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

    }

    public class PermissionFlag
    {
        // Flag schema:
        // R... Restart - A... Admin - F... Farming
        // F =  R A F
        // 0 =  0 0 0
        // 1 =  0 0 1
        // 2 =  0 1 0
        // 4 =  1 0 0
        // ...

        public bool BotAdmin { get; set; }
        public bool BubbleFarming { get; set; }
        public bool RestartAndUpdate { get; set; }
        public PermissionFlag(byte flag)
        {
            BitArray flags = new BitArray(new byte[] { flag });
            BubbleFarming = flags[0];
            BotAdmin = flags[1];
            RestartAndUpdate = flags[2];
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
