using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Plugins.Networking;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Handles commands coming from Discord.
     */
    public class DiscordDiscordCommands
    {
        public static DiscordColor EmbedColor = DiscordColor.Green;

        private string FirstNonEmptyString(params string[] strings)
        {
            return strings.FirstOrDefault(str => !String.IsNullOrEmpty(str)) ?? "";
        }

        public delegate Task DiscordCommand(CommandContext ctx);

        private static void LogCommandException(Exception e)
        {
            Logger.Error("Error occurred while attempting to run that command. Error message: " + e);
            Logger.Error(e.StackTrace);
        }

        [Command("ping")]
        [Description("Checks if the bot is online.")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync("Pong " + ctx.User.Mention);
        }

        [Command("ecostatus")]
        [Description("Retrieves the current status of the Eco Server")]
        public async Task EcoStatus(CommandContext ctx)
        {
            try
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null)
                {
                    await ctx.RespondAsync(
                        "The plugin was unable to be found on the server. Please report this to the plugin author.");
                    return;
                }

                var pluginConfig = plugin.DiscordPluginConfig;
                var serverInfo = NetworkManager.GetServerInfo();

                var name = FirstNonEmptyString(pluginConfig.ServerName, serverInfo.Name);
                var description = FirstNonEmptyString(pluginConfig.ServerDescription, serverInfo.Description,
                    "No server description is available.");

                var addr = IPUtil.GetInterNetworkIP().FirstOrDefault();
                var serverAddress = String.IsNullOrEmpty(pluginConfig.ServerIP)
                    ? addr == null ? "No Configured Address"
                    : addr + ":" + serverInfo.WebPort
                    : pluginConfig.ServerIP;

                var players = $"{serverInfo.OnlinePlayers}/{serverInfo.TotalPlayers}";

                var timeRemainingSpan = new TimeSpan(0, 0, (int) serverInfo.TimeLeft);
                var timeRemaining =
                    $"{timeRemainingSpan.Days} Days, {timeRemainingSpan.Hours} hours, {timeRemainingSpan.Minutes} minutes";

                var timeSinceStartSpan = new TimeSpan(0, 0, (int) serverInfo.TimeSinceStart);
                var timeSinceStart =
                    $"{timeSinceStartSpan.Days} Days, {timeSinceStartSpan.Hours} hours, {timeSinceStartSpan.Minutes} minutes";

                var leader = String.IsNullOrEmpty(serverInfo.Leader) ? "No leader" : serverInfo.Leader;

                var builder = new DiscordEmbedBuilder()
                    .WithColor(EmbedColor)
                    .WithTitle($"**{name} Server Status**")
                    .WithDescription(description)
                    .AddField("Online Players", players)
                    .AddField("Address", serverAddress)
                    .AddField("Time Left until Meteor", timeRemaining)
                    .AddField("Time Since Game Start", timeSinceStart)
                    .AddField("Current Leader", leader);

                //Add the Server Logo Thumbnail
                try
                {
                    builder = String.IsNullOrWhiteSpace(pluginConfig.ServerLogo)
                        ? builder
                        : builder.WithThumbnailUrl(pluginConfig.ServerLogo);
                }
                catch (UriFormatException e)
                {
                    Logger.Info("Warning: The Configured Server Logo is not a valid URL.");
                }


                await ctx.RespondAsync("Current status of the server:", false, builder.Build());
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }


        [Command("players")]
        [Description("Lists the players currently online on the server")]
        public async Task PlayerList(CommandContext ctx)
        {
            try
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null)
                {
                    await ctx.RespondAsync(
                        "The plugin was unable to be found on the server. Please report this to the plugin author.");
                    return;
                }

                var onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected)
                    .Select(user => user.Name);
                var numberOnline = onlineUsers.Count();
                var message = $"{numberOnline} Online Players:\n";
                var playerList = String.Join("\n", onlineUsers);
                var embed = new DiscordEmbedBuilder()
                    .WithColor(EmbedColor)
                    .WithTitle(message)
                    .WithDescription(playerList);

                await ctx.RespondAsync("Displaying Online Players", false, embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        #region Trades

        private static List<Either<Item, User>> _itemLookup = null;

        private static List<Either<Item, User>> ItemLookup =>
            _itemLookup == null
                ? Item.AllItems.Select(item => new Either<Item, User>(item)).ToList()
                : _itemLookup;

        [Command("trades")]
        [Description("Displays the latest trades by person or by item.")]
        [Aliases("trade","offers","offer")]
        public async Task Trades(CommandContext ctx,[Description("The player name or item name in question.")] string playerOrItem)
        {
            try
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null)
                {
                    await ctx.RespondAsync(
                        "The plugin was unable to be found on the server. Please report this to the plugin author.");
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithColor(EmbedColor)
                    .WithTitle("Trade Listings");

                var lookup = ItemLookup.Concat(UserManager.Users.Select(user => new Either<Item, User>(user)));
                
                var match = BestMatchOrDefault(playerOrItem, lookup, o =>
                {
                    if (o.Is<Item>()) return o.Get<Item>().FriendlyName;
                    return o.Get<User>().Name;
                });

                if (match.Is<Item>())
                {
                    var matchItem = match.Get<Item>();
                    embed.WithAuthor(matchItem.FriendlyName);
                    TradeOffersBuySell(plugin, embed, t => t.Item2.Stack.Item == matchItem, t => t.Item1.Parent.OwnerUser.Name);
                }
                else if (match.Is<User>())
                {
                    var matchUser = match.Get<User>();
                    embed.WithAuthor(matchUser.Name);
                    TradeOffersBuySell(plugin, embed, t => t.Item1.Parent.OwnerUser == matchUser, t => t.Item2.Stack.Item.FriendlyName);
                }
                else
                {
                    await ctx.RespondAsync(
                        "The player or item was not found.");
                    return;
                }

                await ctx.RespondAsync(null, false, embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        private void TradeOffersBuySell(DiscordLink plugin, DiscordEmbedBuilder embed, Func<Tuple<StoreComponent,TradeOffer>,bool> filter, Func<Tuple<StoreComponent,TradeOffer>,string> context)
        {
            var stores = WorldObjectManager.All.SelectMany(o => o.Components.OfType<StoreComponent>());

            var sellOffers = stores
                .SelectMany(s => s.SellOffers().Where(t => t.IsSet).Select(o => Tuple.Create(s, o)).Where(filter))
                .OrderBy(t => t.Item2.Stack.Item.FriendlyName).ToList();
            foreach (var offers in sellOffers.GroupBy(t => TradeStoreCurrencyName(plugin, t.Item1)).OrderBy(g => g.Key))
            {
                embed.AddField($"**Selling for {offers.Key}**",
                    TradeOffersFieldMessage(sellOffers,
                    t => t.Item2.Price.ToString(),
                    t => context(t),
                    t => t.Item2.Stack.Quantity)
                    , true);
            }

            var buyOffers = stores
                .SelectMany(s => s.BuyOffers().Where(t => t.IsSet).Select(o => Tuple.Create(s, o)).Where(filter))
                .OrderBy(t => t.Item2.Stack.Item.FriendlyName).ToList();
            foreach (var offers in buyOffers.GroupBy(t => TradeStoreCurrencyName(plugin, t.Item1)).OrderBy(g => g.Key))
            {
                embed.AddField($"**Buying with {offers.Key}**",
                    TradeOffersFieldMessage(buyOffers,
                    t => t.Item2.Price.ToString(),
                    t => context(t),
                    t => t.Item2.ShouldLimit ? (int?)t.Item2.Stack.Quantity : null)
                    , true);
            }
        }

        private string TradeOffersFieldMessage<T>(IEnumerable<T> offers, Func<T, string> getPrice, Func<T,string> getLabel, Func<T,int?> getQuantity)
        {
            return String.Join("\n", offers.Select(t =>
            {
                var price = getPrice(t);
                var quantity = getQuantity(t);
                var quantityString = quantity.HasValue ? $"{quantity.Value} - " : "";
                var line = $"{quantityString}${price} {getLabel(t)}";
                if (quantity == 0) line = $"~~{line}~~";
                return line;
            }));
        }

        private string TradeStoreCurrencyName(DiscordLink plugin, StoreComponent store)
        {
            return plugin.StripTags(store.Parent.GetComponent<CreditComponent>().CurrencyName);
        }

        #endregion Trades

        private T BestMatchOrDefault<T>(string query, IEnumerable<T> lookup, Func<T,string> getKey)
        {
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
    }
}
