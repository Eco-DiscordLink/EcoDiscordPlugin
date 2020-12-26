using DSharpPlus.Entities;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
using Eco.Plugins.DiscordLink.Utilities;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public class CraftingFeed : Feed
    {
        protected override TriggerType GetTriggers()
        {
            return TriggerType.WorkOrderCreated;
        }

        protected override bool ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.CraftingChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (!(data is WorkOrderAction craftingEvent)) return;
            if (craftingEvent.Citizen == null) return; // Happens when a crafting table contiues crafting after finishing an item
            if (craftingEvent.MarkedUpName != "Create Work Order") return; // Happens when a player feeds materials to a blocked work order

            string itemName = craftingEvent.OrderCount > 1 ? craftingEvent.CraftedItem.DisplayNamePlural : craftingEvent.CraftedItem.DisplayName; 
            string message = $"**{craftingEvent.Citizen.Name}** started crafting {craftingEvent.OrderCount} `{itemName}` at {(craftingEvent.WorldObject as WorldObject).Name}.";

            foreach (ChannelLink craftingChannel in DLConfig.Data.CraftingChannels)
            {
                if (!craftingChannel.IsValid()) continue;
                DiscordGuild discordGuild = plugin.GuildByNameOrId(craftingChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByNameOrId(craftingChannel.DiscordChannel);
                if (discordChannel == null) continue;
                await DiscordUtil.SendAsync(discordChannel, message);
            }
        }
    }
}
