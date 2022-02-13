using Eco.Gameplay.GameActions;
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink.Events
{
    #pragma warning disable format
    public enum DLEventType : System.Int64
    {
        Timer                       = 1L << 0,
        DiscordClientConnected      = 1L << 1,
        DiscordClientDisconnected   = 1L << 2,
        ServerStarted               = 1L << 3,
        AccountLinkVerified         = 1L << 4,
        AccountLinkRemoved          = 1L << 5,
        ServerStopped               = 1L << 6,
        EcoMessageSent              = 1L << 7,
        DiscordMessageSent          = 1L << 8,
        DiscordMessageEdited        = 1L << 9,
        DiscordMessageDeleted       = 1L << 10,
        DiscordReactionAdded        = 1L << 11,
        DiscordReactionRemoved      = 1L << 12,
        Join                        = 1L << 13,
        Login                       = 1L << 14,
        Logout                      = 1L << 15,
        Trade                       = 1L << 16,
        AccumulatedTrade            = 1L << 17,
        TradeWatcherDisplayAdded    = 1L << 18,
        TradeWatcherDisplayRemoved  = 1L << 19,
        WorkOrderCreated            = 1L << 20,
        PostedWorkParty             = 1L << 21,
        CompletedWorkParty          = 1L << 22,
        JoinedWorkParty             = 1L << 23,
        LeftWorkParty               = 1L << 24,
        WorkedWorkParty             = 1L << 25,
        Vote                        = 1L << 26,
        StartElection               = 1L << 27,
        StopElection                = 1L << 28,
        CurrencyCreated             = 1L << 29,
        EnteredDemographic          = 1L << 30,
        LeftDemographic             = 1L << 31,
        GainedSpecialty             = 1L << 32,
        ServerLogWritten            = 1L << 33,
    }
    #pragma warning restore format
}

public class AccumulatedTradeEvent
{
    public List<CurrencyTrade> TradeEvents = new List<CurrencyTrade>();
}