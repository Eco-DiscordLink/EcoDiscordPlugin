using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.Property;
using Eco.Moose.Utils.Lookups;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class WorkPartyDisplay : DisplayModule
    {
        protected override string BaseTag { get { return "[Work Party]"; } }
        protected override int TimerUpdateIntervalMs { get { return 60000; } }
        protected override int TimerStartDelayMs { get { return 10000; } }

        public override string ToString() => "Work Party Display";
        protected override DlEventType GetTriggers() => base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.Timer
            | DlEventType.PostedWorkParty | DlEventType.CompletedWorkParty | DlEventType.JoinedWorkParty | DlEventType.LeftWorkParty | DlEventType.WorkedWorkParty;
        protected override async Task<IEnumerable<DiscordTarget>> GetDiscordTargets() => DLConfig.Data.WorkPartyDisplayChannels.Cast<DiscordTarget>();

        protected override void GetDisplayContent(DiscordTarget target, out List<DisplayContent> displayContent)
        {
            displayContent = new List<DisplayContent>();
            foreach (WorkParty workParty in Lookups.ActiveWorkParties)
            {
                string tag = $"{BaseTag} [{workParty.Id}]";
                DiscordLinkEmbed report = MessageBuilder.Discord.GetWorkPartyReport(workParty);
                if (report.Fields.Count > 0)
                    displayContent.Add(new DisplayContent(tag, embedContent: report));
            }
        }
    }
}
