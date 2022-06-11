using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    class ServerLogFeed : Feed
    {
        public override string ToString()
        {
            return "Server Status Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.ServerLogWritten;
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.ServerLogFeedChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (!(data[0] is Logger.LogLevel logLevel) || !(data[1] is string message))
                return;

            foreach (ServerLogFeedChannelLink serverFeedLink in DLConfig.Data.ServerLogFeedChannels)
            {
                if (!serverFeedLink.IsValid())
                    continue;

                if (serverFeedLink.LogLevel > logLevel)
                    continue;

                await DiscordLink.Obj.Client.SendMessageAsync(serverFeedLink.Channel, FormatLogMessage(logLevel, message));
                ++_opsCount;
            }
        }

        private string FormatLogMessage(Logger.LogLevel logLevel, string message)
        {
            string newMeta = logLevel switch
            {
                Logger.LogLevel.Debug => DLConstants.DEBUG_LOG_EMOJI,
                Logger.LogLevel.Warning => DLConstants.WARNING_LOG_EMOJI,
                Logger.LogLevel.Information => DLConstants.INFO_LOG_EMOJI,
                Logger.LogLevel.Error => DLConstants.ERROR_LOG_EMOJI,
                _ => DLConstants.DEBUG_LOG_EMOJI
            };

            return $"{newMeta} {message}";
        }
    }
}
