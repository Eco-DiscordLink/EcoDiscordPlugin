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
        public delegate Task DiscordCommandFunction(CommandContext ctx, params string[] args);

        private static async Task CallWithErrorHandling<TRet>(DiscordCommandFunction toCall, CommandContext ctx, params string[] args)
        {
            try
            {
                if (!IsCommandAllowedInChannel(ctx))
                    return;

                await toCall(ctx, args);
            }
            catch (Exception e)
            {
                Logger.Error("An error occurred while attempting to execute a Discord command. Error message: " + e);
                await RespondToCommand(ctx, "An error occurred while attempting to run that command. Error message: " + e);
            }
        }

        private static async Task RespondToCommand(CommandContext ctx, string textContent, DiscordEmbed embedContent = null)
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
                Logger.Error("An error occurred while attempting to respond to command. Error message: " + e);
            }
        }

        private static bool IsCommandAllowedInChannel(CommandContext ctx)
        {
            var commandChannels = DLConfig.Data.DiscordCommandChannels;
            bool allowed = ctx.Channel.IsPrivate;
            if (!allowed)
            {
                allowed = commandChannels.Count <= 0                        // Always allow if there are no command channels
               || ctx.Member.IsOwner                                        // Always allow if the user is the server owner
               || ctx.Member.Roles.Any(role => role.Name == "Moderator");   // Always allow if the user is a moderator
            }

            // Check if the discord channel used is listed as a command channel
            if (!allowed)
            {
                string channelNameLower = ctx.Channel.Name.ToLower();
                foreach (ChannelLink link in commandChannels)
                {
                    if (channelNameLower == link.DiscordChannel)
                    {
                        allowed = true;
                        break;
                    }
                }
            }

            return allowed;
        }

        [Command("ping")]
        [Description("Checks if the bot is online.")]
        public async Task Ping(CommandContext ctx)
        {
            await CallWithErrorHandling<object>( async (lCtx, args) =>
            {
                await RespondToCommand(ctx, "Pong " + ctx.User.Mention);
            }, ctx);
        }

        [Command("ecostatus")]
        [Description("Prints the Server Info status.")]
        public async Task EcoStatus(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                await RespondToCommand(ctx, "", MessageBuilder.GetServerInfo(MessageBuilder.ServerInfoComponentFlag.All));
            }, ctx);
        }

        [Command("echo")]
        [Description("Sends the provided message to Eco and back to Discord again.")]
        public async Task Echo(CommandContext ctx, [Description("The message to send and then receive back again. A random message will be sent if this parameter is omitted.")] string message = "")
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null)
                {
                    return;
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

                string formattedMessage = $"#{DLConfig.Data.EcoCommandOutputChannel} {DLConstants.ECHO_COMMAND_TOKEN + " " + message}";
                ChatManager.SendChat(formattedMessage, plugin.EcoUser);
            }, ctx);
        }

        [Command("players")]
        [Description("Lists the players currently online on the server.")]
        public async Task PlayerList(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(MessageBuilder.EmbedColor)
                .WithTitle("Players")
                .WithDescription(MessageBuilder.GetPlayerList());
                await RespondToCommand(ctx, "Displaying Online Players", embed);
            }, ctx);
        }

        [Command("DiscordInvite")]
        [Description("Posts the Discord invite message to the Eco chat.")]
        [Aliases("dl-invite")]
        public async Task DiscordInvite(CommandContext ctx, [Description("The Eco channel in which to post the invite message")] string ecoChannel = "")
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                string result = SharedCommands.Invite(ecoChannel);
                await RespondToCommand(ctx,result);
            }, ctx);
        }

        [Command("DiscordLinkAbout")]
        [Description("Posts a message describing what the DiscordLink plugin is.")]
        [Aliases("dl-about")]
        public async Task About(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(MessageBuilder.EmbedColor)
                .WithTitle("About DiscordLink")
                .WithDescription(MessageBuilder.GetAboutMessage());

                await RespondToCommand(ctx, "About DiscordLink", embed);
            }, ctx);
        }

        [Command("Print")]
        [Description("Reposts the inputted message. Can be used to create tags for ordering display tags within a channel.")]
        public async Task Print(CommandContext ctx, [Description("The message to print.")] string message)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                await RespondToCommand(ctx, message);
            }, ctx);
        }

        [Command("Restart")]
        [Description("Restarts the plugin.")]
        [Aliases("dl-restart")]
        [RequireRoles(RoleCheckMode.Any, "Moderator")]
        public async Task Restart(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                string result = SharedCommands.Restart();
                await RespondToCommand(ctx, result);
            }, ctx);
        }

        [Command("SendServerMessage")]
        [Description("Sends an Eco server message to a specified user")]
        [Aliases("dl-servermessage")]
        [RequireRoles(RoleCheckMode.Any, "Moderator")]
        public async Task SendServerMessage(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Name of the recipient Eco user.")] string recipientUserName,
            [Description("Persistance type. Possible values are \"Temporary\" and \"Permanent\". Defaults to \"Temporary\".")] string persistanceType = "temporary")
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendServerMessage(message, ctx.GetSenderName(), recipientUserName, persistanceType));
            }, ctx);
        }

        [Command("BroadcastServerMessage")]
        [Description("Sends an Eco server message to all online users")]
        [Aliases("dl-servermessageall")]
        [RequireRoles(RoleCheckMode.Any, "Moderator")]
        public async Task BroadcastServerMessage(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Persistance type. Possible values are \"Temporary\" and \"Permanent\". Defaults to \"Temporary\".")] string persistanceType = "temporary")
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendServerMessage(message, ctx.GetSenderName(), string.Empty, persistanceType));
            }, ctx);
        }

        [Command("SendPopup")]
        [Description("Sends an Eco popup message to a specified user")]
        [Aliases("dl-popup")]
        [RequireRoles(RoleCheckMode.Any, "Moderator")]
        public async Task SendPopup(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Name of the recipient Eco user.")] string recipientUserName)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendPopup(message, ctx.GetSenderName(), recipientUserName));
            }, ctx);
        }

        [Command("BroadcastPopup")]
        [Description("Sends an Eco popup message to all online users")]
        [Aliases("dl-popupall")]
        [RequireRoles(RoleCheckMode.Any, "Moderator")]
        public async Task BroadcastPopup(CommandContext ctx, [Description("The message to send.")] string message)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendPopup(message, ctx.GetSenderName(), string.Empty));
            }, ctx);
        }

        [Command("SendAnnouncement")]
        [Description("Sends an Eco announcement message")]
        [Aliases("dl-announcement")]
        [RequireRoles(RoleCheckMode.Any, "Moderator")]
        public async Task SendAnnouncement(CommandContext ctx, [Description("The title for the announcement UI.")] string title,
            [Description("The message to display in the announcement UI.")] string message,
            [Description("Name of the recipient Eco user.")] string recipientUserName)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendAnnouncement(title, message, ctx.GetSenderName(), recipientUserName));
            }, ctx);
        }

        [Command("BroadcastAnnouncement")]
        [Description("Sends an Eco announcement message to all online users")]
        [Aliases("dl-announcementall")]
        [RequireRoles(RoleCheckMode.Any, "Moderator")]
        public async Task SendAnnouncement(CommandContext ctx, [Description("The title for the announcement UI.")] string title,
            [Description("The message to display in the announcement UI.")] string message)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendAnnouncement(title, message, ctx.GetSenderName(), string.Empty));
            }, ctx);
        }

        [Command("VerifyLink")]
        [Description("Verifies that an unverified link is correct and should be used")]
        [Aliases("dl-verifylink")]
        public async Task VerifyLink(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
            {
                // Find the linked user for the sender and mark them as verified
                LinkedUser user = LinkedUserManager.LinkedUserByDiscordId(ctx.GetSenderId().ToString(), false);
                if (user != null)
                {
                    user.Verified = true;
                    DLStorage.Instance.Write();
                    await RespondToCommand(ctx, $"Link verified");
                }
                else
                {
                    await RespondToCommand(ctx, $"There is no outstanding link request to verify for your account");
                }
            }, ctx);
        }

        #region Trades

        private readonly Dictionary<string, PagedEnumerator<Tuple<string, string>>> previousQueryEnumerator =
            new Dictionary<string, PagedEnumerator<Tuple<string, string>>>();

        [Command("nextpage")]
        [Description("Continues onto the next page of a trade listing.")]
        [Aliases("continuetrades")]
        public async Task NextPageOfTrades(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
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
            }, ctx);
        }


        [Command("trades")]
        [Description("Displays available trades by person or by item.")]
        [Aliases("trade")]
        public async Task Trades(CommandContext ctx, [Description("The player name or item name in question.")] string itemNameOrUserName = "")
        {
            await CallWithErrorHandling<object>(async (lCtx, args) =>
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
                    embed.WithFooter("More pages available. Use " + DLConfig.Data.DiscordCommandPrefix + "nextpage to show.");
                }

                await RespondToCommand(ctx, null, embed);
            }, ctx);
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
                var enumerator = new PagedEnumerable<string>(offer_descriptions, DLConstants.EMBED_FIELD_CHARACTER_LIMIT, str => str.Length).GetPagedEnumerator();
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
                var enumerator = new PagedEnumerable<string>(offer_descriptions, DLConstants.EMBED_FIELD_CHARACTER_LIMIT, str => str.Length).GetPagedEnumerator();
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

            var pagedFieldEnumerator = new PagedEnumerator<Tuple<string, string>>(fieldEnumerator, DLConstants.EMBED_CONTENT_CHARACTER_LIMIT, field => field.Item1.Length + field.Item2.Length);

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
