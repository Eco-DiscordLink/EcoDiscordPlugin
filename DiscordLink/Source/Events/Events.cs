using Eco.Gameplay.GameActions;
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink.Events
{
    public enum DLEventType
    {
        Timer                       = 1 << 0,
        DiscordClientConnected      = 1 << 1,
        DiscordClientDisconnected   = 1 << 2,
        ServerStarted               = 1 << 3,
        AccountLinkVerified         = 1 << 4,
        AccountLinkRemoved          = 1 << 5,
        ServerStopped               = 1 << 6,
        EcoMessageSent              = 1 << 7,
        DiscordMessageSent          = 1 << 8,
        DiscordMessageEdited        = 1 << 9,
        DiscordMessageDeleted       = 1 << 10,
        DiscordReactionAdded        = 1 << 11,
        DiscordReactionRemoved      = 1 << 12,
        Join                        = 1 << 13,
        Login                       = 1 << 14,
        Logout                      = 1 << 15,
        Trade                       = 1 << 16,
        AccumulatedTrade            = 1 << 17,
        TrackedTradeAdded           = 1 << 18,
        TrackedTradeRemoved         = 1 << 19,
        WorkOrderCreated            = 1 << 20,
        PostedWorkParty             = 1 << 21,
        CompletedWorkParty          = 1 << 22,
        JoinedWorkParty             = 1 << 23,
        LeftWorkParty               = 1 << 24,
        WorkedWorkParty             = 1 << 25,
        Vote                        = 1 << 26,
        StartElection               = 1 << 27,
        StopElection                = 1 << 28,
        CurrencyCreated             = 1 << 29,
    }
}

public class AccumulatedTradeEvent
{
    public List<CurrencyTrade> TradeEvents = new List<CurrencyTrade>();
}