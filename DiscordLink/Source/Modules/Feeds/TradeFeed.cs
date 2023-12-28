using Eco.Gameplay.GameActions;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class TradeFeed : FeedModule
    {
        public override string ToString()
        {
            return "Trade Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.AccumulatedTrade;
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (ChannelLink link in DLConfig.Data.TradeFeedChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (DLConfig.Data.TradeFeedChannels.Count <= 0)
                return;

            if (!(data[0] is List<CurrencyTrade>[] accumulatedTrades))
                return;

            // Each entry is the summarized trade events for a player and a store
            foreach (List<CurrencyTrade> accumulatedTradeList in accumulatedTrades)
            {
                DiscordLinkEmbed content = MessageBuilder.Discord.GetAccumulatedTradeReport(accumulatedTradeList);
                foreach (ChannelLink tradeLink in DLConfig.Data.TradeFeedChannels)
                {
                    if (!tradeLink.IsValid())
                        continue;

                    await DiscordLink.Obj.Client.SendMessageAsync(tradeLink.Channel, string.Empty, content);
                    ++_opsCount;
                }
            }
        }
    }
}
