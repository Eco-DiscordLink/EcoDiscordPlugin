using Eco.Gameplay.GameActions;
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink.Events
{
    public enum DLEventType
    {
        Timer               = 1 << 0,
        Startup             = 1 << 1,
        EcoMessage          = 1 << 2,
        DiscordMessage      = 1 << 3,
        Join                = 1 << 4,
        Login               = 1 << 5,
        Logout              = 1 << 6,
        Trade               = 1 << 7,
        AccumulatedTrade    = 1 << 8,
        TrackedTradeAdded   = 1 << 9,
        TrackedTradeRemoved = 1 << 10,
        WorkOrderCreated    = 1 << 11,
        PostedWorkParty     = 1 << 12,
        CompletedWorkParty  = 1 << 13,
        JoinedWorkParty     = 1 << 14,
        LeftWorkParty       = 1 << 15,
        WorkedWorkParty     = 1 << 16,
        Vote                = 1 << 17,
        StartElection       = 1 << 18,
        StopElection        = 1 << 19,
        CurrencyCreated     = 1 << 20,
    }
}

public class AccumulatedTradeEvent
{
    public List<CurrencyTrade> TradeEvents = new List<CurrencyTrade>();
}