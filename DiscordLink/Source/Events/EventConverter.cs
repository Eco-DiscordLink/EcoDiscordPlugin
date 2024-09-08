using Eco.Core.Utils;
using Eco.Core.Utils.Logging;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.SystemUtils;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Eco.Plugins.DiscordLink.Events
{
    public class DLEventArgs : EventArgs
    {
        public DLEventArgs(DlEventType eventType, object[] data)
        {
            EventType = eventType;
            Data = data;
        }

        public DlEventType EventType { get; set; }
        public object[] Data { get; set; }
    }

    public sealed class EventConverter
    {
        public static readonly ThreadSafeAction<DLEventArgs> OnEventConverted = new ThreadSafeAction<DLEventArgs>();

        public static readonly EventConverter Instance = new EventConverter();

        private const int LOG_POSTING_INTERVAL_MS = 1000;
        private readonly List<Tuple<Logger.LogLevel, string>> _accumulatedLogs = new List<Tuple<Logger.LogLevel, string>>();
        private Timer _logPostingTimer = null;
        private readonly AsyncLock _accumulatedLogssoverlapLock = new AsyncLock();
        private readonly AsyncLock _accumulatedLogsLock = new AsyncLock();

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static EventConverter()
        {
        }

        private EventConverter()
        {
        }

        public void Initialize()
        {
            // Initialize log accumulation
            _logPostingTimer = new Timer(InnerArgs =>
            {
                using (_accumulatedLogssoverlapLock.Lock()) // Make sure this code isn't entered multiple times simultaniously
                {
                    try
                    {
                        if (_accumulatedLogs.Count > 0 && DiscordLink.Obj.Status == DiscordLink.StatusState.Connected)
                        {
                            // Fire the accumulated event
                            Tuple<Logger.LogLevel, string>[] logs = null;
                            using (_accumulatedLogsLock.Lock())
                            {
                                logs = _accumulatedLogs.ToArray();
                                _accumulatedLogs.Clear();
                            }
                            FireEvent(DlEventType.AccumulatedServerLog, (object)logs);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Trace($"Failed to accumulate log events. Exception: {e}"); // Using trace log only as it's risky to log here since we could cause a feedback loop.
                    }
                }
            }, null, 0, LOG_POSTING_INTERVAL_MS);

            // Start listening for log events
            ClientLogEventTrigger.OnLogWritten += (message) => AccumulateLogEvent(message);
        }

        public void Shutdown()
        {
            SystemUtils.StopAndDestroyTimer(ref _logPostingTimer);
        }

        public void HandleEvent(DlEventType eventType, params object[] data)
        {
            switch (eventType)
            {
                default:
                    break;
            }
        }

        public void AccumulateLogEvent(string logEventText)
        {
            string strippedEventText = MessageUtils.StripTags(logEventText);
            Logger.LogLevel eventLevel = Logger.LogLevel.Silent;
            string[] parts = strippedEventText.Split(':', 2);
            if (parts.Length == 0)
            {
                Logger.Warning($"Ignored non delimited log event: \"{strippedEventText}\"");
                return;
            }

            if (parts[0].ContainsCaseInsensitive("Log"))
                eventLevel = Logger.LogLevel.Information;
            else if (parts[0].ContainsCaseInsensitive("Error"))
                eventLevel = Logger.LogLevel.Error;
            else if (parts[0].ContainsCaseInsensitive("Warning"))
                eventLevel = Logger.LogLevel.Warning;
            else if (parts[0].ContainsCaseInsensitive("Debug"))
                eventLevel = Logger.LogLevel.Debug;

            using (_accumulatedLogsLock.Lock())
            {
                _accumulatedLogs.Add(new Tuple<Logger.LogLevel, string>(eventLevel, parts[1].Trim()));
            }
        }

        private void FireEvent(DlEventType evetType, params object[] data)
        {
            OnEventConverted.Invoke(new DLEventArgs(evetType, data));
        }
    }
}
