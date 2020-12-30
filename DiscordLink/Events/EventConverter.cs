using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
using Eco.Plugins.DiscordLink.Utilities;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Eco.Plugins.DiscordLink.Events
{
    public class DLEventArgs : EventArgs
    {
        public DLEventArgs(DLEventType eventType, object data )
        {
            EventType = eventType;
            Data = data;
        }

        public DLEventType EventType { get; set; }
        public object Data { get; set; }
    }

    public sealed class EventConverter
    {
        public static event EventHandler<DLEventArgs> OnEventFired;

        public static readonly EventConverter Instance = new EventConverter();

        private const int TRADE_POSTING_INTERVAL_MS = 1000;
        private readonly Dictionary<Tuple<int, int>, List<CurrencyTrade>> _accumulatedTrades = new Dictionary<Tuple<int, int>, List<CurrencyTrade>>();
        private Timer _tradePostingTimer = null;
        private readonly AsyncLock _overlapLock = new AsyncLock();

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
                using (_overlapLock.Lock()) // Make sure this code isn't entered multiple time simultaniously
                {
                    if (_accumulatedTrades.Count > 0)
                    {
                        // Fire the accumulated event
                        FireEvent(DLEventType.AccumulatedTrade, _accumulatedTrades.Values);
                        _accumulatedTrades.Clear();
                    }
                }

            }, null, 0, TRADE_POSTING_INTERVAL_MS);
        }

        public void Shutdown()
        {
            SystemUtil.StopAndDestroyTimer(ref _tradePostingTimer);
        }

        public void HandleEvent(DLEventType eventType, object data)
        {
            switch(eventType)
            {
                case DLEventType.Trade:
                    if (!(data is CurrencyTrade tradeEvent)) return;

                    // Store the event in a list in order to accumulate trade events that should be considered as one. We do this as each item in a trade will fire an individual event and we want to summarize them
                    Tuple<int, int> IDTuple = new Tuple<int, int>(tradeEvent.Citizen.Id, (tradeEvent.WorldObject as WorldObject).ID);
                    _accumulatedTrades.TryGetValue(IDTuple, out List<CurrencyTrade> trades);
                    if (trades == null)
                    {
                        trades = new List<CurrencyTrade>();
                        _accumulatedTrades.Add(IDTuple, trades);
                    }

                    trades.Add(tradeEvent);
                    break;

                default:
                    break;
            }
        }

        private void FireEvent(DLEventType evetType, object data)
        {
            if(OnEventFired != null)
                OnEventFired.Invoke(this, new DLEventArgs(evetType, data));
        }
    }
}
