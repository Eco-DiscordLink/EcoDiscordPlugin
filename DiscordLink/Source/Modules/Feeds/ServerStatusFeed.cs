﻿using Eco.Moose.Tools.Logger;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    class ServerStatusFeed : FeedModule
    {
        public override string ToString()
        {
            return "Server Status Feed";
        }

        protected override DlEventType GetTriggers()
        {
            return DlEventType.ServerStarted | DlEventType.ServerStopped;
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.ServerStatusFeedChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            string message;
            switch (trigger)
            {
                case DlEventType.ServerStarted:
                    message = "Server Started  :white_check_mark:";
                    break;

                case DlEventType.ServerStopped:
                    message = "Server Stopped  :x:";
                    break;

                default:
                    Logger.Debug("Server Status Feed received unexpected trigger type");
                    return;
            }

            DiscordLinkEmbed embed = new DiscordLinkEmbed();
            embed.WithTitle(message);

            foreach (ChannelLink serverStatusLink in DLConfig.Data.ServerStatusFeedChannels)
            {
                if (!serverStatusLink.IsValid())
                    continue;

                await DiscordLink.Obj.Client.SendMessageAsync(serverStatusLink.Channel, null, embed);
                ++_opsCount;
            }
        }
    }
}
