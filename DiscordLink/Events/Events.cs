using Eco.Gameplay.GameActions;
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink.Events
{
    public enum DLEventType
    {
        Timer                   = 1 << 0,
        DiscordClientStarted    = 1 << 1,
        ServerStarted           = 1 << 2,
        ServerStopped           = 1 << 3,
        EcoMessage              = 1 << 4,
        DiscordMessage          = 1 << 5,
        Join                    = 1 << 6,
        Login                   = 1 << 7,
        Logout                  = 1 << 8,
        Trade                   = 1 << 9,
        AccumulatedTrade        = 1 << 10,
        TrackedTradeAdded       = 1 << 11,
        TrackedTradeRemoved     = 1 << 12,
        WorkOrderCreated        = 1 << 13,
        PostedWorkParty         = 1 << 14,
        CompletedWorkParty      = 1 << 15,
        JoinedWorkParty         = 1 << 16,
        LeftWorkParty           = 1 << 17,
        WorkedWorkParty         = 1 << 18,
        Vote                    = 1 << 19,
        StartElection           = 1 << 20,
        StopElection            = 1 << 21,
        CurrencyCreated         = 1 << 22,
    }
}

public class AccumulatedTradeEvent
{
    public List<CurrencyTrade> TradeEvents = new List<CurrencyTrade>();
}