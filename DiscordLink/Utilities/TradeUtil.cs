using System;
using System.Collections.Generic;
using System.Linq;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public class TradeUtil
    {
        private static List<Either<Item, User>> _itemLookup = null;

        public static List<Either<Item, User>> ItemLookup =>
            _itemLookup == null
                ? Item.AllItems.Select(item => new Either<Item, User>(item)).ToList()
                : _itemLookup;    
        
        public static T BestMatchOrDefault<T>(string rawQuery, IEnumerable<T> lookup, Func<T,string> getKey)
        {
            var query = rawQuery.ToLower();
            var orderedAndKeyed = lookup.Select(t => Tuple.Create(getKey(t).ToLower(), t)).OrderBy(t => t.Item1);

            var matches = new List<Predicate<string>> {
                k => k == query,
                k => k.StartsWith(query),
                k => k.Contains(query)
            };

            foreach (var matcher in matches)
            {
                var match = orderedAndKeyed.FirstOrDefault(t => matcher(t.Item1));
                if (match != default(Tuple<string,T>))
                {
                    return match.Item2;
                }
            }

            return default(T);
        }

        public static Either<Item, User> MatchItemOrUser(string itemNameOrPlayerName)
        {
            var lookup = ItemLookup.Concat(UserManager.Users.Select(user => new Either<Item, User>(user)));
            return BestMatchOrDefault(itemNameOrPlayerName, lookup, o =>
            {
                if (o.Is<Item>()) return o.Get<Item>().DisplayName;
                return o.Get<User>().Name;
            });
        }

        public static IEnumerable<StoreComponent> Stores => WorldObjectUtil.AllObjsWithComponent<StoreComponent>();

        public static string StoreCurrencyName(StoreComponent store)
        {
            return MessageUtil.StripEcoTags(store.Parent.GetComponent<CreditComponent>().CreditData.Currency.Name);
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
    }
}