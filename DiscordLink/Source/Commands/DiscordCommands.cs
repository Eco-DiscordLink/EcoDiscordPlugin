using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Eco.EW.Tools;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.IoC;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Eco.Plugins.DiscordLink.Enums;
using static Eco.Plugins.DiscordLink.SharedCommands;
using static Eco.Plugins.DiscordLink.Utilities.MessageBuilder;

namespace Eco.Plugins.DiscordLink
{
    public class DiscordCommands : ApplicationCommandModule
    {
        #region Commands Base

        public enum PermissionType
        {
            User,
            Admin
        }

        public delegate Task DiscordCommand(InteractionContext ctx, params string[] parameters);

        private static async Task ExecuteCommand<TRet>(PermissionType requiredPermission, DiscordCommand command, InteractionContext ctx, params string[] parameters)
        {
            try
            {
                if (!IsCommandAllowedForUser(ctx, requiredPermission))
                {
                    string permittedRolesDesc = (DLConfig.Data.AdminRoles.Count > 0) ? string.Join("\n- ", DLConfig.Data.AdminRoles.ToArray()) : "No admin roles configured";
                    await RespondToCommand(ctx, $"You lack the `{requiredPermission}` level permission required to execute this command.\nThe permitted roles are:\n```- {permittedRolesDesc}```");
                    return;
                }

                if (!IsCommandAllowedInChannel(ctx))
                {
                    string commandChannels = string.Join("\n- ", DLConfig.Data.DiscordCommandChannels.Where(link => link.IsValid()).Select(channel => channel.Channel.Name));
                    await RespondToCommand(ctx, $"You aren't allowed to post commands in this channel.\nCommands are allowed in the following channels:\n```- {commandChannels}```");
                    return;
                }

                if (ctx.Channel.IsPrivate)
                    Logger.Debug($"{ctx.User.Username} invoked Discord command \"/{command.Method.Name}\" in DM");
                else
                    Logger.Debug($"{ctx.User.Username} invoked Discord command \"/{command.Method.Name}\" in channel {ctx.Channel.Name}");

                await command(ctx, parameters);
            }
            catch (Exception e)
            {
                Logger.Exception($"An error occurred while attempting to execute a Discord command", e);
                await RespondToCommand(ctx, $"An error occurred while attempting to run that command. Error message: {e}");
            }
        }

        private static async Task RespondToCommand(InteractionContext ctx, string fullTextContent, DiscordLinkEmbed embedContent) => await RespondToCommand(ctx, fullTextContent, embedContent.SingleItemAsEnumerable());

        private static async Task RespondToCommand(InteractionContext ctx, string fullTextContent, IEnumerable<DiscordLinkEmbed> embedContent = null)
        {
            async static Task Respond(InteractionContext ctx, string textContent, IEnumerable<DiscordLinkEmbed> embedContent)
            {
                DiscordInteractionResponseBuilder Builder = new DiscordInteractionResponseBuilder();
                if (!string.IsNullOrWhiteSpace(textContent))
                {
                    if (textContent.Length < DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT)
                        Builder.Content = textContent;
                    else
                        Builder.Content = $"{textContent.Substring(0, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT - 4)}...";
                }

                if (embedContent != null)
                {
                    foreach (DiscordLinkEmbed embed in embedContent)
                    {
                        Builder.AddEmbeds(MessageUtils.BuildDiscordEmbeds(embed));
                    }
                }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, Builder);
            }

