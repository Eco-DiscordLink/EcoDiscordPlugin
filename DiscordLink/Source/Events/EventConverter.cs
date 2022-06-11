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
        public static event EventHandler<DLEventArgs> OnEventFired;

        public static readonly EventConverter Instance = new EventConverter();

        private const int TRADE_POSTING_INTERVAL_MS = 1000;
        private readonly Dictionary<Tuple<int, int>, List<CurrencyTrade>> _accumulatedTrades = new Dictionary<Tuple<int, int>, List<CurrencyTrade>>();
        private Timer _tradePostingTimer = null;
        private readonly AsyncLock _overlapLock = new AsyncLock();
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
            _tradePostingTimer = new Timer(InnerArgs =>
            {
                using (_overlapLock.Lock()) // Make sure this code isn't entered multiple times simultaniously
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
                    catch(Exception e)
                    {
                        Logger.Error($"Failed to accumulate trade events. Error: {e}");
                    }
                }

            }, null, 0, TRADE_POSTING_INTERVAL_MS);
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
                    _accumulatedTrades.TryGetValue(IDTuple, out List<CurrencyTrade> trades);
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

        public void ConvertServerLogEvent(string logEventText)
        {
            string strippedEventText = MessageUtils.StripTags(logEventText);
            Logger.LogLevel eventLevel = Logger.LogLevel.Silent;
            string[] parts = strippedEventText.Split( ':', 2);
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

            FireEvent(DLEventType.ServerLogWritten, eventLevel, parts[1].Trim());
        }

        private void FireEvent(DLEventType evetType, params object[] data)
        {
            if (OnEventFired != null)
                OnEventFired.Invoke(this, new DLEventArgs(evetType, data));
        }
    }
}
