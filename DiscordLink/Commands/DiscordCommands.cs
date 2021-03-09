using DiscordLink.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Handles commands coming from Discord.
     */
    public class DiscordCommands : BaseCommandModule
    {
        public enum PermissionType
        {
            User,
            Admin
        }

        public delegate Task DiscordCommandFunction(CommandContext ctx, params string[] args);

        private static async Task CallWithErrorHandling<TRet>(PermissionType requiredPermission, DiscordCommandFunction toCall, CommandContext ctx, params string[] args)
        {
            try
            {
                if (!IsCommandAllowedInChannel(ctx))
                {
                    string commandChannels = string.Join("\n- ", DLConfig.Data.DiscordCommandChannels.Where(channel => channel.IsValid()).Select(channel => channel.DiscordChannel));
                    await RespondToCommand(ctx, $"You aren't allowed to post commands in this channel.\nCommands are allowed in the following channels:\n```- {commandChannels}```");
                    return;
                }

                if (!IsCommandAllowedForUser(ctx, requiredPermission))
                {
                    string permittedRolesDesc = (DLConfig.Data.AdminRoles.Count > 0) ? string.Join("\n- ", DLConfig.Data.AdminRoles.ToArray()) : "No admin roles configured";
                    await RespondToCommand(ctx, $"You lack the `{requiredPermission}` level permission required to execute this command.\nThe permitted roles are:\n```- {permittedRolesDesc}```");
                    return;
                }

                await toCall(ctx, args);
            }
            catch (Exception e)
            {
                Logger.Error("An error occurred while attempting to execute a Discord command. Error message: " + e);
                await RespondToCommand(ctx, "An error occurred while attempting to run that command. Error message: " + e);
            }
        }

        private static async Task RespondToCommand(CommandContext ctx, string fullTextContent, DiscordLinkEmbed embedContent = null)
        {
            async static Task Respond(CommandContext ctx, string textContent, DiscordLinkEmbed embedContent)
            {
                // If needed; split the message into multiple parts
                ICollection<string> stringParts = MessageUtil.SplitStringBySize(textContent, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT);
                ICollection<DiscordEmbed> embedParts = MessageUtil.BuildDiscordEmbeds(embedContent);

                if (stringParts.Count <= 1 && embedParts.Count <= 1)
                {
                    DiscordEmbed embed = (embedParts.Count >= 1) ? embedParts.First() : null;
                    await ctx.RespondAsync(textContent, isTTS: false, embed);
                }
                else
                {
                    // Either make sure we have permission to use embeds or convert the embed to text
                    foreach (string textMessagePart in stringParts)
                    {
                        await ctx.RespondAsync(textMessagePart, isTTS: false, null);
                    }
                    foreach (DiscordEmbed embedPart in embedParts)
                    {
                        await ctx.RespondAsync(null, isTTS: false, embedPart);
                    }
                }
            }

            try
            {
                if (embedContent == null)
                {
                    await Respond(ctx, fullTextContent, embedContent);
                }
                else
                {
                    // Either make sure we have permission to use embeds or convert the embed to text
                    if (DiscordUtil.ChannelHasPermission(ctx.Channel, Permissions.EmbedLinks))
                    {
                        await Respond(ctx, fullTextContent, embedContent);
                    }
                    else
                    {
                        await Respond(ctx, MessageBuilder.Discord.EmbedToText(fullTextContent, embedContent), embedContent);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("An error occurred while attempting to respond to command. Error message: " + e);
            }
        }

        private static bool IsCommandAllowedForUser(CommandContext ctx, PermissionType requiredPermission)
        {
            switch(requiredPermission)
            {
                case PermissionType.User:
                    return true;

                case PermissionType.Admin:
                    foreach(string adminRole in DLConfig.Data.AdminRoles)
                    {
                        if (ctx.Member.Roles.Any(role => role.Name.ToLower() == adminRole.ToLower()))
                            return true;
                    }
                break;
            }
            return false;
        }

        private static bool IsCommandAllowedInChannel(CommandContext ctx)
        {
            var commandChannels = DLConfig.Data.DiscordCommandChannels;
            bool allowed = ctx.Channel.IsPrivate || !(commandChannels.Any(link => link.IsValid())); // Always allow if there are no valid command channels or the command is sent via DM
            if(!allowed)
            {
                foreach(string adminRole in DLConfig.Data.AdminRoles)
                {
                    if (ctx.Member.Roles.Any(role => role.Name.ToLower() == adminRole.ToLower()))
                    {
                        allowed = true;
                        break;
                    }
                }
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

        // Admin commands

        [Command("Print")]
        [Description("Reposts the inputted message. Can be used to create tags for ordering display tags within a channel.")]
        public async Task Print(CommandContext ctx, [Description("The message to print.")] string message)
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, message);
            }, ctx);
        }

        [Command("echo")]
        [Description("Sends the provided message to Eco and back to Discord again.")]
        public async Task Echo(CommandContext ctx, [Description("The message to send and then receive back again. A random message will be sent if this parameter is omitted.")] string message = "", [Description("The eco channel you want to test.")] string ecoChannel = "")
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(message))
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

                if (string.IsNullOrWhiteSpace(ecoChannel))
                    ecoChannel = DLConstants.DEFAULT_CHAT_CHANNEL;

                string formattedMessage = $"#{ecoChannel} {DLConstants.ECHO_COMMAND_TOKEN} {message}";
                ChatManager.SendChat(formattedMessage, plugin.EcoUser);
            }, ctx);
        }

        [Command("Restart")]
        [Description("Restarts the plugin.")]
        [Aliases("dl-restart")]
        public async Task Restart(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                DiscordLink plugin = DiscordLink.Obj;
                string result = plugin.CanRestart ? "Restarting..." : "Restarting is not possible at this time";
                Logger.Info($"Restart command executed - {result}");
                await RespondToCommand(ctx, result);
                await plugin.RestartClient();
            }, ctx);
        }

        [Command("SendServerMessage")]
        [Description("Sends an Eco server message to a specified user")]
        [Aliases("dl-servermessage")]
        public async Task SendServerMessage(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Name of the recipient Eco user.")] string recipientUserName,
            [Description("Persistance type. Possible values are \"Temporary\" and \"Permanent\". Defaults to \"Temporary\".")] string persistanceType = "temporary")
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendServerMessage(message, ctx.GetSenderName(), recipientUserName, persistanceType));
            }, ctx);
        }

        [Command("BroadcastServerMessage")]
        [Description("Sends an Eco server message to all online users")]
        [Aliases("dl-broadcastservermessage")]
        public async Task BroadcastServerMessage(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Persistance type. Possible values are \"Temporary\" and \"Permanent\". Defaults to \"Temporary\".")] string persistanceType = "temporary")
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendServerMessage(message, ctx.GetSenderName(), string.Empty, persistanceType));
            }, ctx);
        }

        [Command("SendPopup")]
        [Description("Sends an Eco popup message to a specified user")]
        [Aliases("dl-popup")]
        public async Task SendPopup(CommandContext ctx, [Description("The message to send.")] string message,
            [Description("Name of the recipient Eco user.")] string recipientUserName)
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendPopup(message, ctx.GetSenderName(), recipientUserName));
            }, ctx);
        }

        [Command("BroadcastPopup")]
        [Description("Sends an Eco popup message to all online users")]
        [Aliases("dl-broadcastpopup")]
        public async Task BroadcastPopup(CommandContext ctx, [Description("The message to send.")] string message)
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendPopup(message, ctx.GetSenderName(), string.Empty));
            }, ctx);
        }

        [Command("SendAnnouncement")]
        [Description("Sends an Eco announcement message")]
        [Aliases("dl-announcement")]
        public async Task SendAnnouncement(CommandContext ctx, [Description("The title for the announcement UI.")] string title,
            [Description("The message to display in the announcement UI.")] string message,
            [Description("Name of the recipient Eco user.")] string recipientUserName)
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendAnnouncement(title, message, ctx.GetSenderName(), recipientUserName));
            }, ctx);
        }

        [Command("BroadcastAnnouncement")]
        [Description("Sends an Eco announcement message to all online users")]
        [Aliases("dl-broadcastannouncement")]
        public async Task SendAnnouncement(CommandContext ctx, [Description("The title for the announcement UI.")] string title,
            [Description("The message to display in the announcement UI.")] string message)
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.SendAnnouncement(title, message, ctx.GetSenderName(), string.Empty));
            }, ctx);
        }

        [Command("ResetWorldData")]
        [Description("Resets world data as if a new world had been created.")]
        [Aliases("dl-resetdata")]
        public async Task ResetData(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, SharedCommands.ResetWorldData());
            }, ctx);
        }

        [Command("pluginstatus")]
        [Description("Shows the plugin status.")]
        [Aliases("dl-status", "status")]
        public async Task PluginStatus(CommandContext ctx)
        {
            PermissionType a = PermissionType.Admin;
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, MessageBuilder.Shared.GetDisplayString(verbose: false));
            }, ctx);
        }

        [Command("pluginstatusverbose")]
        [Description("Shows the plugin status including verbose debug level information.")]
        [Aliases("dl-statusverbose", "statusverbose")]
        public async Task PluginStatusVerbose(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, MessageBuilder.Shared.GetDisplayString(verbose: true));
            }, ctx);
        }

        // User commands

        [Command("playerlist")]
        [Description("Lists the players currently online on the server.")]
        [Aliases("players", "dl-players")]
        public async Task PlayerList(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(PermissionType.User, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                .WithTitle("Players")
                .WithDescription(MessageBuilder.Shared.GetPlayerList());
                await RespondToCommand(ctx, "Displaying Online Players", embed);
            }, ctx);
        }

        [Command("Invite")]
        [Description("Posts the Discord invite message to the target user.")]
        [Aliases("dl-invite")]
        public async Task Invite(CommandContext ctx, [Description("The Eco username of the user receiving the invite")] string targetUserName)
        {
            await CallWithErrorHandling<object>(PermissionType.User, async (lCtx, args) =>
            {
                string result = string.Empty;
                User targetUser = EcoUtil.GetOnlineUserbyName(targetUserName);
                if (targetUser != null)
                {
                    result = SharedCommands.DiscordInvite(targetUser);
                }
                else
                {
                    User offlineUser = EcoUtil.GetUserbyName(targetUserName);
                    if (offlineUser != null)
                        result = $"{MessageUtil.StripTags(offlineUser.Name)} is not online";
                    else
                        result = $"Could not find user with name {targetUserName}";
                }

                await RespondToCommand(ctx, result);
            }, ctx);
        }

        [Command("BroadcastInvite")]
        [Description("Posts the Discord invite message to the Eco chat.")]
        [Aliases("dl-broadcastinvite")]
        public async Task BroadcastInvite(CommandContext ctx, [Description("The Eco channel in which to post the invite message")] string ecoChannel = "")
        {
            await CallWithErrorHandling<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                string result = SharedCommands.BroadcastDiscordInvite(ecoChannel);
                await RespondToCommand(ctx, result);
            }, ctx);
        }

        [Command("DiscordLinkAbout")]
        [Description("Posts a message describing what the DiscordLink plugin is.")]
        [Aliases("dl-about")]
        public async Task About(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(PermissionType.User, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                .WithTitle("About DiscordLink")
                .WithDescription(MessageBuilder.Shared.GetAboutMessage());

                await RespondToCommand(ctx, "About DiscordLink", embed);
            }, ctx);
        }

        [Command("ping")]
        [Description("Checks if the bot is online.")]
        public async Task Ping(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(PermissionType.User, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, "Pong " + ctx.User.Mention);
            }, ctx);
        }

        [Command("serverstatus")]
        [Description("Prints the Server Info status.")]
        [Aliases("dl-ecostatus", "dl-serverinfo", "ecostatus")]
        public async Task ServerStatus(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(PermissionType.User, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, "", MessageBuilder.Discord.GetServerInfo(MessageBuilder.ServerInfoComponentFlag.All));
            }, ctx);
        }

        [Command("VerifyLink")]
        [Description("Verifies that an unverified link is correct and should be used")]
        [Aliases("dl-verifylink")]
        public async Task VerifyLink(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(PermissionType.User, async (lCtx, args) =>
            {
                if (LinkedUserManager.VerifyLinkedUser(ctx.GetSenderId()))
                    await RespondToCommand(ctx, $"Link verified");
                else
                    await RespondToCommand(ctx, $"There is no outstanding link request to verify for your account");
            }, ctx);
        }

        [Command("trades")]
        [Description("Displays available trades by player or item.")]
        [Aliases("dl-trades", "dl-trade", "trade", "dlt")]
        public async Task Trades(CommandContext ctx, [Description("The player name or item name for which to display trades.")] string userOrItemName = "")
        {
            await CallWithErrorHandling<object>(PermissionType.User, async (lCtx, args) =>
            {
                // Fetch trade data
                string result = SharedCommands.Trades(userOrItemName, out string matchedName, out bool isItem, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
                if (!string.IsNullOrEmpty(result))
                {
                    // Report commmand error
                    await RespondToCommand(ctx, result);
                    return;
                }

                DiscordLinkEmbed embedContent;
                MessageBuilder.Discord.FormatTrades(matchedName, isItem, groupedBuyOffers, groupedSellOffers, out embedContent);
                await RespondToCommand(ctx, null, embedContent);
            }, ctx);
        }

        [Command("TrackTrades")]
        [Description("Creates a live updated display of available trades by player or item.")]
        [Aliases("dl-tracktrades")]
        public async Task TrackTrades(CommandContext ctx, [Description("The player name or item name for which to display trades.")] string userOrItemName = "")
        {
            await CallWithErrorHandling<object>(PermissionType.User, async (lCtx, args) =>
            {
                // Ensure that the calling user is linked
                if (LinkedUserManager.LinkedUserByDiscordId(ctx.GetSenderId()) == null)
                {
                    await RespondToCommand(ctx, $"You have not linked your Discord Account to DiscordLink on this Eco Server.\nLog into the game and use the `{DLConfig.Data.DiscordCommandPrefix}dl-link` command to initialize account linking.");
                    return;
                }

                int trackedTradesCount = DLStorage.WorldData.GetTrackedTradesCountForUser(ctx.GetSenderId());
                if (trackedTradesCount >= DLConfig.Data.MaxTrackedTradesPerUser)
                {
                    await RespondToCommand(ctx, $"You are already tracking {trackedTradesCount} trades and the limit is {DLConfig.Data.MaxTrackedTradesPerUser} tracked trades per user.\n\nUse the `{DLConfig.Data.DiscordCommandPrefix}dl-StopTrackTrades` command to remove a tracked trade to make space if you wish to add a new one.");
                    return;
                }

                // Fetch trade data using the trades command once to see that the command parameters are valid
                string result = SharedCommands.Trades(userOrItemName, out string matchedName, out bool isItem, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
                if (!string.IsNullOrEmpty(result))
                {
                    await RespondToCommand(ctx, result);
                    return;
                }

                bool added = await DLStorage.WorldData.AddTrackedTradeItem(ctx.GetSenderId(), matchedName);
                result = added ? $"Tracking all trades for {matchedName}." : $"Failed to start tracking trades for {matchedName}";

                await RespondToCommand(ctx, result);
            }, ctx);
        }

        [Command("StopTrackTrades")]
        [Description("Removes the live updated display of available trades for the player or item.")]
        [Aliases("dl-stoptracktrades")]
        public async Task StopTrackTrades(CommandContext ctx, [Description("The player name or item name for which to display trades.")] string userOrItemName = "")
        {
            await CallWithErrorHandling<object>(PermissionType.User, async (lCtx, args) =>
            {
                // Ensure that the calling user is linked
                if (LinkedUserManager.LinkedUserByDiscordId(ctx.GetSenderId()) == null)
                {
                    await RespondToCommand(ctx, $"You have not linked your Discord Account to DiscordLink on this Eco Server.\nLog into the game and use the `{DLConfig.Data.DiscordCommandPrefix}dl-link` command to initialize account linking.");
                    return;
                }

                bool removed = await DLStorage.WorldData.RemoveTrackedTradeItem(ctx.GetSenderId(), userOrItemName);
                string result = removed ? $"Stopped tracking trades for {userOrItemName}." : $"Failed to stop tracking trades for {userOrItemName}.\nUse `{DLConfig.Data.DiscordCommandPrefix}dl-ListTrackedStores` to see what is currently being tracked.";

                await RespondToCommand(ctx, result);
            }, ctx);
        }

        [Command("ListTrackedTrades")]
        [Description("Lists all tracked trades for the calling user.")]
        [Aliases("dl-listtrackedtrades")]
        public async Task ListTrackedTrades(CommandContext ctx)
        {
            await CallWithErrorHandling<object>(PermissionType.User, async (lCtx, args) =>
            {
                if (LinkedUserManager.LinkedUserByDiscordId(ctx.GetSenderId()) == null)
                {
                    await RespondToCommand(ctx, $"You have not linked your Discord Account to DiscordLink on this Eco Server.\nLog into the game and use the `{DLConfig.Data.DiscordCommandPrefix}dl-link` command to initialize account linking.");
                    return;
                }

                await RespondToCommand(ctx, DLStorage.WorldData.ListTrackedTrades(ctx.GetSenderId()));
            }, ctx);
        }
    }
}
