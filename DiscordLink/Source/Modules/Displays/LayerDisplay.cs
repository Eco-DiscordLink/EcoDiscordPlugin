using Eco.Core.Utils;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Plugins.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class LayerDisplay : DisplayModule
    {
        protected override string BaseTag { get { return "[Layer]"; } }
        protected override int TimerStartDelayMs { get { return 15000; } }
        protected override int TimerUpdateIntervalMs { get { return 3600000; } } // Once an hour as that is the update rate of layers

        public override string ToString() => "Layer Display";
        protected override DlEventType GetTriggers() => base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.Timer;
        protected override async Task<IEnumerable<DiscordTarget>> GetDiscordTargets() => DLConfig.Data.LayerDisplayChannels.Cast<DiscordTarget>();

        protected override async Task<bool> ShouldRun() => await base.ShouldRun() && !NetworkManager.Config.WebServerUrl.IsEmpty();

        protected override async Task HandleConfigChanged(object sender, EventArgs e)
        {
            // TODO: Only update if the network config in particular has changed. This likely requires a specialized EventArgs type
            await base.HandleConfigChanged(sender, e);

            if (await ShouldRun())
                await Update(DiscordLink.Obj, DlEventType.ForceUpdate);
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<DisplayContent> displayContent)
        {
            displayContent = new List<DisplayContent>();
            if (!(target is LayerChannelLink layerTarget) || !layerTarget.IsValid())
                return;
                return;

            string output = layerTarget.UseTerrainComparison
                ? $"{LayerUtils.GetLayerLink($"{layerTarget.LayerName}Latest")}\n{LayerUtils.GetLayerLink("TerrainLatest")}"
                : $"{LayerUtils.GetLayerLink($"{layerTarget.LayerName}Latest")}";
            displayContent.Add(new DisplayContent($"{BaseTag} [{layerTarget.LayerName}]", textContent: output));
        }
    }
}
