using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Plugins.Networking;
using Eco.Shared.Utils;

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
        
        [Command("debug")]
        [Description("Runs the current debugging command.")]
        public async Task Debug(CommandContext ctx)
        {
            try
            {
                var iterable = new[] {1, 2, 3, 4, 5, 6, 7, 8, 9};
                var pagedEnumerable = new PagedEnumerable<int>(iterable, 2, val => 1);

                var output = "";
                
                var enumerator = pagedEnumerable.GetPagedEnumerator();
                enumerator.ForEachInPage(item => output += " " + item);
                output += " |~| ";
                enumerator.ForEachInPage(item => output += " " + item);
                output += " |~| ";
                enumerator.ForEachInPage(item => output += " " + item);
                output += " |~| ";
                enumerator.ForEachInPage(item => output += " " + item);
                output += " |~| ";
                enumerator.ForEachInPage(item => output += " " + item);
                enumerator.Dispose();

                    
                await ctx.RespondAsync(output);
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

        private static int EMBED_CONTENT_CHARACTER_LIMIT = 5000;
        private static int EMBED_FIELD_CHARACTER_LIMIT = 1000;

        private Dictionary<ulong, PagedEnumerator<Tuple<string, string>>> previousQueryEnumerator = 
            new Dictionary<ulong, PagedEnumerator<Tuple<string, string>>>();
        
        [Command("nextpage")]
        [Description("Continues onto the next page of a trade listing.")]
        [Aliases("continuetrades")]
        public async Task NextPageOfTrades(CommandContext ctx)
        {
            try
            {   
                var pagedFieldEnumerator = previousQueryEnumerator.GetOrDefault(ctx.Member.Id);
                //MoveNext() once, to see if we have ANY values. If not, we can say there's no more pages.
                if (pagedFieldEnumerator == null || !pagedFieldEnumerator.HasMorePages) { 
                    await ctx.RespondAsync("No further pages found");
                    return;
                }
                
                
                var embed = new DiscordEmbedBuilder()
                    .WithColor(EmbedColor)
                    .WithTitle("Trade Listings");

                while (pagedFieldEnumerator.MoveNext())
                {
                    embed.AddField(pagedFieldEnumerator.Current.Item1, pagedFieldEnumerator.Current.Item2, true);
                }

                await ctx.RespondAsync(null, false, embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        [Command("trades")]
        [Description("Displays the latest trades by person or by item.")]
        [Aliases("trade")]
        public async Task Trades(CommandContext ctx,[Description("The player name or item name in question.")] string itemNameOrUserName)
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

                var match = TradeHelper.MatchItemOrUser(itemNameOrUserName);

                if (match.Is<Item>())
                {
                    var matchItem = match.Get<Item>();
                    embed.WithAuthor(matchItem.FriendlyName);
                    previousQueryEnumerator[ctx.Member.Id] = TradeOffersBuySell(embed, (store, offer) => offer.Stack.Item == matchItem, t => t.Item1.Parent.OwnerUser.Name);
                }
                else if (match.Is<User>())
                {
                    var matchUser = match.Get<User>();
                    embed.WithAuthor(matchUser.Name);
                    previousQueryEnumerator[ctx.Member.Id] = TradeOffersBuySell(embed, (store, offer) => store.Parent.OwnerUser == matchUser, t => t.Item2.Stack.Item.FriendlyName);
                }
                else
                {
                    await ctx.RespondAsync(
                        "The player or item was not found.");
                    return;
                }

                var pagedEnumerator = previousQueryEnumerator[ctx.Member.Id];
                if (pagedEnumerator.HasMorePages)
                {
                    embed.WithFooter("More pages available. Use ?nextpage.");
                }

                await ctx.RespondAsync(null, false, embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        private IEnumerable<Tuple<string, string>> OffersToFields<T>(T buyOffers, T sellOffers, Func<Tuple<StoreComponent,TradeOffer>,string> context)
            where T : IEnumerable<IGrouping<string, Tuple<StoreComponent, TradeOffer>>>
        {   
            foreach(var group in sellOffers)
            {
                var body = TradeOffersFieldMessage(group,
                    t => t.Item2.Price.ToString(),
                    t => context(t),
                    t => t.Item2.Stack.Quantity);
                yield return Tuple.Create($"**Selling for {group.Key}**", body.Substring(0, body.Length > 1000? 1000 : body.Length));
            }
            foreach(var group in buyOffers)
            {
                var body = TradeOffersFieldMessage(group,
                    t => t.Item2.Price.ToString(),
                    t => context(t),
                    t => t.Item2.ShouldLimit ? (int?) t.Item2.Stack.Quantity : null);
                yield return Tuple.Create($"**Buying with {group.Key}**", body.Substring(0, body.Length > 1000? 1000 : body.Length));
            }
        }

        private PagedEnumerator<Tuple<string, string>> TradeOffersBuySell(DiscordEmbedBuilder embed, Func<StoreComponent,TradeOffer, bool> filter, Func<Tuple<StoreComponent,TradeOffer>,string> context)
        {
            var sellOffers = TradeHelper.SellOffers(filter);
            var groupedSellOffers = sellOffers.GroupBy(t => TradeHelper.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
            
            var buyOffers = TradeHelper.BuyOffers(filter);
            var groupedBuyOffers = buyOffers.GroupBy(t => TradeHelper.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

            var fieldEnumerator = OffersToFields(groupedBuyOffers, groupedSellOffers, context).GetEnumerator();
            
            var pagedFieldEnumerator = new PagedEnumerator<Tuple<string, string>>(fieldEnumerator, EMBED_CONTENT_CHARACTER_LIMIT, field => field.Item1.Length + field.Item2.Length);
            
            pagedFieldEnumerator.ForEachInPage(field => { embed.AddField(field.Item1, field.Item2, true); });

            return pagedFieldEnumerator;
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

        #endregion Trades
    }
}
