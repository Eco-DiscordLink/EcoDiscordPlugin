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
        EcoMessageSent          = 1 << 4,
        DiscordMessageSent      = 1 << 5,
        DiscordMessageEdited    = 1 << 6,
        DiscordMessageDeleted   = 1 << 7,
        Join                    = 1 << 8,
        Login                   = 1 << 9,
        Logout                  = 1 << 10,
        Trade                   = 1 << 11,
        AccumulatedTrade        = 1 << 12,
        TrackedTradeAdded       = 1 << 13,
        TrackedTradeRemoved     = 1 << 14,
        WorkOrderCreated        = 1 << 15,
        PostedWorkParty         = 1 << 16,
        CompletedWorkParty      = 1 << 17,
        JoinedWorkParty         = 1 << 18,
        LeftWorkParty           = 1 << 19,
        WorkedWorkParty         = 1 << 20,
        Vote                    = 1 << 21,
        StartElection           = 1 << 22,
        StopElection            = 1 << 23,
        CurrencyCreated         = 1 << 24,
    }
}

public class AccumulatedTradeEvent
{
    public List<CurrencyTrade> TradeEvents = new List<CurrencyTrade>();
}