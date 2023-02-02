using System;
using System.Collections.Generic;
using System.Linq;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Shared.Utils;

using LookupEntry = Eco.Plugins.DiscordLink.Utilities.Either<Eco.Gameplay.Items.Item, Eco.Gameplay.Players.User, Eco.Gameplay.Items.Tag, Eco.Gameplay.Components.StoreComponent>;
using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public enum TradeTargetType
    {
        Tag,
        Item,
        User,
        Store,
        Invalid,
    }

    public class TradeUtils
    {
        private static List<LookupEntry> _itemLookup = null;

        public static List<LookupEntry> ItemLookup =>
            _itemLookup ??= Item.AllItems.Select(item => new LookupEntry(item)).ToList();

        private static List<LookupEntry> _tagLookup = null;

        public static List<LookupEntry> TagLookup =>
            _tagLookup ??= FindTags().Select(tag => new LookupEntry(tag)).ToList();

        public static List<LookupEntry> UserLookup => UserManager.Users.Select(user => new LookupEntry(user)).ToList();

        public static List<LookupEntry> StoreLookup => Stores.Select(store => new LookupEntry(store)).ToList();

        public static T BestMatchOrDefault<T>(string query, IEnumerable<T> lookup, Func<T, string> getKey)
        {
            var orderedAndKeyed = lookup.Select(t => Tuple.Create(getKey(t), t)).OrderBy(t => t.Item1);
            var matches = new List<Predicate<string>> {
                k => k == query,
                k => k.StartWithCaseInsensitive(query),
                k => k.ContainsCaseInsensitive(query)
            };

            foreach (var matcher in matches)
            {
                var match = orderedAndKeyed.FirstOrDefault(t => matcher(t.Item1));
                if (match != default(Tuple<string, T>))
                {
                    return match.Item2;
                }
            }

            return default;
        }

        public static IEnumerable<StoreComponent> Stores => WorldObjectUtil.AllObjsWithComponent<StoreComponent>();

        public static string StoreCurrencyName(StoreComponent store)
        {
            return MessageUtils.StripTags(store.CurrencyName);
        }

        public static string GetMatchAndOffers(string searchName, out TradeTargetType offerType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers)
        {
            List<string> entries = new List<string>();

            IEnumerable<LookupEntry> lookup = ItemLookup.Concat(TagLookup).Concat(UserLookup).Concat(StoreLookup);
            LookupEntry match = BestMatchOrDefault(searchName, lookup, o =>
            {
                if (o.Is<Tag>())
                    return o.Get<Tag>().DisplayName;
                else if (o.Is<Item>())
                    return o.Get<Item>().DisplayName;
                else if (o.Is<User>())
                    return o.Get<User>().Name;
                else if (o.Is<StoreComponent>())
                    return o.Get<StoreComponent>().Parent.Name;
                else
                    return string.Empty;
            });

            string matchedName = string.Empty;
            offerType = TradeTargetType.Invalid;
            groupedBuyOffers = null;
            groupedSellOffers = null;
            if (match.Is<Tag>())
            {
                Tag matchTag = match.Get<Tag>();
                matchedName = matchTag.Name;

                bool filter(StoreComponent store, TradeOffer offer) => offer.Stack.Item.Tags().Contains(matchTag);
                groupedSellOffers = SellOffers(filter).GroupBy(t => StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(t => StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

                offerType = TradeTargetType.Tag;
            }
            else if (match.Is<Item>())
            {
                Item matchItem = match.Get<Item>();
                matchedName = matchItem.DisplayName;

                bool filter(StoreComponent store, TradeOffer offer) => offer.Stack.Item.TypeID == matchItem.TypeID;
                groupedSellOffers = SellOffers(filter).GroupBy(t => StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(t => StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

                offerType = TradeTargetType.Item;
            }
            else if (match.Is<User>())
            {
                User matchUser = match.Get<User>();
                matchedName = matchUser.Name;

                bool filter(StoreComponent store, TradeOffer offer) => store.Parent.Owners == matchUser;
                groupedSellOffers = SellOffers(filter).GroupBy(t => StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(t => StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

                offerType = TradeTargetType.User;
            }
            else if (match.Is<StoreComponent>())
            {
                StoreComponent matchStore = match.Get<StoreComponent>();
                matchedName = matchStore.Parent.Name;

                bool filter(StoreComponent store, TradeOffer offer) => store == matchStore;
                groupedSellOffers = SellOffers(filter).GroupBy(t => StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
                groupedBuyOffers = BuyOffers(filter).GroupBy(t => StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

                offerType = TradeTargetType.Store;
            }

            return matchedName;
        }

        public static IEnumerable<Tuple<StoreComponent, TradeOffer>>
            SellOffers(Func<StoreComponent, TradeOffer, bool> shouldIncludeFilter,
                       int start = 0,
                       int count = Int32.MaxValue)
        {
            return AllStoresToOffers(store => store.StoreData.SellOffers, shouldIncludeFilter, start, count);
        }

        public static IEnumerable<Tuple<StoreComponent, TradeOffer>>
            BuyOffers(Func<StoreComponent, TradeOffer, bool> shouldIncludeFilter,
                int start = 0,
                int count = Int32.MaxValue)
        {
            return AllStoresToOffers(store => store.StoreData.BuyOffers, shouldIncludeFilter, start, count);
        }

        public static IEnumerable<Tuple<StoreComponent, TradeOffer>>
            AllStoresToOffers(Func<StoreComponent, IEnumerable<TradeOffer>> storeToOffers,
                   Func<StoreComponent, TradeOffer, bool> shouldIncludeFilter,
                   int start = 0,
                   int count = Int32.MaxValue)
        {
            return Stores
                .SelectMany(store =>
                    storeToOffers(store)
                        .Where(offer => offer.IsSet && shouldIncludeFilter(store, offer))
                        .Select(offer => Tuple.Create(store, offer)))
                .OrderBy(t => t.Item2.Stack.Item.DisplayName)
                .Skip(start)
                .Take(count);
        }

        private static IEnumerable<Tag> FindTags()
        {
            List<Tag> uniqueTags = new List<Tag>();
            foreach (Item item in Item.AllItems)
            {
                foreach (Tag tag in item.Tags())
                {
                    if (!uniqueTags.Contains(tag))
                        uniqueTags.Add(tag);
                }
            }
            return uniqueTags;
        }
    }
}
