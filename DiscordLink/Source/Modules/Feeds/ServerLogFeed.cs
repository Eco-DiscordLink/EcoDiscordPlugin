using Eco.EW.Tools;
using Eco.Plugins.DiscordLink.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    class ServerLogFeed : FeedModule
    {
        public override string ToString()
        {
            return "Server Log Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.AccumulatedServerLog;
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
            if (!(data[0] is Tuple<Logger.LogLevel, string>[] logData))
                return;

            foreach (ServerLogFeedChannelLink serverFeedLink in DLConfig.Data.ServerLogFeedChannels)
            {
                if (!serverFeedLink.IsValid())
                    continue;

                StringBuilder accumulatedLogBuilder = new StringBuilder();
                foreach(Tuple<Logger.LogLevel, string> logEntry in logData)
                {
                    if (serverFeedLink.LogLevel > logEntry.Item1)
                        continue;

                    accumulatedLogBuilder.AppendLine(FormatLogMessage(logEntry.Item1, logEntry.Item2) );
                }

                if(accumulatedLogBuilder.Length > 0)
                {
                    await DiscordLink.Obj.Client.SendMessageAsync(serverFeedLink.Channel, accumulatedLogBuilder.ToString());
                    ++_opsCount;
                }
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
