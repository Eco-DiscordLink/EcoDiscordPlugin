using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
using Eco.Plugins.DiscordLink.Events;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class CraftingFeed : FeedModule
    {
        public override string ToString()
        {
            return "Crafting Feed";
        }

        protected override DlEventType GetTriggers()
        {
            return DlEventType.WorkOrderCreated;
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.CraftingFeedChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            if (!(data[0] is CreateWorkOrder craftingEvent))
                return;

            string itemName = craftingEvent.OrderCount > 1 ? craftingEvent.CraftedItem.DisplayNamePlural : craftingEvent.CraftedItem.DisplayName;
            string message = $"**{craftingEvent.Citizen.Name}** started crafting {craftingEvent.OrderCount} `{itemName}` at {(craftingEvent.WorldObject as WorldObject).Name}.";

            foreach (ChannelLink craftingLink in DLConfig.Data.CraftingFeedChannels)
            {
                if (!craftingLink.IsValid())
                    continue;

                await plugin.Client.SendMessageAsync(craftingLink.Channel, message);
                ++_opsCount;
            }
        }
    }
}
