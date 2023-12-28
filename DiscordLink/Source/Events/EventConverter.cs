using Eco.Core.Utils;
using Eco.Core.Utils.Logging;
using Eco.Moose.Tools;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
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
        public DLEventArgs(DLEventType eventType, object[] data)
        {
            EventType = eventType;
            Data = data;
        }

        public DLEventType EventType { get; set; }
        public object[] Data { get; set; }
    }

    public sealed class EventConverter
    {
        public static readonly ThreadSafeAction<DLEventArgs> OnEventConverted = new ThreadSafeAction<DLEventArgs>();

        public static readonly EventConverter Instance = new EventConverter();

        private const int LOG_POSTING_INTERVAL_MS = 1000;
        private const int TRADE_POSTING_INTERVAL_MS = 1000;
        private readonly List<Tuple<Logger.LogLevel, string>> _accumulatedLogs = new List<Tuple<Logger.LogLevel, string>>();
        private readonly Dictionary<Tuple<int, int>, List<CurrencyTrade>> _accumulatedTrades = new Dictionary<Tuple<int, int>, List<CurrencyTrade>>();
        private Timer _logPostingTimer = null;
        private Timer _tradePostingTimer = null;
        private readonly AsyncLock _accumulatedLogssoverlapLock = new AsyncLock();
        private readonly AsyncLock _accumulatedTradesoverlapLock = new AsyncLock();
        private readonly AsyncLock _accumulatedLogsLock = new AsyncLock();
        private readonly AsyncLock _accumulatedTradesLock = new AsyncLock();

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static EventConverter()
        {
        }

        private EventConverter()
        {
        }

        public void Initialize()
        {
            // Initialize trade accumulation
            _tradePostingTimer = new Timer(InnerArgs =>
            {
                using (_accumulatedTradesoverlapLock.Lock()) // Make sure this code isn't entered multiple times simultaniously
                {
                    try
                    {
                        if (_accumulatedTrades.Count > 0)
                        {
                            // Fire the accumulated event
                            List<CurrencyTrade>[] trades = null;
                            using (_accumulatedTradesLock.Lock())
                            {
                                trades = new List<CurrencyTrade>[_accumulatedTrades.Values.Count];
                                _accumulatedTrades.Values.CopyTo(trades, 0);
                                _accumulatedTrades.Clear();
                            }
                            FireEvent(DLEventType.AccumulatedTrade, (object)trades);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Exception($"Failed to accumulate trade events", e);
                    }
                }

            }, null, 0, TRADE_POSTING_INTERVAL_MS);

            // Initialize log accumulation
            _logPostingTimer = new Timer(InnerArgs =>
            {
                using (_accumulatedLogssoverlapLock.Lock()) // Make sure this code isn't entered multiple times simultaniously
                {
                    try
                    {
                        if (_accumulatedLogs.Count > 0 && DiscordLink.Obj.Status == DiscordLink.StatusState.Connected )
                        {
                            // Fire the accumulated event
                            Tuple<Logger.LogLevel, string>[] logs = null;
                            using (_accumulatedLogsLock.Lock())
                            {
                                logs = _accumulatedLogs.ToArray();
                                _accumulatedLogs.Clear();
                            }
                            FireEvent(DLEventType.AccumulatedServerLog, (object)logs);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.DebugVerbose($"Failed to accumulate log events. Exception: {e}"); // Using verbose log only as it's risky to log here since we could cause a feedback loop.
                    }
                }
            }, null, 0, LOG_POSTING_INTERVAL_MS);

            // Start listening for log events
            ClientLogEventTrigger.OnLogWritten += (message) => AccumulateLogEvent(message);
        }

        public void Shutdown()
        {
            SystemUtils.StopAndDestroyTimer(ref _tradePostingTimer);
        }

        public void HandleEvent(DLEventType eventType, params object[] data)
        {
            switch (eventType)
            {
                case DLEventType.Trade:
                    if (!(data[0] is CurrencyTrade tradeEvent))
                        return;

                    // Store the event in a list in order to accumulate trade events that should be considered as one. We do this as each item in a trade will fire an individual event and we want to summarize them
                    Tuple<int, int> IDTuple = new Tuple<int, int>(tradeEvent.Citizen.Id, (tradeEvent.WorldObject as WorldObject).ID);
                    List<CurrencyTrade> trades;
                    using (_accumulatedTradesLock.Lock())
                    {
                        _accumulatedTrades.TryGetValue(IDTuple, out trades);
                    }
                    if (trades == null)
                    {
                        trades = new List<CurrencyTrade>();
                        using (_accumulatedTradesLock.Lock())
                        {
                            _accumulatedTrades.Add(IDTuple, trades);
                        }
                    }
                    using (_accumulatedTradesLock.Lock())
                    {
                        trades.Add(tradeEvent);
                    }
                    break;

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

        private void FireEvent(DLEventType evetType, params object[] data)
        {
            OnEventConverted.Invoke(new DLEventArgs(evetType, data));
        }
    }
}
