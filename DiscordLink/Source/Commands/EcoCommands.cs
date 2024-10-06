using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Message;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Plugins.Networking;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Eco.Plugins.DiscordLink.Utilities.MessageBuilder;

namespace Eco.Plugins.DiscordLink
{
    public class EcoCommandContext : CommandContext
    {
        public EcoCommandContext(User user)
        {
            base.Interface = ApplicationInterfaceType.Eco;
            User = user;
        }

        public User User { get; private set; }
    }

    [ChatCommandHandler]
    public class EcoCommands
    {
#pragma warning disable CS4014 // Call not awaited (Shared commands are async but Eco commands can't be)

        #region Commands Base

        private delegate Task EcoCommand(User callingUser, params string[] parameters);

        private static async Task ExecuteCommand(EcoCommand command, User callingUser, params string[] parameters)
        {
            // Trim the arguments since they often have a space at the beginning
            for (int i = 0; i < parameters.Length; ++i)
            {
                parameters[i] = parameters[i].Trim();
            }

            string commandName = command.Method.Name;
            try
            {
                Logger.Debug($"{MessageUtils.StripTags(callingUser.Name)} invoked Eco command \"/{command.Method.Name}\"");
                await command(callingUser, parameters);
            }
            catch (Exception e)
            {
                Message.SendInfoBoxToUser(callingUser, $"Error occurred while attempting to run that command. Error message: {e}");
                Logger.Exception($"An exception occured while attempting to execute a command.\nCommand name: \"{commandName}\"\nCalling user: \"{MessageUtils.StripTags(callingUser.Name)}\"", e);
            }
        }

        [ChatCommand("Commands for the Discord integration plugin.", "DL", ChatAuthorizationLevel.User)]
#pragma warning disable IDE0079 // Remove unnecessary suppression (This is a false positive case)
#pragma warning disable IDE0060 // Remove unused parameter - callingUser parameter required
        public static void DiscordLink(User callingUser) { }
#pragma warning restore IDE0079
#pragma warning restore IDE0060

        #endregion

        #region User Feedback

        public static void ReportCommandInfo(EcoCommandContext ctx, string message)
        {
            message = MessageUtils.FormatMessageForApplication(ApplicationInterfaceType.Eco, message);
            Message.SendInfoBoxToUser(ctx.User, message);
        }

        public static void ReportCommandError(EcoCommandContext ctx, string message)
        {
            message = MessageUtils.FormatMessageForApplication(ApplicationInterfaceType.Eco, message);
            Message.SendErrorBoxToUser(ctx.User, message);
        }

        public static void DisplayCommandData(EcoCommandContext ctx, string panelInstance, string title, string data)
        {
            title = MessageUtils.FormatMessageForApplication(ApplicationInterfaceType.Eco, title);
            data = MessageUtils.FormatMessageForApplication(ApplicationInterfaceType.Eco, data);
            Message.SendInfoPanelToUser(ctx.User, panelInstance, title, data);
        }

        #endregion

        #region Plugin Management

        [ChatSubCommand("DiscordLink", "Forces an update.", ChatAuthorizationLevel.Admin)]
        public static async Task Update(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.Update(ctx);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Restarts the plugin.", ChatAuthorizationLevel.Admin)]
        public static async Task RestartPlugin(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.RestartPlugin(ctx);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Reloads the DiscordLink config.", ChatAuthorizationLevel.Admin)]
        public static async Task ReloadConfig(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.ReloadConfig(ctx);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes all persistent storage data.", ChatAuthorizationLevel.Admin)]
        public static async Task ResetPersistentData(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.ResetPersistentData(ctx);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Resets world data as if a new world had been created.", ChatAuthorizationLevel.Admin)]
        public static async Task ResetWorldData(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.ResetWorldData(ctx);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Deletes all Discord roles created and tracked by DiscordLink.", ChatAuthorizationLevel.Admin)]
        public static async Task ClearRoles(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.ClearRoles(ctx);
            }, callingUser);
        }


        [ChatSubCommand("DiscordLink", "Displays a description of the persistent storage data.", ChatAuthorizationLevel.Admin)]
        public static async Task PersistentStorageData(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.PersistentStorageData(ctx);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a description of the world storage data.", ChatAuthorizationLevel.Admin)]
        public static async Task WorldStorageData(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.WorldStorageData(ctx);
            }, callingUser);
        }

        #endregion

        #region Server Management

        [ChatSubCommand("DiscordLink", "Shuts the server down.", ChatAuthorizationLevel.Admin)]
        public static async Task ServerShutdown(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.ServerShutdown(ctx);
            }, callingUser);
        }

