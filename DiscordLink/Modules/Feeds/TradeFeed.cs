using DiscordLink.Extensions;
using DSharpPlus.Entities;
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
            foreach (ChannelLink link in DLConfig.Data.TradeChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, object data)
        {
            if (DLConfig.Data.TradeChannels.Count <= 0) return;
            if (!(data is IEnumerable<List<CurrencyTrade>> accumulatedTrades)) return;


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
                        boughtItemsDesc += trade.NumberOfItems + " X " + trade.ItemUsed.DisplayName + " * " + trade.CurrencyAmount / trade.NumberOfItems + " = " + trade.CurrencyAmount + "\n";
                        boughtTotal += trade.CurrencyAmount;
                    }
                    else if (trade.BoughtOrSold == Shared.Items.BoughtOrSold.Selling)
                    {
                        soldItemsDesc += trade.NumberOfItems + " X " + trade.ItemUsed.DisplayName + " * " + trade.CurrencyAmount / trade.NumberOfItems + " = " + trade.CurrencyAmount + "\n";
                        soldTotal += trade.CurrencyAmount;
                    }
                }

                if (!boughtItemsDesc.IsEmpty())
                {
                    boughtItemsDesc += "\nTotal = " + boughtTotal.ToString("n2");
                    embed.AddField("Bought", boughtItemsDesc);
                }

                if (!soldItemsDesc.IsEmpty())
                {
                    soldItemsDesc += "\nTotal = " + soldTotal.ToString("n2");
                    embed.AddField("Sold", soldItemsDesc);
                }

                float subTotal = soldTotal - boughtTotal;
                char sign = (subTotal > 0.0f ? '+' : '-');
                embed.AddField("Total", sign + Math.Abs(subTotal).ToString("n2") + " " + MessageUtil.StripTags(firstTrade.Currency.Name));

                // Post the trade summary in all trade 
                foreach (ChannelLink tradeChannel in DLConfig.Data.TradeChannels)
                {
                    if (!tradeChannel.IsValid()) continue;
                    DiscordGuild discordGuild = plugin.GuildByNameOrId(tradeChannel.DiscordGuild);
                    if (discordGuild == null) continue;
                    DiscordChannel discordChannel = discordGuild.ChannelByNameOrId(tradeChannel.DiscordChannel);
                    if (discordChannel == null) continue;

                    _ = DiscordUtil.SendAsync(discordChannel, string.Empty, embed);
                }
            }
        }
    }
}
