using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    class ServerStatusFeed : Feed
    {
        public override string ToString()
        {
            return "Server Status Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.ServerStarted | DLEventType.ServerStopped;
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

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            string message;
            switch (trigger)
            {
                case DLEventType.ServerStarted:
                    message = "Server Started  :white_check_mark:";
                    break;

                case DLEventType.ServerStopped:
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
