using DiscordLink.Extensions;
using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class TradeFeed : Feed
    {
        public override string ToString()
        {
            return "Trade Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.AccumulatedTrade;
        }

        protected override bool ShouldRun()
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

            if (!(data is IEnumerable<List<CurrencyTrade>> accumulatedTrades))
                return;

            // Each entry is the summarized trade events for a player and a store
            foreach (List<CurrencyTrade> accumulatedTradeList in accumulatedTrades)
            {
                if (accumulatedTradeList.Count <= 0) continue;
                
                CurrencyTrade firstTrade = accumulatedTradeList[0];

                DiscordLinkEmbed embed = new DiscordLinkEmbed();
                string leftName = firstTrade.Citizen.Name;
                string rightName = (firstTrade.WorldObject as WorldObject).Name;
                embed.WithTitle($"{leftName} traded at {MessageUtil.StripTags(rightName)}");

                // Go through all acumulated trade events and create a summary
                string boughtItemsDesc = string.Empty;
                float boughtTotal = 0;
                string soldItemsDesc = string.Empty;
                float soldTotal = 0;
                foreach (CurrencyTrade trade in accumulatedTradeList)
                {
                    if (trade.BoughtOrSold == Shared.Items.BoughtOrSold.Buying)
                    {
                        boughtItemsDesc += $"{trade.NumberOfItems} X {trade.ItemUsed.DisplayName} * {trade.CurrencyAmount / trade.NumberOfItems} = {trade.CurrencyAmount} \n";
                        boughtTotal += trade.CurrencyAmount;
                    }
                    else if (trade.BoughtOrSold == Shared.Items.BoughtOrSold.Selling)
                    {
                        soldItemsDesc += $"{trade.NumberOfItems} X {trade.ItemUsed.DisplayName} * {trade.CurrencyAmount / trade.NumberOfItems} = {trade.CurrencyAmount} \n";
                        soldTotal += trade.CurrencyAmount;
                    }
                }

                if (!boughtItemsDesc.IsEmpty())
                {
                    boughtItemsDesc += $"\nTotal = {boughtTotal.ToString("n2")}";
                    embed.AddField("Bought", boughtItemsDesc, inline: true);
                }

                if (!soldItemsDesc.IsEmpty())
                {
                    soldItemsDesc += $"\nTotal = {soldTotal.ToString("n2")}";
                    embed.AddField("Sold", soldItemsDesc, inline: true);
                }

                float subTotal = soldTotal - boughtTotal;
                char sign = (subTotal > 0.0f ? '+' : '-');
                embed.AddField("Total", $"{sign} {Math.Abs(subTotal).ToString("n2")} {MessageUtil.StripTags(firstTrade.Currency.Name)}");

                // Post the trade summary in all trade channels
                foreach (ChannelLink tradeLink in DLConfig.Data.TradeFeedChannels)
                {
                    if (!tradeLink.IsValid())
                        continue;

                    _ = DiscordLink.Obj.Client.SendMessageAsync(tradeLink.Channel, string.Empty, embed);
                    ++_opsCount;
                }
            }
        }
    }
}
