﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Eco.Core.Utils;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.Message;
using Eco.Moose.Utils.TextUtils;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.IoC;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Eco.Plugins.DiscordLink.Enums;
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
                DiscordClient client = DiscordLink.Obj.Client;
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
                        await Respond(ctx, $"{fullTextContent}\n{string.Join("\n\n", embedContent.Select(embed => embed.AsDiscordText()))}", null);
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

        [SlashCommand("EcoCommand", "Executes an ingame command.")]
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
                await SharedCommands.Update(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("RestartPlugin", "Restarts the DiscordLink plugin.")]
        public async Task RestartPlugin(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.RestartPlugin(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("ReloadConfig", "Reloads the DiscordLink config.")]
        public async Task ReloadConfig(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ReloadConfig(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("ResetPersistentData", "Removes all persistent storage data.")]
        public async Task ResetPersistentData(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ResetPersistentData(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("ResetWorldData", "Resets world data as if a new world had been created.")]
        public async Task ResetWorldData(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ResetWorldData(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("ClearRoles", "Deletes all Discord roles created and tracked by DiscordLink.")]
        public async Task ClearRoles(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ClearRoles(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("PersistentStorageData", "Displays a description of the persistent storage data.")]
        public async Task PersistentStorageData(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.PersistentStorageData(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("WorldStorageData", "Displays a description of the world storage data.")]
        public async Task WorldStorageData(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.WorldStorageData(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Server Management

        [SlashCommand("ServerShutdown", "Shuts down the Eco server.")]
        public async Task ServerShutdown(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ServerShutdown(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Meta

        [SlashCommand("Version", "Displays the installed and latest available plugin version.")]
        public async Task Version(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Version")
                    .WithDescription(TextUtils.StripTags(MessageBuilder.Shared.GetVersionMessage()));

                await RespondToCommand(ctx, null, embed);
            }, ctx);
        }

        [SlashCommand("About", "Displays information about the DiscordLink plugin.")]
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

        [SlashCommand("Documentation", "Opens the documentation web page.")]
        public async Task Documentation(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, "The documentation can be found here: <https://github.com/Eco-DiscordLink/EcoDiscordPlugin>");
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
                await SharedCommands.VerifyConfig(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("VerifyPermissions", "Checks all permissions and intents needed and reports any missing ones.")]
        public async Task VerifyPermissions(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(ApplicationInterfaceType.Discord, ctx, MessageBuilder.PermissionReportComponentFlag.All);
            }, ctx);
        }

        [SlashCommand("VerifyIntents", "Checks all intents needed and reports any missing ones.")]
        public async Task CheckIntents(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(ApplicationInterfaceType.Discord, ctx, MessageBuilder.PermissionReportComponentFlag.Intents);
            }, ctx);
        }

        [SlashCommand("VerifyServerPermissions", "Checks all server permissions needed and reports any missing ones.")]
        public async Task VerifyServerPermissions(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(ApplicationInterfaceType.Discord, ctx, MessageBuilder.PermissionReportComponentFlag.ServerPermissions);
            }, ctx);
        }

        [SlashCommand("VerifyChannelPermissions", "Checks all permissions needed for the given channel and reports any missing ones.")]
        public async Task CheckChannelPermissions(InteractionContext ctx, [Option("Channel", "Name or ID of the channel to check permissions for. Defaults to the current channel.")] string channelNameOrId = "")
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                if (string.IsNullOrWhiteSpace(channelNameOrId))
                    await SharedCommands.VerifyPermissionsForChannel(ApplicationInterfaceType.Discord, ctx, ctx.Channel);
                else
                    await SharedCommands.VerifyPermissionsForChannel(ApplicationInterfaceType.Discord, ctx, channelNameOrId);
            }, ctx);
        }

        [SlashCommand("ListLinkedChannels", "Presents a list of all channel links.")]
        public async Task ListLinkedChannels(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                await SharedCommands.ListChannelLinks(ApplicationInterfaceType.Discord, ctx);
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
                    Message.SendChatToChannel(null, ecoChannel, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                    targetEcoChannelNames.Add(ecoChannel);
                }
                else
                {
                    bool linkFound = false;
                    foreach (ChatChannelLink chatLink in DLConfig.ChatLinksForDiscordChannel(ctx.Channel))
                    {
                        Message.SendChatToChannel(null, chatLink.EcoChannel, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                        targetEcoChannelNames.Add(chatLink.EcoChannel);
                        linkFound = true;
                    }

                    if (!linkFound)
                    {
                        Message.SendChatToChannel(null, DLConstants.DEFAULT_CHAT_CHANNEL, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
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
                    .WithDescription(MessageBuilder.Shared.GetLinkAccountInfoMessage());

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
            [Option("Player", "Name or ID of the player for which to display the report.")] string playerNameOrId, [Option("Report", "Which type of information the report should include.")] PlayerReportComponentFlag reportType = PlayerReportComponentFlag.All)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PlayerReport(ApplicationInterfaceType.Discord, ctx, playerNameOrId, reportType);
            }, ctx);
        }

        [SlashCommand("CurrencyReport", "Displays the Currency Report for the given currency.")]
        public async Task CurrencyReport(InteractionContext ctx,
            [Option("Currency", "Name or ID of the currency for which to display a report.")] string currencyNameOrId)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.CurrencyReport(ApplicationInterfaceType.Discord, ctx, currencyNameOrId);
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
                await SharedCommands.CurrenciesReport(ApplicationInterfaceType.Discord, ctx, currencyType, (int)maxCurrenciesPerType, (int)holdersPerCurrency);
            }, ctx);
        }

        [SlashCommand("ElectionReport", "Displays the Election Report for the given election.")]
        public async Task ElectionReport(InteractionContext ctx,
            [Option("Election", "Name or ID of the election for which to display a report.")] string electionNameOrId)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.ElectionReport(ApplicationInterfaceType.Discord, ctx, electionNameOrId);
            }, ctx);
        }

        [SlashCommand("ElectionsReport", "Displays a report for the currently active elections.")]
        public async Task ElectionsReport(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.ElectionsReport(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        [SlashCommand("WorkPartyReport", "Displays the Work Party Report for the given work party.")]
        public async Task WorkPartyReport(InteractionContext ctx,
            [Option("WorkParty", "Name or ID of the work party for which to display a report.")] string workPartyNameOrI)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.WorkPartyReport(ApplicationInterfaceType.Discord, ctx, workPartyNameOrI);
            }, ctx);
        }

        [SlashCommand("WorkPartiesReport", "Displays a report for the currently active work parties.")]
        public async Task WorkPartiesReport(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.WorkPartiesReport(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Invites

        [SlashCommand("PostInviteMessage", "Posts a Discord invite message to the Eco chat.")]
        public async Task PostInviteMessage(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.PostInviteMessage(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Trades

        [SlashCommand("Trades", "Displays available trades by player, tag, item or store.")]
        public async Task Trades(InteractionContext ctx, [Option("SearchName", "The player name or item name for which to display trades. Case insensitive and auto completed.")] string searchName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.Trades(ApplicationInterfaceType.Discord, ctx, searchName);
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
                await SharedCommands.AddTradeWatcher(ApplicationInterfaceType.Discord, ctx, searchName, Modules.ModuleArchetype.Display);
            }, ctx);
        }

        [SlashCommand("RemoveTradeWatcherDisplay", "Removes the live updated display of available trades for a player, tag, item or store.")]
        public async Task RemoveTradeWatcherDisplay(InteractionContext ctx, [Option("SearchName", "The player, tag, item or store name for which to display trades.")] string searchName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(ApplicationInterfaceType.Discord, ctx, searchName, Modules.ModuleArchetype.Display);
            }, ctx);
        }

        [SlashCommand("AddTradeWatcherFeed", "Creates a trade feed filtered by a search query.")]
        public async Task AddTradeWatcherFeed(InteractionContext ctx, [Option("SearchName", "The player, tag, item or store name for which to post trades.")] string searchName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.AddTradeWatcher(ApplicationInterfaceType.Discord, ctx, searchName, Modules.ModuleArchetype.Feed);
            }, ctx);
        }

        [SlashCommand("RemoveTradeWatcherFeed", "Removes the trade watcher feed for a player, tag, item or store.")]
        public async Task RemoveTradeWatcherFeed(InteractionContext ctx, [Option("SearchName", "The player, tag item or store name for which to remove trades.")] string searchName)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(ApplicationInterfaceType.Discord, ctx, searchName, Modules.ModuleArchetype.Feed);
            }, ctx);
        }

        [SlashCommand("ListTradeWatchers", "Lists all trade watchers for the calling user.")]
        public async Task ListTradeWatchers(InteractionContext ctx)
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.ListTradeWatchers(ApplicationInterfaceType.Discord, ctx);
            }, ctx);
        }

        #endregion

        #region Snippets

        [SlashCommand("Snippet", "Posts a predefined snippet to Eco or Discord.")]
        public async Task Snippet(InteractionContext ctx, [Option("Key", "Key of the snippet to post. Displays the key list if omitted.")] string snippetKey = "")
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.Snippet(ApplicationInterfaceType.Discord, ctx, ApplicationInterfaceType.Discord, ctx.GetSenderName(), snippetKey);
            }, ctx);
        }

        [SlashCommand("EcoSnippet", "Posts a predefined snippet to Eco.")]
        public async Task EcoSnippet(InteractionContext ctx, [Option("Key", "Key of the snippet to post. Displays the key list if omitted.")] string snippetKey = "")
        {
            await ExecuteCommand<object>(PermissionType.User, async (lCtx, args) =>
            {
                await SharedCommands.Snippet(ApplicationInterfaceType.Discord, ctx, ApplicationInterfaceType.Eco, ctx.GetSenderName(), snippetKey);
            }, ctx);
        }

        #endregion

        #region Message Relaying

        [SlashCommand("Announce", "Announces a message to everyone or a specified user.")]
        public async Task Announce(InteractionContext ctx, [Option("Message", "The message to send.")] string message, [Option("MessageType", "The type of message to send.")] Moose.Data.Enums.MessageType messageType = Moose.Data.Enums.MessageType.Notification, [Option("Player", "Name or ID of the player to send the message to. Sends to everyone if omitted.")] string recipientUserNameOrId = "")
        {
            await ExecuteCommand<object>(PermissionType.Admin, async (lCtx, args) =>
            {
                if (message.IsEmpty())
                {
                    await ReportCommandError(ctx, $"Failed to send message - Message can not be empty");
                    return;
                }

                User recipient = null;
                if (!recipientUserNameOrId.IsEmpty())
                {
                    recipient = Lookups.Users.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(recipientUserNameOrId) || user.Id.ToString().EqualsCaseInsensitive(recipientUserNameOrId));
                    if (recipient == null)
                    {
                        await ReportCommandError(ctx, $"No player with the name or ID \"{recipientUserNameOrId}\" could be found.");
                        return;
                    }
                }


                if (recipient != null && messageType != Moose.Data.Enums.MessageType.NotificationOffline && !recipient.IsOnline)
                {
                    await ReportCommandError(ctx, $"Failed to send message - {recipient.Name} is offline.");
                    return;
                }

                string formattedMessage = messageType switch
                {
                    Moose.Data.Enums.MessageType.Chat => $"{ctx.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageType.Info => $"{ctx.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageType.Warning => $"{ctx.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageType.Error => $"{ctx.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageType.Notification => $"[{ctx.Member.DisplayName}]\n\n{message}",
                    Moose.Data.Enums.MessageType.NotificationOffline => $"[{ctx.Member.DisplayName}]\n\n{message}",
                    Moose.Data.Enums.MessageType.Popup => $"[{ctx.Member.DisplayName}]\n{message}",
                };

                bool result = true;
                switch (messageType)
                {
                    case Moose.Data.Enums.MessageType.Chat:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendChatToUser(null, recipient, formattedMessage);
                            }
                            else
                            {
                                result = Message.SendChatToDefaultChannel(null, formattedMessage);
                            }
                            break;
                        }

                    case Moose.Data.Enums.MessageType.Info:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendInfoBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendInfoBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }

                    case Moose.Data.Enums.MessageType.Warning:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendWarningBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendWarningBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case Moose.Data.Enums.MessageType.Error:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendErrorBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendErrorBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case Moose.Data.Enums.MessageType.Popup:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendPopupToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendPopupToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case Moose.Data.Enums.MessageType.Notification:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendNotificationToUser(recipient, message, sendOffline: false);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendNotificationToUser(onlineUser, formattedMessage, sendOffline: false) && result;
                                }
                            }
                            break;
                        }

                    case Moose.Data.Enums.MessageType.NotificationOffline:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendNotificationToUser(recipient, message, sendOffline: true);
                            }
                            else
                            {
                                foreach (User user in UserManager.Users)
                                {
                                    result = Message.SendNotificationToUser(user, formattedMessage, sendOffline: true) && result;
                                }
                            }
                            break;
                        }
                }

                string sendContext = recipient == null ? "all players" : recipient.Name;
                if (result)
                    await ReportCommandInfo(ctx, $"Message delivered to {sendContext}.");
                else
                    await ReportCommandError(ctx, $"Failed to send message to {sendContext}.");


            }, ctx);
        }

        #endregion
    }
}
