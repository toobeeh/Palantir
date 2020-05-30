﻿using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using System.Threading;

namespace Palantir
{
    public class Tether
    {
        public ObservedGuild PalantirEndpoint { get; private set; }
        private Thread Dataflow;
        private bool abort;
        private DiscordMessage TargetMessage;
        private DiscordChannel TargetChannel;


        public Tether(ObservedGuild guild)
        {
            abort = false;
            PalantirEndpoint = guild;
            Dataflow = new Thread(new ThreadStart(ObserveLobbies));
            Dataflow.Name = "Dataflow GuildID " + guild.GuildID;
        }

        public void SetNewPalantirEndpoint(ObservedGuild guild)
        {
            abort = false;
            PalantirEndpoint = guild;
            Dataflow = new Thread(new ThreadStart(ObserveLobbies));
            Dataflow.Name = "Dataflow GuildID " + guild.GuildID;
        }

        public void EstablishDataflow()
        {
            Dataflow.Start();
        }

        public void StopDataflow()
        {
            abort = true;
        }

        private async void ObserveLobbies()
        {
            try
            {
                TargetChannel = await Program.Client.GetChannelAsync(PalantirEndpoint.ChannelID);
                TargetMessage = await TargetChannel.GetMessageAsync(PalantirEndpoint.MessageID);
            }
            catch
            {
                try
                {
                    DiscordGuild guild = await Program.Client.GetGuildAsync(PalantirEndpoint.GuildID);
                    Feanor.RemovePalantiri(PalantirEndpoint);
                    StopDataflow();
                    Console.WriteLine("Removed guild " + PalantirEndpoint.GuildID);
                    await guild.GetDefaultChannel().SendMessageAsync("The observed message couldn't be found. Set a new channel!");
                }
                catch
                {
                    Feanor.RemovePalantiri(PalantirEndpoint);
                    StopDataflow();
                    Console.WriteLine("Removed guild " + PalantirEndpoint.GuildID);
                    return;
                }
            }
            

            while (!abort)
            {
                TargetMessage = await TargetMessage.ModifyAsync(TargetMessage.Content + ".");
                Thread.Sleep(1000);
            }
        }

    }
}
