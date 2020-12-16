﻿using DSharpPlus.Entities;
using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Objects;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public class TradeFeed : Feed
    {
        private const int TRADE_POSTING_INTERVAL_MS = 1000;
        private readonly Dictionary<Tuple<int, int>, List<CurrencyTrade>> _accumulatedTrades = new Dictionary<Tuple<int, int>, List<CurrencyTrade>>();
        private Timer _tradePostingTimer = null;

        public override async Task Initialize()
        {
            _tradePostingTimer = new Timer(InnerArgs =>
            {
                lock (_overlapLock)
                {
                    if (_accumulatedTrades.Count > 0)
                    {
                        _ = PostAccumulatedTrades();
                    }
                }
            }, null, 0, TRADE_POSTING_INTERVAL_MS);
            await base.Initialize();
        }

        public override async Task Shutdown()
        {
            SystemUtil.StopAndDestroyTimer(ref _tradePostingTimer);
            await base.Shutdown();
        }

        protected override TriggerType GetTriggers()
        {
            return TriggerType.Trade;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (!(data is CurrencyTrade tradeEvent)) return;
            if (DLConfig.Data.TradeChannels.Count <= 0) return;

            // Store the event in a list until we want to post the information. We do this as each item in a trade will fire an individual event and we want to summarize them
            Tuple<int, int> IDTuple = new Tuple<int, int>(tradeEvent.Citizen.Id, (tradeEvent.WorldObject as WorldObject).ID);
            _accumulatedTrades.TryGetValue(IDTuple, out List<CurrencyTrade> trades);
            if (trades == null)
            {
                trades = new List<CurrencyTrade>();
                _accumulatedTrades.Add(IDTuple, trades);
            }

            trades.Add(tradeEvent);
        }

        private async Task PostAccumulatedTrades()
        {
            if (DLConfig.Data.TradeChannels.Count <= 0) return;

            // Each entry is the summarized trade events for a player and a store
            foreach (List<CurrencyTrade> accumulatedTrades in _accumulatedTrades.Values)
            {
                if (accumulatedTrades.Count <= 0) continue;

                CurrencyTrade firstTrade = accumulatedTrades[0];

                DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
                string leftName = firstTrade.Citizen.Name;
                string rightName = (firstTrade.WorldObject as WorldObject).Name;
                builder.Title = leftName + " traded at " + rightName;

                // Go through all acumulated trade events and create a summary
                string boughtItemsDesc = string.Empty;
                float boughtTotal = 0;
                string soldItemsDesc = string.Empty;
                float soldTotal = 0;
                foreach (CurrencyTrade trade in accumulatedTrades)
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
                    builder.AddField("Bought", boughtItemsDesc);
                }

                if (!soldItemsDesc.IsEmpty())
                {
                    soldItemsDesc += "\nTotal = " + soldTotal.ToString("n2");
                    builder.AddField("Sold", soldItemsDesc);
                }

                float subTotal = soldTotal - boughtTotal;
                char sign = (subTotal > 0.0f ? '+' : '-');
                builder.AddField("Total", sign + Math.Abs(subTotal).ToString("n2") + " " + firstTrade.Currency.Name);

                // Post the trade summary in all trade channels
                DiscordLink plugin = DiscordLink.Obj;
                if (plugin == null) return;
                foreach (ChannelLink tradeChannel in DLConfig.Data.TradeChannels)
                {
                    if (!tradeChannel.IsValid()) continue;
                    DiscordGuild discordGuild = plugin.GuildByNameOrId(tradeChannel.DiscordGuild);
                    if (discordGuild == null) continue;
                    DiscordChannel discordChannel = discordGuild.ChannelByNameOrId(tradeChannel.DiscordChannel);
                    if (discordChannel == null) continue;

                    _ = DiscordUtil.SendAsync(discordChannel, "", builder.Build());
                }
            }
            _accumulatedTrades.Clear();
        }
    }
}
