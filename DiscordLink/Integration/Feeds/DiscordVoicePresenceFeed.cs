using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.EventArgs;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    class DiscordVoicePresenceFeed : Feed
    {
        private List<VoiceNextConnection> VoiceConenctions = new List<VoiceNextConnection>();

        protected override async Task Initialize()
        {
            await UpdateVoiceChannelConenctions();
        }

        protected override TriggerType GetTriggers()
        {
            return 0;
        }

        protected override bool ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.VoiceFeedChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected async override Task OnConfigChanged()
        {
            await UpdateVoiceChannelConenctions();
            await base.OnConfigChanged();
        }

        protected override Task UpdateInternal(DiscordLink plugin, TriggerType trigger, object data)
        {
            throw new NotImplementedException();
        }

        private async Task UpdateVoiceChannelConenctions()
        {
            DiscordLink plugin = DiscordLink.Obj;
            if (plugin == null) return;

            // Disconnect all existing connections
            foreach (VoiceNextConnection connection in VoiceConenctions)
            {
                connection.Dispose();
            }

            // Set up new connections and register callbacks
            foreach (ChannelLink voiceChannel in DLConfig.Data.VoiceFeedChannels)
            {
                if (!voiceChannel.IsValid()) continue;
                DiscordGuild discordGuild = plugin.GuildByNameOrId(voiceChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByNameOrId(voiceChannel.DiscordChannel, ChannelType.Voice);
                if (discordChannel == null) continue;

                VoiceNextConnection connection = await plugin.VoiceClient.ConnectAsync(discordChannel);
                connection.UserJoined += async (connection, args) => await UserJoined(connection, args);
                connection.UserLeft += async (connection, args) => await UserLeft(connection, args);
                VoiceConenctions.Add(connection);
            }
        }

        private async Task UserJoined(VoiceNextConnection connection, VoiceUserJoinEventArgs args)
        {
            Logger.Debug($"{args.User.Id} joined {connection.TargetChannel.Name}");
        }

        private async Task UserLeft(VoiceNextConnection connection, VoiceUserLeaveEventArgs args)
        {
            Logger.Debug($"{args.User.Id} left {connection.TargetChannel.Name}");
        }
    }
}