            try
            {
                DLDiscordClient client = DiscordLink.Obj.Client;
                if (!client.ChannelHasPermission(ctx.Channel, Permissions.SendMessages) || !client.ChannelHasPermission(ctx.Channel, Permissions.ReadMessageHistory))
                {
                    Logger.Error($"Failed to respond to command \"{ctx.CommandName}\" in channel \"{ctx.Channel}\" as the bot lacks permissions for sending and/or reading messages in this channel.");
                    return;
                }

                if (embedContent == null)
                {
                    await Respond(ctx, fullTextContent, null);
                }
                else
                {
                    // Either make sure we have permission to use embeds or convert the embed to text
                    if (client.ChannelHasPermission(ctx.Channel, Permissions.EmbedLinks))
                    {
                        await Respond(ctx, fullTextContent, embedContent);
                    }
                    else
                    {
                        await Respond(ctx, $"{fullTextContent}\n{string.Join("\n\n", embedContent.Select(embed => embed.AsText()))}", null);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Exception($"An error occurred while attempting to respond to command", e);
            }
        }

        private static bool IsCommandAllowedForUser(InteractionContext ctx, PermissionType requiredPermission)
        {
            return requiredPermission switch
            {
                PermissionType.User => true,
                PermissionType.Admin => DiscordLink.Obj.Client.MemberIsAdmin(ctx.Member),
                _ => false,
            };
        }

        private static bool IsCommandAllowedInChannel(InteractionContext ctx)
        {
            var commandChannels = DLConfig.Data.DiscordCommandChannels;
            bool allowed =
                ctx.Channel.IsPrivate
                || DiscordLink.Obj.Client.MemberIsAdmin(ctx.Member) // Allow admins to override channel requirements
                || !(commandChannels.Any(link => link.IsValid())); // Always allow if there are no valid command channels or the command is sent via DM

            // Check if the discord channel used is listed as a command channel
            if (!allowed)
            {
                foreach (ChannelLink link in commandChannels)
                {
                    if (link.IsChannel(ctx.Channel))
                    {
                        allowed = true;
                        break;
                    }
                }
            }

            return allowed;
        }

        #endregion

        #region User Feedback

        public static async Task ReportCommandError(InteractionContext ctx, string message)
        {
            await RespondToCommand(ctx, message);
        }

        public static async Task ReportCommandInfo(InteractionContext ctx, string message)
        {
            await RespondToCommand(ctx, message);
        }

        public static async Task DisplayCommandData(InteractionContext ctx, string title, DiscordLinkEmbed embed) => await DisplayCommandData(ctx, title, embed.SingleItemAsEnumerable());

        public static async Task DisplayCommandData(InteractionContext ctx, string title, IEnumerable<DiscordLinkEmbed> embeds)
        {
            await RespondToCommand(ctx, title, embeds);
        }

        public static async Task DisplayCommandData(InteractionContext ctx, string title, string content)
        {
            await RespondToCommand(ctx, $"**{title}**\n```{content}```");
        }

        #endregion

        #region Eco Commands

        [SlashCommand("EcoCommand", "Runs an ingame command.")]
        public async Task EcoCommand(InteractionContext ctx, [Option("Command", "The Eco command to run.")] string command)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                RemoteEcoCommandClient Client = new RemoteEcoCommandClient(ctx);
                await ServiceHolder<IChatManager>.Obj.ExecuteCommandAsync(Client, command);
            }, ctx);
        }

        #endregion

        #region Plugin Management

        [SlashCommand("Update", "Forces an update of most internal systems.")]
        public async Task Update(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.Update(CommandInterface.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("RestartPlugin", "Restarts the DiscordLink plugin.")]
        public async Task RestartPlugin(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.RestartPlugin(CommandInterface.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("ResetPersistentData", "Removes all persistent storage data.")]
        public async Task ResetPersistentData(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ResetPersistentData(CommandInterface.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("ResetWorldData", "Resets world data as if a new world had been created.")]
        public async Task ResetWorldData(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ResetWorldData(CommandInterface.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("ClearRoles", "Deletes all Discord roles created and tracked by DiscordLink.")]
        public async Task ClearRoles(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ClearRoles(CommandInterface.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Server Management

        [SlashCommand("ServerShutdown", "Shuts the server down.")]
        public async Task ServerShutdown(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ServerShutdown(CommandInterface.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Meta

        [SlashCommand("About", "Displays a message describing what the DiscordLink plugin is.")]
        public async Task About(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("About DiscordLink")
                    .WithDescription(MessageBuilder.Shared.GetAboutMessage());

                await RespondToCommand(ctx, null, embed);
            }, ctx);
        }

        [SlashCommand("PluginStatus", "Displays the current plugin status.")]
        public async Task PluginStatus(InteractionContext ctx, [Option("Verbose", "Use verbose output with extra information.")] bool verbose = false)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, await MessageBuilder.Shared.GetDisplayStringAsync(verbose));
            }, ctx);
        }

        [SlashCommand("VerifyConfig", "Checks configuration setup and reports any errors.")]
        public async Task VerifyConfig(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyConfig(CommandInterface.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("VerifyPermissions", "Checks all permissions and intents needed and reports any missing ones.")]
        public async Task VerifyPermissions(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(CommandInterface.Discord, ctx, MessageBuilder.PermissionReportComponentFlag.All);
            }, ctx);
        }

        [SlashCommand("VerifyIntents", "Checks all intents needed and reports any missing ones.")]
        public async Task CheckIntents(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(CommandInterface.Discord, ctx, MessageBuilder.PermissionReportComponentFlag.Intents);
            }, ctx);
        }

        [SlashCommand("VerifyServerPermissions", "Checks all server permissions needed and reports any missing ones.")]
        public async Task VerifyServerPermissions(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(CommandInterface.Discord, ctx, MessageBuilder.PermissionReportComponentFlag.ServerPermissions);
            }, ctx);
        }

        [SlashCommand("VerifyChannelPermissions", "Checks all permissions needed for the given channel and reports any missing ones.")]
        public async Task CheckChannelPermissions(InteractionContext ctx, [Option("Channel", "Name or ID of the channel to check permissions for. Defaults to the current channel.")] string channelNameOrID = "")
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                if (string.IsNullOrWhiteSpace(channelNameOrID))
                    await SharedCommands.VerifyPermissionsForChannel(CommandInterface.Discord, ctx, ctx.Channel);
                else
                    await SharedCommands.VerifyPermissionsForChannel(CommandInterface.Discord, ctx, channelNameOrID);
            }, ctx);
        }

        [SlashCommand("ListLinkedChannels", "Presents a list of all channel links.")]
        public async Task ListLinkedChannels(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ListChannelLinks(CommandInterface.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("Echo", "Sends a message to Eco and back to Discord again if a chat link is configured for the channel.")]
        public async Task Echo(InteractionContext ctx, [Option("Message", "The message to send. Defaults to a random message.")] string message = "", [Option("EcoChannel", "The eco channel you want to test.")] string ecoChannel = "")
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
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

                List<string> targetEcoChannelNames = new List<string>();
                if (!string.IsNullOrWhiteSpace(ecoChannel))
                {
                    EcoUtils.SendChatToChannel(null, ecoChannel, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                    targetEcoChannelNames.Add(ecoChannel);
                }
                else
                {
                    bool linkFound = false;
                    foreach (ChatChannelLink chatLink in DLConfig.ChatLinksForDiscordChannel(ctx.Channel))
                    {
                        EcoUtils.SendChatToChannel(null, chatLink.EcoChannel, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                        targetEcoChannelNames.Add(chatLink.EcoChannel);
                        linkFound = true;
                    }

                    if (!linkFound)
                    {
                        EcoUtils.SendChatToChannel(null, DLConstants.DEFAULT_CHAT_CHANNEL, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                        targetEcoChannelNames.Add(DLConstants.DEFAULT_CHAT_CHANNEL);
                    }
                }

                await RespondToCommand(ctx, $"Message sent to the following Eco channel(s): {string.Join(',', targetEcoChannelNames)}");
            }, ctx);
        }

        #endregion

        #region Account Linking

        [SlashCommand("LinkInformation", "Presents information about account linking.")]
        public async Task LinkInformation(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Eco --> Discord Account Linking")
                    .WithDescription(MessageBuilder.Shared.GetLinkAccountInfoMessage(CommandInterface.Discord));

                await RespondToCommand(ctx, null, embed);
            }, ctx);
        }

        #endregion

        #region Lookups

        [SlashCommand("ServerStatus", "Displays the Server Info status.")]
        public async Task ServerStatus(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await DisplayCommandData(ctx, string.Empty, MessageBuilder.Discord.GetServerInfo(MessageBuilder.ServerInfoComponentFlag.All));
            }, ctx);
        }

        [SlashCommand("PlayerList", "Lists the players currently online on the server.")]
        public async Task PlayerList(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Players")
                    .WithDescription(MessageBuilder.Shared.GetOnlinePlayerList());
                await DisplayCommandData(ctx, string.Empty, embed);
            }, ctx);
        }

        [SlashCommand("PlayerReport", "Displays the Player Report for the given player.")]
        public async Task PlayerReport(InteractionContext ctx,
            [Option("Player", "Name or ID of the player for which to display the report.")] string playerNameOrID, [Option("Report", "Which type of information the report should include.")] PlayerReportComponentFlag reportType = PlayerReportComponentFlag.All)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerReport(CommandInterface.Discord, ctx, playerNameOrID, reportType);
            }, ctx);
        }

        [SlashCommand("CurrencyReport", "Displays the Currency Report for the given currency.")]
        public async Task CurrencyReport(InteractionContext ctx,
            [Option("Currency", "Name or ID of the currency for which to display a report.")] string currencyNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.CurrencyReport(CommandInterface.Discord, ctx, currencyNameOrID);
            }, ctx);
        }

        [SlashCommand("CurrenciesReport", "Displays a report for the top used currencies.")]
        public async Task CurrenciesReport(InteractionContext ctx,
            [Option("Type", "The type of currencies to include in the report.")] CurrencyType currencyType = CurrencyType.All,
            [Option("MaxPerType", "How many currencies per type to display reports for.")] long maxCurrenciesPerType = DLConstants.CURRENCY_REPORT_COMMAND_MAX_CURRENCIES_PER_TYPE_DEFAULT,
            [Option("HolderCount", "How many top account holders per currency to include in the report.")] long holdersPerCurrency = DLConstants.CURRENCY_REPORT_COMMAND_MAX_TOP_HOLDERS_PER_CURRENCY_DEFAULT)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.CurrenciesReport(CommandInterface.Discord, ctx, currencyType, (int)maxCurrenciesPerType, (int)holdersPerCurrency);
            }, ctx);
        }

        [SlashCommand("ElectionReport", "Displays the Election Report for the given election.")]
        public async Task ElectionReport(InteractionContext ctx,
            [Option("Election", "Name or ID of the election for which to display a report.")] string electionNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.ElectionReport(CommandInterface.Discord, ctx, electionNameOrID);
            }, ctx);
        }

        [SlashCommand("ElectionsReport", "Displays a report for the currently active elections.")]
        public async Task ElectionsReport(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.ElectionsReport(CommandInterface.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("WorkPartyReport", "Displays the Work Party Report for the given work party.")]
        public async Task WorkPartyReport(InteractionContext ctx,
            [Option("WorkParty", "Name or ID of the work party for which to display a report.")] string workPartyNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.WorkPartyReport(CommandInterface.Discord, ctx, workPartyNameOrID);
            }, ctx);
        }

        [SlashCommand("WorkPartiesReport", "Displays a report for the currently active work parties.")]
        public async Task WorkPartiesReport(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.WorkPartiesReport(CommandInterface.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Invites

        [SlashCommand("PostInviteMessage", "Posts a Discord invite message to the Eco chat.")]
        public async Task PostInviteMessage(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PostInviteMessage(CommandInterface.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Trades

        [SlashCommand("Trades", "Displays available trades by player, tag, item or store.")]
        public async Task Trades(InteractionContext ctx, [Option("SearchName", "The player name or item name for which to display trades. Case insensitive and auto completed.")] string searchName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.Trades(CommandInterface.Discord, ctx, searchName);
            }, ctx);
        }

        [SlashCommand("DLT", "Shorthand for the Trades command")]
        public async Task DLT(InteractionContext ctx, [Option("SearchName", "The player name or item name for which to display trades. Case insensitive and auto completed.")] string searchName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await Trades(ctx, searchName);
            }, ctx);
        }

        [SlashCommand("AddTradeWatcherDisplay", "Creates a live updated display of available trades by player, tag, item or store.")]
        public async Task AddTradeWatcherDisplay(InteractionContext ctx, [Option("SearchName", "The player name or item name for which to display trades.")] string searchName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.AddTradeWatcher(CommandInterface.Discord, ctx, searchName, Modules.ModuleArchetype.Display);
            }, ctx);
        }

        [SlashCommand("RemoveTradeWatcherDisplay", "Removes the live updated display of available trades for a player, tag, item or store.")]
        public async Task RemoveTradeWatcherDisplay(InteractionContext ctx, [Option("SearchName", "The player, tag, item or store name for which to display trades.")] string searchName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(CommandInterface.Discord, ctx, searchName, Modules.ModuleArchetype.Display);
            }, ctx);
        }

        [SlashCommand("AddTradeWatcherFeed", "Creates a trade feed filtered by a search query.")]
        public async Task AddTradeWatcherFeed(InteractionContext ctx, [Option("SearchName", "The player, tag, item or store name for which to post trades.")] string searchName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.AddTradeWatcher(CommandInterface.Discord, ctx, searchName, Modules.ModuleArchetype.Feed);
            }, ctx);
        }

        [SlashCommand("RemoveTradeWatcherFeed", "Removes the trade watcher feed for a player, tag, item or store.")]
        public async Task RemoveTradeWatcherFeed(InteractionContext ctx, [Option("SearchName", "The player, tag item or store name for which to remove trades.")] string searchName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(CommandInterface.Discord, ctx, searchName, Modules.ModuleArchetype.Feed);
            }, ctx);
        }

        [SlashCommand("ListTradeWatchers", "Lists all trade watchers for the calling user.")]
        public async Task ListTradeWatchers(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.ListTradeWatchers(CommandInterface.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Snippets

        [SlashCommand("DiscordSnippet", "Posts a predefined snippet to Discord.")]
        public async Task Snippet(InteractionContext ctx, [Option("Key", "Key of the snippet to post. Displays the key list if omitted.")] string snippetKey = "", [Option("Context", "Where the snippet should be sent.")] CommandInterface commandTarget = CommandInterface.Discord)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.Snippet(CommandInterface.Discord, ctx, commandTarget, ctx.GetSenderName(), snippetKey);
            }, ctx);
        }

        #endregion

        #region Message Relaying

        [SlashCommand("AnnounceAll", "Sends an Eco info box message to all online users.")]
        public async Task AnnounceAll(InteractionContext ctx, [Option("Message", "The message to send.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Info, CommandInterface.Discord, ctx, message, string.Empty);
            }, ctx);
        }

        [SlashCommand("AnnounceUser", "Sends an Eco info box message to the specified user.")]
        public async Task AnnounceUser(InteractionContext ctx, [Option("Message", "The message to send.")] string message,
            [Option("Recipient", "Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Info, CommandInterface.Discord, ctx, message, recipientUserNameOrID);
            }, ctx);
        }

        [SlashCommand("WarnAll", "Sends an Eco warning box message to all online users.")]
        public async Task WarnAll(InteractionContext ctx, [Option("Message", "The message to send.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Warning, CommandInterface.Discord, ctx, message, string.Empty);
            }, ctx);
        }

        [SlashCommand("WarnUser", "Sends an Eco warning box message to the specified user.")]
        public async Task WarnUser(InteractionContext ctx, [Option("Message", "The message to send.")] string message,
            [Option("Recipient", "Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Warning, CommandInterface.Discord, ctx, message, recipientUserNameOrID);
            }, ctx);
        }

        [SlashCommand("ErrorAll", "Sends an Eco error box message to all online users.")]
        public async Task ErrorAll(InteractionContext ctx, [Option("Message", "The message to send.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Error, CommandInterface.Discord, ctx, message, string.Empty);
            }, ctx);
        }

        [SlashCommand("ErrorUser", "Sends an Eco error box message to the specified user.")]
        public async Task ErrorUser(InteractionContext ctx, [Option("Message", "The message to send.")] string message,
            [Option("Recipient", "Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Error, CommandInterface.Discord, ctx, message, recipientUserNameOrID);
            }, ctx);
        }

        [SlashCommand("NotificyAll", "Sends an Eco notification message to all online and conditionally offline users.")]
        public async Task NotificyAll(InteractionContext ctx, [Option("Message", "The message to send.")] string message,
            [Option("IncludeOffline", "Whether or not to send the message to offline users as well.")] bool includeOfflineUsers = true)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendNotification(CommandInterface.Discord, ctx, message, string.Empty, includeOfflineUsers);
            }, ctx);
        }

        [SlashCommand("NotifyUser", "Sends an Eco notification message to the specified user.")]
        public async Task NotifyUser(InteractionContext ctx, [Option("Message", "The message to send.")] string message,
            [Option("Recipient", "Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendNotification(CommandInterface.Discord, ctx, message, recipientUserNameOrID, includeOfflineUsers: true);
            }, ctx);
        }

        [SlashCommand("PopupAll", "Sends an Eco popup message to all online users.")]
        public async Task PopupAll(InteractionContext ctx, [Option("Message", "The message to send.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendPopup(CommandInterface.Discord, ctx, message, string.Empty);
            }, ctx);
        }

        [SlashCommand("PopupUser", "Sends an Eco popup message to the specified user.")]
        public async Task PopupUser(InteractionContext ctx, [Option("Message", "The message to send.")] string message,
            [Option("Recipient", "Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendPopup(CommandInterface.Discord, ctx, message, recipientUserNameOrID);
            }, ctx);
        }

        [SlashCommand("InfoPanelAll", "Displays an info panel to all online users.")]
        public async Task InfoPanelToAll(InteractionContext ctx, [Option("Title", "The title for the info panel.")] string title,
            [Option("Message", "The message to display in the info panel.")] string message)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendInfoPanel(CommandInterface.Discord, ctx, DLConstants.ECO_PANEL_NOTIFICATION, title, message, string.Empty);
            }, ctx);
        }

        [SlashCommand("InfoPanelUser", "Displays an info panel to the specified user.")]
        public async Task InfoPanelToUser(InteractionContext ctx, [Option("Title", "The title for the info panel.")] string title,
            [Option("Message", "The message to display in the info panel.")] string message,
            [Option("Recipient", "Name or ID of the recipient Eco user.")] string recipientUserNameOrID)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.SendInfoPanel(CommandInterface.Discord, ctx, DLConstants.ECO_PANEL_NOTIFICATION, title, message, recipientUserNameOrID);
            }, ctx);
        }

        #endregion
    }
}
