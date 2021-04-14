using System;
using System.Collections.Generic;
using System.Linq;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public enum TradeTargetType
    {
        Tag,
        Item,
        User,
        Invalid,
    }

    public class TradeUtil
    {
        private static List<Either<Item, User, Tag>> _itemLookup = null;

        public static List<Either<Item, User, Tag>> ItemLookup =>
            _itemLookup ?? Item.AllItems.Select(item => new Either<Item, User, Tag>(item)).ToList();

        private static List<Either<Item, User, Tag>> _tagLookup = null;

        public static List<Either<Item, User, Tag>> TagLookup =>
            _tagLookup ??= FindTags().Select(tag => new Either<Item, User, Tag>(tag)).ToList();

        public static List<Either<Item, User, Tag>> UserLookup => UserManager.Users.Select(user => new Either<Item, User, Tag>(user)).ToList();

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

        public static Either<Item, User, Tag> MatchType(string name)
        {
            var lookup = ItemLookup.Concat(TagLookup).Concat(UserManager.Users.Select(user => new Either<Item, User, Tag>(user)));
            return BestMatchOrDefault(name, lookup, o =>
            {
                if (o.Is<Tag>())
                    return o.Get<Tag>().DisplayName;
                else if (o.Is<Item>())
                    return o.Get<Item>().DisplayName;
                else if (o.Is<User>())
                    return o.Get<User>().Name;
                else
                    return string.Empty;
            });
        }

        public static IEnumerable<StoreComponent> Stores => WorldObjectUtil.AllObjsWithComponent<StoreComponent>();

        public static string StoreCurrencyName(StoreComponent store)
        {
            return MessageUtil.StripTags(store.CurrencyName);
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
            foreach(Item item in Item.AllItems)
            {
                foreach(Tag tag in item.Tags())
                {
                    if (!uniqueTags.Contains(tag)) 
                        uniqueTags.Add(tag);
                }
            }
            return uniqueTags;
        }
    }
}
