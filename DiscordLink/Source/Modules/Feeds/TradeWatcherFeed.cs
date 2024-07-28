using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
using Eco.Moose.Extensions;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class TradeWatcherFeed : FeedModule
    {
        public override string GetDisplayText(string childInfo, bool verbose)
        {
            string info = $"Tracked Watcher Feeds: {DLStorage.WorldData.TradeWatcherFeedCountTotal}";
            return base.GetDisplayText($"{childInfo}{info}\r\n", verbose);
        }

        public override string ToString()
        {
            return "Trade Watcher Feed";
        }

        protected override DlEventType GetTriggers()
        {
            return DlEventType.AccumulatedTrade;
        }

        protected override async Task<bool> ShouldRun()
        {
            return DLConfig.Data.UseTradeWatcherFeeds;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            if (!(data[0] is List<CurrencyTrade>[] accumulatedTrades))
                return;

            foreach (var memberIdAndWatcher in DLStorage.WorldData.TradeWatchers)
            {
                LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordId(memberIdAndWatcher.Key);
                if (linkedUser == null)
                    continue;

                foreach (List<CurrencyTrade> accumulatedTradeList in accumulatedTrades)
                {
                    CurrencyTrade firstTrade = accumulatedTradeList[0];
                    foreach (TradeWatcherEntry tradeWatcherEntry in memberIdAndWatcher.Value.Where(entry => entry.Type == ModuleArchetype.Feed
                    && (entry.Key.EqualsCaseInsensitive(firstTrade.Buyer.Name) || entry.Key.EqualsCaseInsensitive(firstTrade.Seller.Name) // Player name
                || entry.Key.EqualsCaseInsensitive((firstTrade.WorldObject as WorldObject).Name) // Store name
                || accumulatedTrades.SelectMany(tradeList => tradeList).Any(trade => entry.Key.EqualsCaseInsensitive(trade.ItemUsed.DisplayName))) // Item name
                || accumulatedTrades.SelectMany(tradeList => tradeList).Any(trade => trade.ItemUsed.HasTagWithName(entry.Key)))) // Tag
                    {
                        DiscordLinkEmbed content = MessageBuilder.Discord.GetAccumulatedTradeReport(accumulatedTradeList);
                        await DiscordLink.Obj.Client.SendMessageAsync(await linkedUser.DiscordMember.CreateDmChannelAsync(), string.Empty, content);
                        ++_opsCount;
                    }
                }
            }
        }
    }
}
