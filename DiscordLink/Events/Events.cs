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
        WorkOrderCreated    = 1 << 9,
        PostedWorkParty     = 1 << 10,
        CompletedWorkParty  = 1 << 11,
        JoinedWorkParty     = 1 << 12,
        LeftWorkParty       = 1 << 13,
        WorkedWorkParty     = 1 << 14,
        Vote                = 1 << 15,
        StartElection       = 1 << 16,
        StopElection        = 1 << 17,
        CurrencyCreated     = 1 << 18,
    }
}

public class AccumulatedTradeEvent
{
    public List<CurrencyTrade> TradeEvents = new List<CurrencyTrade>();
}