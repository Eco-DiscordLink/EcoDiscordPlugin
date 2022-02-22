using Eco.Gameplay.Economy.WorkParties;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class WorkPartyDisplay : Display
    {
        protected override string BaseTag { get { return "[Work Party]"; } }
        protected override int TimerUpdateIntervalMS { get { return 60000; } }
        protected override int TimerStartDelayMS { get { return 10000; } }

        public override string ToString()
        {
            return "Work Party Display";
        }

        protected override DLEventType GetTriggers()
        {
            return base.GetTriggers() | DLEventType.DiscordClientConnected | DLEventType.Timer | DLEventType.PostedWorkParty | DLEventType.CompletedWorkParty
                | DLEventType.JoinedWorkParty | DLEventType.LeftWorkParty | DLEventType.WorkedWorkParty;
        }

        protected override async Task<List<DiscordTarget>> GetDiscordTargets()
        {
            return DLConfig.Data.WorkPartyDisplayChannels.Cast<DiscordTarget>().ToList();
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();
            foreach (WorkParty workParty in EcoUtils.ActiveWorkParties)
            {
                string tag = $"{BaseTag} [{workParty.Id}]";
                DiscordLinkEmbed report = MessageBuilder.Discord.GetWorkPartyReport(workParty);
                if (report.Fields.Count > 0)
                    tagAndContent.Add(new Tuple<string, DiscordLinkEmbed>(tag, report));
            }
        }
    }
}