        #endregion

        #region Meta

        [ChatSubCommand("DiscordLink", "Displays the installed and latest available plugin version.", ChatAuthorizationLevel.User)]
        public static async Task Version(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                ReportCommandInfo(ctx, MessageBuilder.Shared.GetVersionMessage());
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays information about the DiscordLink plugin.", ChatAuthorizationLevel.User)]
        public static async Task About(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                DisplayCommandData(ctx, DLConstants.ECO_PANEL_DL_MESSAGE_MEDIUM, $"About DiscordLink {Plugins.DiscordLink.DiscordLink.Obj.InstalledVersion.ToString(3)}", MessageBuilder.Shared.GetAboutMessage());
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Opens the documentation web page", ChatAuthorizationLevel.User)]
        public static async Task Documentation(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                callingUser.OpenWebpage("https://github.com/Eco-DiscordLink/EcoDiscordPlugin");
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Shows the plugin status.", ChatAuthorizationLevel.Admin)]
        public static async Task PluginStatus(User callingUser, bool verbose = false)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                DisplayCommandData(ctx, DLConstants.ECO_PANEL_COMPLEX_LIST, "DiscordLink Status", MessageBuilder.Shared.GetDisplayStringAsync(verbose).Result);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks configuration setup and reports any errors.", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyConfig(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.VerifyConfig(ctx);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all permissions and intents needed for the current configuration and reports any missing ones.", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyPermissions(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissions(ctx, MessageBuilder.PermissionReportComponentFlag.All);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all intents needed and reports any missing ones.", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyIntents(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissions(ctx, MessageBuilder.PermissionReportComponentFlag.Intents);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all server permissions needed and reports any missing ones.", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyServerPermissions(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissions(ctx, MessageBuilder.PermissionReportComponentFlag.ServerPermissions);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all permissions needed for the given channel and reports any missing ones.", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyChannelPermissions(User callingUser, string channelNameOrId)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissionsForChannel(ctx, channelNameOrId);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Presents a list of all channel links.", ChatAuthorizationLevel.Admin)]
        public static async Task ListLinkedChannels(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.ListChannelLinks(ctx);
            }, callingUser);
        }

        #endregion

        #region Lookups

        [ChatSubCommand("DiscordLink", "Displays the Player Report for the given player.", ChatAuthorizationLevel.User)]
        public static async Task PlayerReport(User callingUser, string playerNameOrId, string reportType = "All")
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                if (!Enum.TryParse(reportType, out PlayerReportComponentFlag reportTypeEnum))
                {
                    ReportCommandError(ctx, $"\"{reportType}\" is not a valid report type. The available report types are: {string.Join(", ", Enum.GetNames(typeof(PlayerReportComponentFlag)))}");
                    return;
                }

                await SharedCommands.PlayerReport(ctx, playerNameOrId, reportTypeEnum);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Currency Report for the given currency.", ChatAuthorizationLevel.User)]
        public static async Task CurrencyReport(User callingUser, string currencyNameOrId,
            int maxTopHoldersCount = DLConfig.DefaultValues.MaxTopCurrencyHolderCount,
            bool useTradeCount = true,
            bool useBackingInfo = false)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.CurrencyReport(ctx, currencyNameOrId, maxTopHoldersCount, useTradeCount, useBackingInfo);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the top used currencies.", ChatAuthorizationLevel.User)]
        public static async Task CurrenciesReport(User callingUser, string currencyType = "all",
            int maxCurrenciesPerType = DLConstants.CURRENCY_REPORT_COMMAND_MAX_CURRENCIES_PER_TYPE_DEFAULT,
            int holdersPerCurrency = DLConstants.CURRENCY_REPORT_COMMAND_MAX_TOP_HOLDERS_PER_CURRENCY_DEFAULT)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                CurrencyType type;
                if (!Enum.TryParse(currencyType, out type))
                {
                    ReportCommandError(ctx, "The CurrencyType parameter must be \"All\", \"Personal\" or \"Minted\".");
                    return;
                }

                await SharedCommands.CurrenciesReport(ctx, type, maxCurrenciesPerType, holdersPerCurrency);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Election Report for the given election.", ChatAuthorizationLevel.User)]
        public static async Task ElectionReport(User callingUser, string electionNameOrId)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.ElectionReport(ctx, electionNameOrId);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the currently active elections.", ChatAuthorizationLevel.User)]
        public static async Task ElectionsReport(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.ElectionsReport(ctx);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Work Party Report for the given work party.", ChatAuthorizationLevel.User)]
        public static async Task WorkPartyReport(User callingUser, string workPartyNameOrId)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.WorkPartyReport(ctx, workPartyNameOrId);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the currently active work parties.", ChatAuthorizationLevel.User)]
        public static async Task WorkPartiesReport(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.WorkPartiesReport(ctx);
            }, callingUser);
        }

