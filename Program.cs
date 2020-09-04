using System;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
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
            Client.GuildCreated += onjoin;
            Commands.RegisterCommands<Commands>();
            await Client.ConnectAsync(new DiscordActivity(" u on skribbl.io",ActivityType.Watching));
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

            //Start bubble tracer job
            Console.WriteLine("Starting bubbletracer job\n...");
            await scheduler.ScheduleJob(tracer, tracerTrigger);


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

        public static async Task SendEmbed(DiscordChannel channel, string title, string description, string footer="")
        {
            DiscordEmbedBuilder embedErr = new DiscordEmbedBuilder();
            embedErr.Title = title;
            embedErr.Description = description;
            embedErr.Color = DiscordColor.Magenta;
            if (footer != "") embedErr.WithFooter(footer);
            await channel.SendMessageAsync(embed: embedErr);
            return;
        }

    }
}
