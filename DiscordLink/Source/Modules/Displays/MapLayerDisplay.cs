using Eco.Core.Utils;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Plugins.Networking;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class MapLayerDisplay : DisplayModule
    {
        protected override string BaseTag { get { return "[MapLayer]"; } }
        protected override int TimerStartDelayMs { get { return 15000; } }
        protected override int TimerUpdateIntervalMs { get { return 3600000; } } // Once an hour as that is the update rate of layers

        public override string ToString() => "Map Layer Display";
        protected override DlEventType GetTriggers() => base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.Timer;
        protected override async Task<IEnumerable<DiscordTarget>> GetDiscordTargets() => DLConfig.Data.MapDisplayChannels.Cast<DiscordTarget>();

        protected override async Task<bool> ShouldRun() => await base.ShouldRun() && !NetworkManager.Config.WebServerUrl.IsEmpty();

        protected override async Task HandleConfigChanged(object sender, EventArgs e)
        {
            // TODO: Only update if the network config in particular has changed. This likely requires a specialized EventArgs type
            await Update(DiscordLink.Obj, DlEventType.ForceUpdate);
            await base.HandleConfigChanged(sender, e);
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<DisplayContent> displayContent)
        {
            displayContent = null;
            if (target is MapChannelLink mapTarget)
            {
                string layerName = LayerUtils.GetLayerName(mapTarget.MapType);
                if (layerName.IsEmpty())
                    return;

                displayContent = new DisplayContent($"{BaseTag} [Map]", textContent: LayerUtils.GetLayerLink(layerName)).SingleItemAsEnumerable().ToList();
            }
        }
    }
}