        #endregion

        #region Invites

        [ChatSubCommand("DiscordLink", "Posts a Discord invite message to the Eco chat.", ChatAuthorizationLevel.User)]
        public static async Task PostInviteMessage(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.PostInviteMessage(ctx);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Opens an invite to the Discord server.", ChatAuthorizationLevel.User)]
        public static async Task InviteMe(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                string discordAddress = NetworkManager.Config.DiscordAddress;
                if (string.IsNullOrEmpty(discordAddress))
                {
                    ReportCommandError(ctx, "This server does not have an associated Discord server.");
                    return;
                }

                int findIndex = discordAddress.LastIndexOf('/');
                if (findIndex < 0)
                {
                    ReportCommandError(ctx, "The configured discord address is invalid.");
                    return;
                }

                string inviteCode = discordAddress.Substring(findIndex + 1);
                callingUser.OpenDiscordInvite(inviteCode);
                ReportCommandInfo(ctx, "Invite sent");
            }, callingUser);
        }

        #endregion

        #region Account Linking

        [ChatSubCommand("DiscordLink", "Presents information about account linking.", "LinkInfo", ChatAuthorizationLevel.User)]
        public static async Task LinkInformation(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                DisplayCommandData(ctx, DLConstants.ECO_PANEL_DL_MESSAGE_MEDIUM, $"Discord Account Linking", MessageUtils.FormatMessageForEco(MessageBuilder.Shared.GetLinkAccountInfoMessage()));
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Links the calling user account to a Discord account.", ChatAuthorizationLevel.User)]
        public static async Task LinkAccount(User callingUser, string discordName)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;

                if (!plugin.Client.BotHasIntent(DiscordIntents.GuildMembers))
                {
                    ReportCommandError(ctx, $"This server is not configured to use account linking as the bot lacks the elevated Guild Members Intent.");
                    return;
                }

                // Find the Discord user
                DiscordMember matchingMember = null;
                IReadOnlyCollection<DiscordMember> guildMembers = await plugin.Client.GetMembersAsync();
                if (guildMembers == null)
                    return;

                foreach (DiscordMember member in guildMembers)
                {
                    if (member.HasNameOrMemberId(discordName))
                    {
                        matchingMember = member;
                        break;
                    }
                }

                if (matchingMember == null)
                {
                    ReportCommandError(ctx, $"No Discord account with the name \"{discordName}\" could be found.\nUse `/DL LinkInformation` for linking instructions.");
                    return;
                }

                // Make sure that the accounts aren't already linked to any account
                foreach (LinkedUser linkedUser in DLStorage.PersistentData.LinkedUsers)
                {
                    bool hasStrangeId = !string.IsNullOrWhiteSpace(callingUser.StrangeId) && !string.IsNullOrWhiteSpace(linkedUser.StrangeId);
                    bool hasSteamId = !string.IsNullOrWhiteSpace(callingUser.SteamId) && !string.IsNullOrWhiteSpace(linkedUser.SteamId);
                    if ((hasStrangeId && callingUser.StrangeId == linkedUser.StrangeId) || (hasSteamId && callingUser.SteamId == linkedUser.SteamId))
                    {
                        if (linkedUser.DiscordId == matchingMember.Id.ToString())
                            ReportCommandInfo(ctx, $"Eco account is already linked to this Discord account.\nUse `/DL UnlinkAccount` to remove the existing link.");
                        else
                            ReportCommandInfo(ctx, $"Eco account is already linked to a different Discord account.\nUse `/DL UnlinkAccount` to remove the existing link.");
                        return;
                    }
                    else if (linkedUser.DiscordId == matchingMember.Id.ToString())
                    {
                        ReportCommandError(ctx, "Discord account is already linked to a different Eco account.");
                        return;
                    }
                }

                // Try to Notify the Discord account, that a link has been made and ask for verification
                DiscordMessage message = await plugin.Client.SendDmAsync(matchingMember, null, MessageBuilder.Discord.GetVerificationDM(callingUser));

                // This message can be null, when the target user has blocked direct messages from guild members.
                if (message != null)
                {
                    _ = message.CreateReactionAsync(DLConstants.ACCEPT_EMOJI);
                    _ = message.CreateReactionAsync(DLConstants.DENY_EMOJI);
                }
                else
                {
                    ReportCommandError(ctx, $"Failed to send direct message to {matchingMember.Username}.\nPlease check your privacy settings and verify, that members of your server are allowed to send you direct messages.");
                    return;
                }

                // Create a linked user from the combined Eco and Discord info
                UserLinkManager.AddLinkedUser(callingUser, matchingMember.Id.ToString(), matchingMember.Guild.Id.ToString());


                // Notify the Eco user that the link has been created and that verification is required
                ReportCommandInfo(ctx, $"Your account has been linked.\nThe link requires verification before becoming active.\nInstructions have been sent to the linked Discord account.");
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Unlinks the Eco account from a linked Discord account.", ChatAuthorizationLevel.User)]
        public static async Task UnlinkAccount(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                bool result = UserLinkManager.RemoveLinkedUser(callingUser);
                if (result)
                    ReportCommandInfo(ctx, $"Discord account unlinked.");
                else
                    ReportCommandError(ctx, $"No linked Discord account could be found.");
            }, callingUser);
        }

        #endregion

        #region Trades

        [ChatSubCommand("DiscordLink", "Displays available trades by player, tag, item or store. Alias for /Moose Trades.", "DLT", ChatAuthorizationLevel.User)]
        public static async Task Trades(User callingUser, string searchName)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                Moose.Plugin.Commands.Trades(callingUser, searchName);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Creates a live updated display of available trades by player, tag, item or store", ChatAuthorizationLevel.User)]
        public static async Task WatchTradeDisplay(User callingUser, string searchName)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.AddTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Display);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes the live updated display of available trades for the player, tag, item or store.", ChatAuthorizationLevel.User)]
        public static async Task UnwatchTradeDisplay(User callingUser, string searchName)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Display);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Creates a feed where the bot will post trades filtered by the search query, as they occur ingame. The search query can filter by player, tag, item or store.", ChatAuthorizationLevel.User)]
        public static async Task WatchTradeFeed(User callingUser, string searchName)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.AddTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Feed);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes the trade watcher feed for a player, tag, item or store.", ChatAuthorizationLevel.User)]
        public static async Task UnwatchTradeFeed(User callingUser, string searchName)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Feed);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Lists all trade watchers for the calling user.", ChatAuthorizationLevel.User)]
        public static async Task ListTradeWatchers(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.ListTradeWatchers(ctx);
            }, callingUser);
        }

        #endregion

        #region Snippets

        [ChatSubCommand("DiscordLink", "Post a predefined snippet from Discord to Eco.", ChatAuthorizationLevel.User)]
        public static async Task Snippet(User callingUser, string snippetKey = "")
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                await SharedCommands.Snippet(ctx, ApplicationInterfaceType.Eco, callingUser.Name, snippetKey);
            }, callingUser);
        }

        #endregion

        #region Message Relaying

        [ChatSubCommand("DiscordLink", "Announces a message to everyone or a specified user. Alias for /Moose Announce.", ChatAuthorizationLevel.Admin)]
        public static async Task Announce(User callingUser, string message, string messageType = "Notification", User recipient = null)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                Moose.Plugin.Commands.Announce(callingUser, message, messageType, recipient);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Opts the calling user out of chat synchronization.", ChatAuthorizationLevel.User)]
        public static async Task OptOut(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                if (DLConfig.Data.ChatSyncMode == ChatSyncMode.OptOut)
                {
                    if (DLStorage.PersistentData.OptedOutUsers.Any(user => user.HasAnyId(callingUser.StrangeId, callingUser.SteamId)))
                    {
                        ReportCommandError(ctx, "You have already opted out of chat synchronization.");
                    }
                    else
                    {
                        DLStorage.PersistentData.OptedOutUsers.Add(new EcoUser(callingUser.StrangeId, callingUser.SteamId));
                        ReportCommandInfo(ctx, "You have opted out of chat synchronization.");
                    }
                }
                else if (DLConfig.Data.ChatSyncMode == ChatSyncMode.OptIn)
                {
                    EcoUser optedInUser;
                    if ((optedInUser = DLStorage.PersistentData.OptedInUsers.FirstOrDefault(user => user.HasAnyId(callingUser.StrangeId, callingUser.SteamId))) != null)
                    {
                        DLStorage.PersistentData.OptedInUsers.Remove(optedInUser);
                        ReportCommandInfo(ctx, "You have opted back out of chat synchronization.");
                    }
                    else
                    {
                        ReportCommandError(ctx, "This server is configured to use opt-in by default.");
                    }
                }

            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Opts the calling user into chat synchronization.", ChatAuthorizationLevel.User)]
        public static async Task Optin(User callingUser)
        {
            EcoCommandContext ctx = new EcoCommandContext(callingUser);
            await ExecuteCommand(async (lUser, args) =>
            {
                if (DLConfig.Data.ChatSyncMode == ChatSyncMode.OptOut)
                {
                    EcoUser optedOutUser;
                    if ((optedOutUser = DLStorage.PersistentData.OptedOutUsers.FirstOrDefault(user => user.HasAnyId(callingUser.StrangeId, callingUser.SteamId))) != null)
                    {
                        DLStorage.PersistentData.OptedOutUsers.Remove(optedOutUser);
                        ReportCommandInfo(ctx, "You have opted back into chat synchronization.");
                    }
                    else
                    {
                        ReportCommandError(ctx, "This server is configured to use opt-out by default.");
                    }
                }
                else if (DLConfig.Data.ChatSyncMode == ChatSyncMode.OptIn)
                {
                    if (DLStorage.PersistentData.OptedInUsers.Any(user => user.HasAnyId(callingUser.StrangeId, callingUser.SteamId)))
                    {
                        ReportCommandError(ctx, "You have already opted into chat synchronization.");
                    }
                    else
                    {
                        DLStorage.PersistentData.OptedInUsers.Add(new EcoUser(callingUser.StrangeId, callingUser.SteamId));
                        ReportCommandInfo(ctx, "You have opted into chat synchronization.");
                    }
                }
            }, callingUser);
        }

        #endregion

#pragma warning restore CS4014 // Call not awaited
    }
}
