using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Eco.Plugins.DiscordLink.Events;
using Eco.Gameplay.Economy;
using Eco.Plugins.DiscordLink.Utilities;
using DiscordLink.Extensions;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class CurrencyDisplay : Display
    {
        protected override string BaseTag { get { return "[Currencies]"; } }
        protected override int TimerUpdateIntervalMS { get { return 60000; } }
        protected override int TimerStartDelayMS { get { return 10000; } }

        public override string ToString()
        {
            return "Currency Display";
        }

        protected override DLEventType GetTriggers()
        {
            return base.GetTriggers() | DLEventType.DiscordClientStarted | DLEventType.Timer | DLEventType.CurrencyCreated;
        }

        protected override List<DiscordTarget> GetDiscordTargets()
        {
            return DLConfig.Data.CurrencyDisplayChannels.Cast<DiscordTarget>().ToList();
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();
            IEnumerable<Currency> currencies = CurrencyManager.Currencies;
            var currencyTradesMap = DLStorage.WorldData.CurrencyToTradeCountMap;
            if (!(target is CurrencyChannelLink currencyLink))
                return;

            void AddCurrencyEntry(Currency currency, List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed();
                embed.WithTitle(MessageUtils.StripTags(currency.Name));
                embed.WithFooter(MessageBuilder.Discord.GetStandardEmbedFooter());

                // Find and sort relevant accounts
                IEnumerable<BankAccount> accounts = BankAccountManager.Obj.Accounts.Where(acc => acc.GetCurrencyHoldingVal(currency) >= 1).OrderByDescending(acc => acc.GetCurrencyHoldingVal(currency));
                int tradesCount = currencyTradesMap.Keys.Contains(currency.Id) ? currencyTradesMap[currency.Id] : 0;

                var accountEnumerator = accounts.GetEnumerator();
                string topAccounts = string.Empty;
                string amounts = string.Empty;
                string topAccountHolders = string.Empty;
                for (int i = 0; i < currencyLink.MaxTopCurrencyHolderCount && accountEnumerator.MoveNext(); ++i)
                {
                    // Some bank accounts (e.g treasury) have no creator and one will belong to the bot
                    // Unbacked currencies has their creator owning infinity
                    float currencyAmount = accountEnumerator.Current.GetCurrencyHoldingVal(currency);
                    if (accountEnumerator.Current.Creator == null || accountEnumerator.Current.Creator == DiscordLink.Obj.EcoUser || currencyAmount == float.PositiveInfinity)
                    {
                        --i;
                        continue;
                    }
                    topAccounts += $"{MessageUtils.StripTags(accountEnumerator.Current.Name)}\n";
                    amounts += $"**{accountEnumerator.Current.GetCurrencyHoldingVal(currency):n0}**\n";
                    topAccountHolders += $"{accountEnumerator.Current.Creator.Name}\n";
                }

                if (tradesCount <= 0 && string.IsNullOrWhiteSpace(topAccounts))
                    return;

                string backededItemName = currency.Backed ? $"{currency.BackingItem.DisplayName}" : "Personal";

                // Build message
                embed.AddField("Total trades", tradesCount.ToString("n0"), inline: true);
                embed.AddField("Amount in circulation", currency.Circulation.ToString("n0"), inline: true);
                embed.AddAlignmentField();

                embed.AddField("Backing", backededItemName, inline: true);
                embed.AddField("Coins per item", currency.CoinsPerItem.ToString("n0"), inline: true);
                embed.AddAlignmentField();

                if (!string.IsNullOrWhiteSpace(topAccounts))
                {
                    embed.AddField("Top Holders", topAccountHolders, inline: true);
                    embed.AddField("Amount", amounts, inline: true);
                    embed.AddField("Account", topAccounts, inline: true);
                }
                else
                {
                    embed.AddField("Top Holders", "--- No player holding this currency---", inline: true);
                }

                tagAndContent.Add(new Tuple<string, DiscordLinkEmbed>($"{BaseTag} [{currency.Id}]", embed));
            }

            // Figure out which displays to enable based on config
            bool mintedExists = currencies.Any(c => c.Backed);
            bool useMinted = currencyLink.UseMintedCurrency == CurrencyTypeDisplayCondition.Always
                || (mintedExists && currencyLink.UseMintedCurrency == CurrencyTypeDisplayCondition.MintedExists)
                || (!mintedExists && currencyLink.UseMintedCurrency == CurrencyTypeDisplayCondition.NoMintedExists);

            bool usePersonal = currencyLink.UsePersonalCurrency == CurrencyTypeDisplayCondition.Always
                || (mintedExists && currencyLink.UsePersonalCurrency == CurrencyTypeDisplayCondition.MintedExists)
                || (!mintedExists && currencyLink.UsePersonalCurrency == CurrencyTypeDisplayCondition.NoMintedExists);

            if (useMinted)
            {
                IEnumerable<Currency> mintedCurrencies = currencies.Where(c => c.Backed).OrderByDescending(c => currencyTradesMap.Keys.Contains(c.Id) ? currencyTradesMap[c.Id] : 0);
                var currencyEnumerator = mintedCurrencies.GetEnumerator();
                for (int i = 0; i < currencyLink.MaxMintedCount && currencyEnumerator.MoveNext(); ++i)
                {
                    AddCurrencyEntry(currencyEnumerator.Current, tagAndContent);
                }
            }

            if(usePersonal)
            {
                IEnumerable<Currency> personalCurrencies = currencies.Where(c => !c.Backed).OrderByDescending(c => currencyTradesMap.Keys.Contains(c.Id) ? currencyTradesMap[c.Id] : 0);
                var currencyEnumerator = personalCurrencies.GetEnumerator();
                for (int i = 0; i < currencyLink.MaxPersonalCount && currencyEnumerator.MoveNext(); ++i)
                {
                    if (currencyEnumerator.Current.Creator == DiscordLink.Obj.EcoUser)
                        continue; // Ignore the bot currency

                    AddCurrencyEntry(currencyEnumerator.Current, tagAndContent);
                }
            }
        }
    }
}
