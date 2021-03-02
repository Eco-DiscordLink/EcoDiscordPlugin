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
            return DLEventType.DiscordClientStarted | DLEventType.Timer | DLEventType.CurrencyCreated;
        }

        protected override List<DiscordTarget> GetDiscordTargets()
        {
            return DLConfig.Data.CurrencyChannels.Cast<DiscordTarget>().ToList();
        }

        protected override void GetDisplayContent(DiscordTarget target, out List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
        {
            tagAndContent = new List<Tuple<string, DiscordLinkEmbed>>();
            IEnumerable<Currency> currencies = CurrencyManager.Currencies;
            var currencyTradesMap = DLStorage.WorldData.CurrencyToTradeCountMap;
            CurrencyChannelLink currencyLink = target as CurrencyChannelLink;
            if (currencyLink == null)
                return;

            void AddCurrencyEntry(Currency currency, List<Tuple<string, DiscordLinkEmbed>> tagAndContent)
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed();
                embed.WithFooter(MessageBuilder.Discord.GetStandardEmbedFooter());

                // Find and sort relevant accounts
                IEnumerable<BankAccount> accounts = BankAccountManager.Obj.Accounts.Where(acc => acc.GetCurrencyHoldingVal(currency) >= 1).OrderByDescending(acc => acc.GetCurrencyHoldingVal(currency));
                var currencyEnumerator = accounts.GetEnumerator();
                string topAccounts = string.Empty;
                for (int i = 0; i < currencyLink.MaxTopCurrencyHolderCount && currencyEnumerator.MoveNext(); ++i)
                {
                    // Some bank accounts (e.g treasury) have no creator
                    // Unbacked currencies has their creator owning infinity
                    float currencyAmount = currencyEnumerator.Current.GetCurrencyHoldingVal(currency);
                    if (currencyEnumerator.Current.Creator == null || currencyAmount == float.PositiveInfinity) 
                    {
                        --i;
                        continue;
                    }
                    topAccounts += $"**{currencyEnumerator.Current.GetCurrencyHoldingVal(currency):n0}** - {MessageUtil.StripTags(currencyEnumerator.Current.Name)} *({currencyEnumerator.Current.Creator.Name})*\n";
                }

                // Fetch data
                int tradesCount = currencyTradesMap.Keys.Contains(currency.Id) ? currencyTradesMap[currency.Id] : 0;
                string backededItemName = currency.Backed ? $"{currency.BackingItem.DisplayName}" : "Personal";

                // Build message
                string circulationDesc = $"**Total in circulation**: {currency.Circulation:n0}\n";
                string tradesCountDesc = currencyLink.UseTradeCount ? $"**Total trades**: {tradesCount}\n" : string.Empty;
                string backedItemDesc = currencyLink.UseBackingInfo ? $"**Backing**: {backededItemName}\n" : string.Empty;
                string coinsPerItemDesc = (currencyLink.UseBackingInfo && currency.Backed) ? $"**Coins per item**: {currency.CoinsPerItem}\n" : string.Empty;
                string topAccountsDesc = $"**Top accounts**\n{topAccounts}";
                embed.AddField(MessageUtil.StripTags(currency.Name), $"{circulationDesc}{tradesCountDesc}{backedItemDesc}{coinsPerItemDesc}\n{topAccountsDesc}");
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
                    AddCurrencyEntry(currencyEnumerator.Current, tagAndContent);
                }
            }
        }
    }
}
