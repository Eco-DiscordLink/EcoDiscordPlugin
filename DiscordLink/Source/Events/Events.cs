using System;

namespace Eco.Plugins.DiscordLink.Events
{
    #pragma warning disable format
    [Flags]
    public enum DlEventType : System.UInt64
    {
        Timer                       = 1L << 0,
        ForceUpdate                 = 1L << 1,
        DiscordClientConnected      = 1L << 2,
        DiscordClientDisconnected   = 1L << 3,
        ServerStarted               = 1L << 4,
        ServerStopped               = 1L << 5,
        WorldReset                  = 1L << 6,
        AccountLinkVerified         = 1L << 7,
        AccountLinkRemoved          = 1L << 8,
        EcoMessageSent              = 1L << 9,
        DiscordMessageSent          = 1L << 10,
        DiscordMessageEdited        = 1L << 11,
        DiscordMessageDeleted       = 1L << 12,
        DiscordReactionAdded        = 1L << 13,
        DiscordReactionRemoved      = 1L << 14,
        DiscordMemberRemoved        = 1L << 15,
        Join                        = 1L << 16,
        Login                       = 1L << 17,
        Logout                      = 1L << 18,
        Trade                       = 1L << 19,
        TradeWatcherDisplayAdded    = 1L << 20,
        TradeWatcherDisplayRemoved  = 1L << 21,
        WorkOrderCreated            = 1L << 22,
        PostedWorkParty             = 1L << 23,
        CompletedWorkParty          = 1L << 24,
        JoinedWorkParty             = 1L << 25,
        LeftWorkParty               = 1L << 26,
        WorkedWorkParty             = 1L << 27,
        Vote                        = 1L << 28,
        ElectionStarted             = 1L << 29,
        ElectionStopped             = 1L << 30,
        CurrencyCreated             = 1L << 31,
        EnteredDemographic          = 1L << 32,
        LeftDemographic             = 1L << 33,
        GainedSpecialty             = 1L << 34,
        LostSpecialty               = 1L << 35,
        AccumulatedServerLog        = 1L << 36,

        // Matched with other plugins
        SettlementFounded           = 1L << 61,
        AccumulatedTrade            = 1L << 62,
    }
    #pragma warning restore format
}
