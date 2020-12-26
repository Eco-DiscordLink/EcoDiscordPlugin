using System;
using System.Collections.Generic;
using System.Text;

namespace Eco.Plugins.DiscordLink.Utilities
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
        WorkOrderCreated    = 1 << 8,
        PostedWorkParty     = 1 << 9,
        CompletedWorkParty  = 1 << 10,
        JoinedWorkParty     = 1 << 11,
        LeftWorkParty       = 1 << 12,
        WorkedWorkParty     = 1 << 13,
        Vote                = 1 << 14,
        StartElection       = 1 << 15,
        StopElection        = 1 << 16,
        CurrencyCreated     = 1 << 17,
    }
}
