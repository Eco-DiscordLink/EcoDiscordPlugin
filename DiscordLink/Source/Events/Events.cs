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
        DiscordReactionAdded    = 1 << 8,
        DiscordReactionRemoved  = 1 << 9,
        Join                    = 1 << 10,
        Login                   = 1 << 11,
        Logout                  = 1 << 12,
        Trade                   = 1 << 13,
        AccumulatedTrade        = 1 << 14,
        TrackedTradeAdded       = 1 << 15,
        TrackedTradeRemoved     = 1 << 16,
        WorkOrderCreated        = 1 << 17,
        PostedWorkParty         = 1 << 18,
        CompletedWorkParty      = 1 << 19,
        JoinedWorkParty         = 1 << 20,
        LeftWorkParty           = 1 << 21,
        WorkedWorkParty         = 1 << 22,
        Vote                    = 1 << 23,
        StartElection           = 1 << 24,
        StopElection            = 1 << 25,
        CurrencyCreated         = 1 << 26,
    }
}

public class AccumulatedTradeEvent
{
    public List<CurrencyTrade> TradeEvents = new List<CurrencyTrade>();
}