using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Plugins.Networking;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Handles commands coming from Discord.
     */
    public class DiscordDiscordCommands : BaseCommandModule
    {
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
                await ctx.RespondAsync("Current status of the server:", false, MessageBuilder.GetEcoStatus(MessageBuilder.EcoStatusComponentFlag.All));
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
                await ctx.RespondAsync("Displaying Online Players", false, MessageBuilder.GetPlayerList());
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        [Command("DiscordInvite")]
        [Description("Posts the Discord invite message to the Eco chat")]
        public async Task DiscordInvite(CommandContext ctx)
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

                var config = plugin.DiscordPluginConfig;
                var serverInfo = Networking.NetworkManager.GetServerInfo();

                // Send to Eco
                string inviteMessage = config.InviteMessage;
                if (!inviteMessage.Contains(DiscordLink.InviteCommandLinkToken) || string.IsNullOrEmpty(serverInfo.DiscordAddress))
                {
                    await ctx.RespondAsync(
                        "This server is not configured for using the " + config.DiscordCommandPrefix + "DiscordInvite command.");
                    return;
                }

                inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DiscordLink.InviteCommandLinkToken), serverInfo.DiscordAddress);
                string formattedInviteMessage = $"#{config.EcoCommandChannel} {inviteMessage}";
                ChatManager.SendChat(formattedInviteMessage, plugin.EcoUser);

                // Respond to Discord
                var embed = new DiscordEmbedBuilder()
                    .WithColor(MessageBuilder.EmbedColor)
                    .WithDescription(inviteMessage);

                await ctx.RespondAsync("Posted message to Eco channel #" + plugin.DiscordPluginConfig.EcoCommandChannel, false, embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        #region Trades

        private static int EMBED_CONTENT_CHARACTER_LIMIT = 5000;
        private static int EMBED_FIELD_CHARACTER_LIMIT = 900;

        private Dictionary<string, PagedEnumerator<Tuple<string, string>>> previousQueryEnumerator = 
            new Dictionary<string, PagedEnumerator<Tuple<string, string>>>();
        
        [Command("nextpage")]
        [Description("Continues onto the next page of a trade listing.")]
        [Aliases("continuetrades")]
        public async Task NextPageOfTrades(CommandContext ctx)
        {
            try
            {   
                var pagedFieldEnumerator = previousQueryEnumerator.GetOrDefault(ctx.User.UniqueUsername());
                //MoveNext() once, to see if we have ANY values. If not, we can say there's no more pages.
                if (pagedFieldEnumerator == null || !pagedFieldEnumerator.HasMorePages) { 
                    await ctx.RespondAsync("No further pages found");
                    return;
                }
                
                
                var embed = new DiscordEmbedBuilder()
                    .WithColor(MessageBuilder.EmbedColor)
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
        public async Task Trades(CommandContext ctx,[Description("The player name or item name in question.")] string itemNameOrUserName = "")
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

                if (itemNameOrUserName == "")
                {
                    await ctx.RespondAsync(
                        "Please provide the name of an item or player to search for. " +
                        "Usage: trades <item name or player name>");
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithColor(MessageBuilder.EmbedColor)
                    .WithTitle("Trade Listings");

                var match = TradeHelper.MatchItemOrUser(itemNameOrUserName);

                if (match.Is<Item>())
                {
                    var matchItem = match.Get<Item>();
                    embed.WithAuthor(matchItem.DisplayName);
                    previousQueryEnumerator[ctx.User.UniqueUsername()] = TradeOffersBuySell(embed, (store, offer) => offer.Stack.Item == matchItem, t => t.Item1.Parent.Owners.Name);
                }
                else if (match.Is<User>())
                {
                    var matchUser = match.Get<User>();
                    embed.WithAuthor(matchUser.Name);
                    previousQueryEnumerator[ctx.User.UniqueUsername()] = TradeOffersBuySell(embed, (store, offer) => store.Parent.Owners == matchUser, t => t.Item2.Stack.Item.DisplayName);
                }
                else
                {
                    await ctx.RespondAsync(
                        "The player or item was not found.");
                    return;
                }

                var pagedEnumerator = previousQueryEnumerator[ctx.User.UniqueUsername()];
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
                var offer_descriptions = TradeOffersToDescriptions(group,
                    t => t.Item2.Price.ToString(),
                    t => context(t),
                    t => t.Item2.Stack.Quantity);
                var enumerator = new PagedEnumerable<string>(offer_descriptions, EMBED_FIELD_CHARACTER_LIMIT, str => str.Length).GetPagedEnumerator();
                while (enumerator.HasMorePages)
                {
                    var fieldBodyBuilder = new StringBuilder();
                    while (enumerator.MoveNext())
                    {
                        fieldBodyBuilder.Append(enumerator.Current);
                        fieldBodyBuilder.Append("\n");
                    }
                    yield return Tuple.Create($"**Selling for {group.Key}**", fieldBodyBuilder.ToString());
                }
            }
            foreach(var group in buyOffers)
            {
                var offer_descriptions = TradeOffersToDescriptions(group,
                    t => t.Item2.Price.ToString(),
                    t => context(t),
                    t => t.Item2.Stack.Quantity);
                var enumerator = new PagedEnumerable<string>(offer_descriptions, EMBED_FIELD_CHARACTER_LIMIT, str => str.Length).GetPagedEnumerator();
                while (enumerator.HasMorePages)
                {
                    var fieldBodyBuilder = new StringBuilder();
                    while (enumerator.MoveNext())
                    {
                        fieldBodyBuilder.Append(enumerator.Current);
                        fieldBodyBuilder.Append("\n");
                    }
                    yield return Tuple.Create($"**Buying for {group.Key}**", fieldBodyBuilder.ToString());
                }
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

        private IEnumerable<string> TradeOffersToDescriptions<T>(IEnumerable<T> offers, Func<T, string> getPrice, Func<T,string> getLabel, Func<T,int?> getQuantity)
        {
            return offers.Select(t =>
            {
                var price = getPrice(t);
                var quantity = getQuantity(t);
                var quantityString = quantity.HasValue ? $"{quantity.Value} - " : "";
                var line = $"{quantityString}${price} {getLabel(t)}";
                if (quantity == 0) line = $"~~{line}~~";
                return line;
            });
        }

        #endregion Trades
    }
}
