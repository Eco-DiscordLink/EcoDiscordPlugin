using System;
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

                var stores = WorldObjectManager.All.SelectMany(o => o.Components.OfType<StoreComponent>());
                var lookupItem = Item.AllItems.FirstOrDefault(i => i.FriendlyName.ToLower() == playerOrItem.ToLower());
                if (lookupItem == default(Item))
                {
                    var user = UserManager.Users.OrderBy(u=>u.Name).FirstOrDefault(u=>u.Name.ToLower().StartsWith(playerOrItem.ToLower()));
                    if (user == default(User))
                    {
                        await ctx.RespondAsync(
                        "The player or item was not found.");
                        return;
                    }

                    embed.WithAuthor(user.Name);

                    var sellOffers = stores.Where(s => s.Parent.OwnerUser == user)
                        .SelectMany(s => s.SellOffers().Where(t => t.IsSet).Select(o => Tuple.Create(s, o)))
                        .OrderBy(t => t.Item2.Stack.Item.FriendlyName).ToList();
                    if (sellOffers.Count > 0)
                    {
                        embed.AddField("**Sell Offers**",
                            TradeOffersFieldMessage(sellOffers,
                            t => null,
                            t => t.Item2.Stack.Item.FriendlyName,
                            t => t.Item2.Stack.Quantity)
                            , true);
                    }

                    var buyOffers = stores.Where(s => s.Parent.OwnerUser == user)
                        .SelectMany(s=>s.BuyOffers().Where(t => t.IsSet).Select(o => Tuple.Create(s, o)))
                        .OrderBy(t => t.Item2.Stack.Item.FriendlyName).ToList();
                    if (buyOffers.Count > 0)
                    {
                        embed.AddField("**Buy Offers**",
                            TradeOffersFieldMessage(buyOffers,
                            t => null,
                            t => t.Item2.Stack.Item.FriendlyName,
                            t => t.Item2.ShouldLimit ? (int?)t.Item2.Stack.Quantity : null)
                            ,true);
                    }
                }
                else
                {
                    embed.WithAuthor(lookupItem.FriendlyName);

                    var sellOffers = stores
                        .SelectMany(s => s.SellOffers().Where(t => t.IsSet && t.Stack.Item == lookupItem).Select(o => Tuple.Create(s, o)))
                        .OrderBy(t => t.Item2.Stack.Item.FriendlyName).ToList();
                    if (sellOffers.Count > 0)
                    {
                        embed.AddField("**Sell Offers**",
                            TradeOffersFieldMessage(sellOffers,
                            t => null,
                            t => t.Item1.Parent.OwnerUser.Name,
                            t => t.Item2.Stack.Quantity)
                            , true);
                    }

                    var buyOffers = stores
                        .SelectMany(s => s.BuyOffers().Where(t => t.IsSet && t.Stack.Item == lookupItem).Select(o => Tuple.Create(s, o)))
                        .OrderBy(t => t.Item2.Stack.Item.FriendlyName).ToList();
                    if (buyOffers.Count > 0)
                    {
                        embed.AddField("**Buy Offers**",
                            TradeOffersFieldMessage(buyOffers,
                            t => null,
                            t => t.Item1.Parent.OwnerUser.Name,
                            t => t.Item2.ShouldLimit ? (int?)t.Item2.Stack.Quantity : null)
                            , true);
                    }
                }

                await ctx.RespondAsync(null, false, embed);
            }
            catch (Exception e)
            {
                LogCommandException(e);
            }
        }

        private string TradeOffersFieldMessage<T>(IEnumerable<T> offers, Func<T, string> getPrice, Func<T,string> getLabel, Func<T,int?> getQuantity)
        {
            return String.Join("\n", offers.Select(t =>
            {
                var price = getPrice(t);
                var priceString = String.IsNullOrWhiteSpace(price) ? "" : $"{price} - ";
                var quantity = getQuantity(t);
                var quantityString = quantity.HasValue ? $"({quantity.Value})" : "";
                var line = $"{priceString}{quantityString} {getLabel(t)}";
                if (quantity == 0) line = $"~~{line}~~";
                return line;
            }));
        }

        #endregion Trades
    }
}
