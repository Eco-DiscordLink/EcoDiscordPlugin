namespace Eco.Plugins.DiscordLink.Events
{
    #pragma warning disable format
    public enum DLEventType : System.UInt64
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
        Join                        = 1L << 15,
        Login                       = 1L << 16,
        Logout                      = 1L << 17,
        Trade                       = 1L << 18,
        TradeWatcherDisplayAdded    = 1L << 19,
        TradeWatcherDisplayRemoved  = 1L << 20,
        WorkOrderCreated            = 1L << 21,
        PostedWorkParty             = 1L << 22,
        CompletedWorkParty          = 1L << 23,
        JoinedWorkParty             = 1L << 24,
        LeftWorkParty               = 1L << 25,
        WorkedWorkParty             = 1L << 26,
        Vote                        = 1L << 27,
        StartElection               = 1L << 28,
        StopElection                = 1L << 29,
        CurrencyCreated             = 1L << 30,
        EnteredDemographic          = 1L << 31,
        LeftDemographic             = 1L << 32,
        GainedSpecialty             = 1L << 33,
        AccumulatedServerLog        = 1L << 34,

        // Matched with other plugins
        AccumulatedTrade            = 1L << 62,
    }
    #pragma warning restore format
}
