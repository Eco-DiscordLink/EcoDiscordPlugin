using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Eco.Core.Utils;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Handles commands coming from Discord.
     */
    public class DiscordCommands : BaseCommandModule
    {
        public delegate Task DiscordCommand(CommandContext ctx);

        private static void LogCommandException(Exception e)
        {
            Logger.Error("Error occurred while attempting to run that command. Error message: " + e);
        }

        private async Task RespondToCommand(CommandContext ctx, string textContent, DiscordEmbed embedContent = null)
        {
            try
            {
                if (embedContent == null)
                {
                    await ctx.RespondAsync(textContent, false);
                }
                else
                {
                    // Either make sure we have permission to use embeds or convert the embed to text
                    if (DiscordUtil.ChannelHasPermission(ctx.Channel, Permissions.EmbedLinks))
                    {
                        await ctx.RespondAsync(textContent, false, embedContent);
                    }
                    else
                    {
                        await ctx.RespondAsync(MessageBuilder.EmbedToText(textContent, embedContent), false);
                    }
                }
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        [Command("ping")]
        [Description("Checks if the bot is online.")]
        public async Task Ping(CommandContext ctx)
        {
            await RespondToCommand(ctx, "Pong " + ctx.User.Mention);
        }

        [Command("ecostatus")]
        [Description("Retrieves the current status of the Eco Server.")]
        public async Task EcoStatus(CommandContext ctx)
        {
            await RespondToCommand(ctx, "", MessageBuilder.GetServerInfo(MessageBuilder.ServerInfoComponentFlag.All));
        }

#if DEBUG
        [Command("debug")]
        [Description("Runs the current debugging command.")]
        public async Task Debug(CommandContext ctx)
        {
            try
            {
                var iterable = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
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

                await RespondToCommand(ctx, output);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }
#endif

        [Command("echo")]
        [Description("Sends the provided message to Eco and back to Discord again.")]
        public Task Echo(CommandContext _, [Description("The message to send and then receive back again. A random message will be sent if this parameter is omitted.")] string message = "")
        {
            try
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null)
                {
                    Task.FromResult(0);
                }

                if (message.IsEmpty())
                {
                    Random rnd = new Random();
                    switch (rnd.Next(1, 5))
                    {
                        case 1:
                            message = "One thing has suddenly ceased to lead to another.";
                            break;

                        case 2:
                            message = "Nothing travels faster than the speed of light with the possible exception of bad news, which obeys its own special laws.";
                            break;

                        case 3:
                            message = "Life... is like a grapefruit. It's orange and squishy, and has a few pips in it, and some folks have half a one for breakfast.";
                            break;

                        case 4:
                            message = "So long and thanks for all the fish.";
                            break;

                        case 5:
                            message = "Time is an illusion. Lunch-time doubly so.";
                            break;
                    }
                }

                string formattedMessage = $"#{DLConfig.Data.EcoCommandChannel} {DiscordLink.EchoCommandToken + " " + message}";
                ChatManager.SendChat(formattedMessage, plugin.EcoUser);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }

            return Task.FromResult(0);
        }

        [Command("players")]
        [Description("Lists the players currently online on the server.")]
        public async Task PlayerList(CommandContext ctx)
        {
            try
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(MessageBuilder.EmbedColor)
                .WithTitle("Players")
                .WithDescription(MessageBuilder.GetPlayerList());
                await RespondToCommand(ctx, "Displaying Online Players", embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        [Command("DiscordInvite")]
        [Description("Posts the Discord invite message to the Eco chat.")]
        [Aliases("dl-invite")]
        public async Task DiscordInvite(CommandContext ctx)
        {
            try
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null)
                {
                    return;
                }

                var config = DLConfig.Data;
                var serverInfo = Networking.NetworkManager.GetServerInfo();

                // Send to Eco
                string inviteMessage = config.InviteMessage;
                if (!inviteMessage.Contains(DLConfig.InviteCommandLinkToken) || string.IsNullOrEmpty(serverInfo.DiscordAddress))
                {

                    await RespondToCommand(ctx, "This server is not configured for using the " + config.DiscordCommandPrefix + "DiscordInvite command.");
                    return;
                }

                inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConfig.InviteCommandLinkToken), serverInfo.DiscordAddress);
                string formattedInviteMessage = $"#{config.EcoCommandChannel} {inviteMessage}";
                ChatManager.SendChat(formattedInviteMessage, plugin.EcoUser);

                // Respond to Discord
                var embed = new DiscordEmbedBuilder()
                    .WithColor(MessageBuilder.EmbedColor)
                    .WithDescription(inviteMessage);

                await RespondToCommand(ctx, "Posted message to Eco channel #" + config.EcoCommandChannel, embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        [Command("DiscordLinkAbout")]
        [Description("Posts a message describing what the DiscordLink plugin is.")]
        [Aliases("dl-about")]
        public async Task About(CommandContext ctx)
        {
            try
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(MessageBuilder.EmbedColor)
                .WithTitle("About DiscordLink")
                .WithDescription(MessageBuilder.GetAboutMessage());

                await RespondToCommand(ctx, "About DiscordLink", embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        [Command("Print")]
        [Description("Reposts the inputted message. Can be used to create tags for ordering display tags within a channel.")]
        public async Task Print(CommandContext ctx, [Description("The message to print.")] string message)
        {
            try
            {
                await RespondToCommand(ctx, message);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        [Command("Restart")]
        [Description("Reposts the inputted message. Can be used to create tags for ordering display tags within a channel.")]
        [Aliases("dl-restart")]
        [RequireRoles(RoleCheckMode.Any, "Moderator")]
        public async Task Restart(CommandContext ctx)
        {
            try
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null) return;

                await RespondToCommand(ctx, "Restarting DiscordLink");
                _ = plugin.RestartClient();
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        #region Trades

        private readonly Dictionary<string, PagedEnumerator<Tuple<string, string>>> previousQueryEnumerator =
            new Dictionary<string, PagedEnumerator<Tuple<string, string>>>();

        [Command("nextpage")]
        [Description("Continues onto the next page of a trade listing.")]
        [Aliases("continuetrades")]
        public async Task NextPageOfTrades(CommandContext ctx)
        {
            try
            {
                var pagedFieldEnumerator = previousQueryEnumerator.GetOrDefault(ctx.User.UniqueUsername());
                if (pagedFieldEnumerator == null || !pagedFieldEnumerator.HasMorePages)
                {
                    await RespondToCommand(ctx, "No further trade pages found or no trade command executed");
                    return;
                }


                var embed = new DiscordEmbedBuilder()
                    .WithColor(MessageBuilder.EmbedColor)
                    .WithTitle("Trade Listings");

                while (pagedFieldEnumerator.MoveNext())
                {
                    embed.AddField(pagedFieldEnumerator.Current.Item1, pagedFieldEnumerator.Current.Item2, true);
                }

                await RespondToCommand(ctx, null, embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }


        [Command("trades")]
        [Description("Displays the latest trades by person or by item.")]
        [Aliases("trade")]
        public async Task Trades(CommandContext ctx, [Description("The player name or item name in question.")] string itemNameOrUserName = "")
        {
            try
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(itemNameOrUserName))
                {
                    await RespondToCommand(ctx,
                        "Please provide the name of an item or player to search for. " +
                        "Usage: trades <item name or player name>");
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithColor(MessageBuilder.EmbedColor)
                    .WithTitle("Trade Listings");

                var match = TradeUtil.MatchItemOrUser(itemNameOrUserName);

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
                    await RespondToCommand(ctx, "The player or item was not found.");
                    return;
                }

                var pagedEnumerator = previousQueryEnumerator[ctx.User.UniqueUsername()];
                if (pagedEnumerator.HasMorePages)
                {
                    embed.WithFooter("More pages available. Use " + DiscordLink.EchoCommandToken + "nextpage to show.");
                }

                await RespondToCommand(ctx, null, embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        private IEnumerable<Tuple<string, string>> OffersToFields<T>(T buyOffers, T sellOffers, Func<Tuple<StoreComponent, TradeOffer>, string> context)
            where T : IEnumerable<IGrouping<string, Tuple<StoreComponent, TradeOffer>>>
        {
            foreach (var group in sellOffers)
            {
                var offer_descriptions = TradeOffersToDescriptions(group,
                    t => t.Item2.Price.ToString(),
                    t => context(t),
                    t => t.Item2.Stack.Quantity);
                var enumerator = new PagedEnumerable<string>(offer_descriptions, DiscordUtil.EMBED_FIELD_CHARACTER_LIMIT, str => str.Length).GetPagedEnumerator();
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
            foreach (var group in buyOffers)
            {
                var offer_descriptions = TradeOffersToDescriptions(group,
                    t => t.Item2.Price.ToString(),
                    t => context(t),
                    t => t.Item2.Stack.Quantity);
                var enumerator = new PagedEnumerable<string>(offer_descriptions, DiscordUtil.EMBED_FIELD_CHARACTER_LIMIT, str => str.Length).GetPagedEnumerator();
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

        private PagedEnumerator<Tuple<string, string>> TradeOffersBuySell(DiscordEmbedBuilder embed, Func<StoreComponent, TradeOffer, bool> filter, Func<Tuple<StoreComponent, TradeOffer>, string> context)
        {
            var sellOffers = TradeUtil.SellOffers(filter);
            var groupedSellOffers = sellOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

            var buyOffers = TradeUtil.BuyOffers(filter);
            var groupedBuyOffers = buyOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

            var fieldEnumerator = OffersToFields(groupedBuyOffers, groupedSellOffers, context).GetEnumerator();

            var pagedFieldEnumerator = new PagedEnumerator<Tuple<string, string>>(fieldEnumerator, DiscordUtil.EMBED_CONTENT_CHARACTER_LIMIT, field => field.Item1.Length + field.Item2.Length);

            pagedFieldEnumerator.ForEachInPage(field => { embed.AddField(field.Item1, field.Item2, true); });

            return pagedFieldEnumerator;
        }

        private IEnumerable<string> TradeOffersToDescriptions<T>(IEnumerable<T> offers, Func<T, string> getPrice, Func<T, string> getLabel, Func<T, int?> getQuantity)
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
