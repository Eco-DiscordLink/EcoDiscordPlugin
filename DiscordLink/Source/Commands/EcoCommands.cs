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
using static Eco.Plugins.DiscordLink.Enums;
using static Eco.Plugins.DiscordLink.Utilities.MessageBuilder;

namespace Eco.Plugins.DiscordLink
{
    [ChatCommandHandler]
    public class EcoCommands
    {
#pragma warning disable CS4014 // Call not awaited (Shared commands are async but Eco commands can't be)

        #region Commands Base

        private delegate Task EcoCommand(User callingUser, params string[] parameters);

        private static async Task ExecuteCommand<TRet>(EcoCommand command, User callingUser, params string[] parameters)
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

        public static void ReportCommandInfo(User callingUser, string message)
        {
            Message.SendInfoBoxToUser(callingUser, message);
        }

        public static void ReportCommandError(User callingUser, string message)
        {
            Message.SendErrorBoxToUser(callingUser, message);
        }

        public static void DisplayCommandData(User callingUser, string panelInstance, string title, string data)
        {
            Message.SendInfoPanelToUser(callingUser, panelInstance, title, data);
        }

        #endregion

        #region Plugin Management

        [ChatSubCommand("DiscordLink", "Forces an update.", ChatAuthorizationLevel.Admin)]
        public static async Task Update(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.Update(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Restarts the plugin.", ChatAuthorizationLevel.Admin)]
        public static async Task RestartPlugin(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.RestartPlugin(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes all persistent storage data.", ChatAuthorizationLevel.Admin)]
        public static async Task ResetPersistentData(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ResetPersistentData(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Resets world data as if a new world had been created.", ChatAuthorizationLevel.Admin)]
        public static async Task ResetWorldData(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ResetWorldData(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Deletes all Discord roles created and tracked by DiscordLink.", ChatAuthorizationLevel.Admin)]
        public static async Task ClearRoles(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ClearRoles(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Server Management

        [ChatSubCommand("DiscordLink", "Shuts the server down.", ChatAuthorizationLevel.Admin)]
        public static async Task ServerShutdown(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ServerShutdown(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Meta

        [ChatSubCommand("DiscordLink", "Displays the installed and latest available plugin version.", ChatAuthorizationLevel.User)]
        public static async Task Version(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                ReportCommandInfo(callingUser, MessageBuilder.Shared.GetVersionMessage());
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays information about the DiscordLink plugin.", ChatAuthorizationLevel.User)]
        public static async Task About(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_DL_MESSAGE_MEDIUM, $"About DiscordLink {Plugins.DiscordLink.DiscordLink.Obj.InstalledVersion.ToString(3)}", MessageBuilder.Shared.GetAboutMessage());
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Open the documentation web page", ChatAuthorizationLevel.User)]
        public static async Task Documentation(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                callingUser.OpenWebpage("https://github.com/Eco-DiscordLink/EcoDiscordPlugin");
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Shows the plugin status.", ChatAuthorizationLevel.Admin)]
        public static async Task PluginStatus(User callingUser, bool verbose = false)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_COMPLEX_LIST, "DiscordLink Status", MessageBuilder.Shared.GetDisplayStringAsync(verbose).Result);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks configuration setup and reports any errors.", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyConfig(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.VerifyConfig(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all permissions and intents needed for the current configuration and reports any missing ones.", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyPermissions(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissions(ApplicationInterfaceType.Eco, callingUser, MessageBuilder.PermissionReportComponentFlag.All);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all intents needed and reports any missing ones.", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyIntents(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissions(ApplicationInterfaceType.Eco, callingUser, MessageBuilder.PermissionReportComponentFlag.Intents);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all server permissions needed and reports any missing ones.", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyServerPermissions(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissions(ApplicationInterfaceType.Eco, callingUser, MessageBuilder.PermissionReportComponentFlag.ServerPermissions);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Checks all permissions needed for the given channel and reports any missing ones.", ChatAuthorizationLevel.Admin)]
        public static async Task VerifyChannelPermissions(User callingUser, string channelNameOrID)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.VerifyPermissionsForChannel(ApplicationInterfaceType.Eco, callingUser, channelNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Presents a list of all channel links.", ChatAuthorizationLevel.Admin)]
        public static async Task ListLinkedChannels(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ListChannelLinks(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Lookups

        [ChatSubCommand("DiscordLink", "Displays the Player Report for the given player.", ChatAuthorizationLevel.User)]
        public static async Task PlayerReport(User callingUser, string playerNameOrID, string reportType = "All")
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                if (!Enum.TryParse(reportType, out PlayerReportComponentFlag reportTypeEnum))
                {
                    ReportCommandError(callingUser, $"\"{reportType}\" is not a valid report type. The available report types are: {string.Join(", ", Enum.GetNames(typeof(PlayerReportComponentFlag)))}");
                    return;
                }

                await SharedCommands.PlayerReport(ApplicationInterfaceType.Eco, callingUser, playerNameOrID, reportTypeEnum);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Currency Report for the given currency.", ChatAuthorizationLevel.User)]
        public static async Task CurrencyReport(User callingUser, string currencyNameOrID)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.CurrencyReport(ApplicationInterfaceType.Eco, callingUser, currencyNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the top used currencies.", ChatAuthorizationLevel.User)]
        public static async Task CurrenciesReport(User callingUser, string currencyType = "all",
            int maxCurrenciesPerType = DLConstants.CURRENCY_REPORT_COMMAND_MAX_CURRENCIES_PER_TYPE_DEFAULT,
            int holdersPerCurrency = DLConstants.CURRENCY_REPORT_COMMAND_MAX_TOP_HOLDERS_PER_CURRENCY_DEFAULT)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                CurrencyType type;
                if (!Enum.TryParse(currencyType, out type))
                {
                    ReportCommandError(callingUser, "The CurrencyType parameter must be \"All\", \"Personal\" or \"Minted\".");
                    return;
                }

                await SharedCommands.CurrenciesReport(ApplicationInterfaceType.Eco, callingUser, type, maxCurrenciesPerType, holdersPerCurrency);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Election Report for the given election.", ChatAuthorizationLevel.User)]
        public static async Task ElectionReport(User callingUser, string electionNameOrID)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ElectionReport(ApplicationInterfaceType.Eco, callingUser, electionNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the currently active elections.", ChatAuthorizationLevel.User)]
        public static async Task ElectionsReport(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ElectionsReport(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Work Party Report for the given work party.", ChatAuthorizationLevel.User)]
        public static async Task WorkPartyReport(User callingUser, string workPartyNameOrID)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.WorkPartyReport(ApplicationInterfaceType.Eco, callingUser, workPartyNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the currently active work parties.", ChatAuthorizationLevel.User)]
        public static async Task WorkPartiesReport(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.WorkPartiesReport(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Invites

        [ChatSubCommand("DiscordLink", "Posts a Discord invite message to the Eco chat.", ChatAuthorizationLevel.User)]
        public static async Task PostInviteMessage(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.PostInviteMessage(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Opens an invite to the Discord server.", ChatAuthorizationLevel.User)]
        public static async Task InviteMe(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                string discordAddress = NetworkManager.Config.DiscordAddress;
                if (string.IsNullOrEmpty(discordAddress))
                {
                    ReportCommandError(callingUser, "This server does not have an associated Discord server.");
                    return;
                }

                int findIndex = discordAddress.LastIndexOf('/');
                if (findIndex < 0)
                {
                    ReportCommandError(callingUser, "The configured discord address is invalid.");
                    return;
                }

                string inviteCode = discordAddress.Substring(findIndex + 1);
                callingUser.OpenDiscordInvite(inviteCode);
                ReportCommandInfo(callingUser, "Invite sent");
            }, callingUser);
        }

        #endregion

        #region Account Linking

        [ChatSubCommand("DiscordLink", "Presents information about account linking.", ChatAuthorizationLevel.User)]
        public static async Task LinkInformation(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_DL_MESSAGE_MEDIUM, $"Discord Account Linking", MessageUtils.FormatMessageForEco(MessageBuilder.Shared.GetLinkAccountInfoMessage()));
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Links the calling user account to a Discord account.", ChatAuthorizationLevel.User)]
        public static async Task LinkAccount(User callingUser, string discordName)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;

                if (!plugin.Client.BotHasIntent(DiscordIntents.GuildMembers))
                {
                    ReportCommandError(callingUser, $"This server is not configured to use account linking as the bot lacks the elevated Guild Members Intent.");
                    return;
                }

                // Find the Discord user
                DiscordMember matchingMember = null;
                IReadOnlyCollection<DiscordMember> guildMembers = await plugin.Client.GetGuildMembersAsync();
                if (guildMembers == null)
                    return;

                foreach (DiscordMember member in guildMembers)
                {
                    if (member.HasNameOrID(discordName))
                    {
                        matchingMember = member;
                        break;
                    }
                }

                if (matchingMember == null)
                {
                    ReportCommandError(callingUser, $"No Discord account with the name \"{discordName}\" could be found.\nUse `/DL LinkInformation` for linking instructions.");
                    return;
                }

                // Make sure that the accounts aren't already linked to any account
                foreach (LinkedUser linkedUser in DLStorage.PersistentData.LinkedUsers)
                {
                    bool hasSLGID = !string.IsNullOrWhiteSpace(callingUser.SlgId) && !string.IsNullOrWhiteSpace(linkedUser.SlgID);
                    bool hasSteamID = !string.IsNullOrWhiteSpace(callingUser.SteamId) && !string.IsNullOrWhiteSpace(linkedUser.SteamID);
                    if ((hasSLGID && callingUser.SlgId == linkedUser.SlgID) || (hasSteamID && callingUser.SteamId == linkedUser.SteamID))
                    {
                        if (linkedUser.DiscordID == matchingMember.Id.ToString())
                            ReportCommandInfo(callingUser, $"Eco account is already linked to this Discord account.\nUse `/DL UnlinkAccount` to remove the existing link.");
                        else
                            ReportCommandInfo(callingUser, $"Eco account is already linked to a different Discord account.\nUse `/DL UnlinkAccount` to remove the existing link.");
                        return;
                    }
                    else if (linkedUser.DiscordID == matchingMember.Id.ToString())
                    {
                        ReportCommandError(callingUser, "Discord account is already linked to a different Eco account.");
                        return;
                    }
                }

                // Try to Notify the Discord account, that a link has been made and ask for verification
                DiscordMessage message = await plugin.Client.SendDMAsync(matchingMember, null, MessageBuilder.Discord.GetVerificationDM(callingUser));

                // This message can be null, when the target user has blocked direct messages from guild members.
                if (message != null)
                {
                    _ = message.CreateReactionAsync(DLConstants.ACCEPT_EMOJI);
                    _ = message.CreateReactionAsync(DLConstants.DENY_EMOJI);
                }
                else
                {
                    ReportCommandError(callingUser, $"Failed to send direct message to {matchingMember.Username}.\nPlease check your privacy settings and verify, that members of your server are allowed to send you direct messages.");
                    return;
                }

                // Create a linked user from the combined Eco and Discord info
                UserLinkManager.AddLinkedUser(callingUser, matchingMember.Id.ToString(), matchingMember.Guild.Id.ToString());


                // Notify the Eco user that the link has been created and that verification is required
                ReportCommandInfo(callingUser, $"Your account has been linked.\nThe link requires verification before becoming active.\nInstructions have been sent to the linked Discord account.");
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Unlinks the Eco account from a linked Discord account.", ChatAuthorizationLevel.User)]
        public static async Task UnlinkAccount(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                bool result = UserLinkManager.RemoveLinkedUser(callingUser);
                if (result)
                    ReportCommandInfo(callingUser, $"Discord account unlinked.");
                else
                    ReportCommandError(callingUser, $"No linked Discord account could be found.");
            }, callingUser);
        }

        #endregion

        #region Trades

        [ChatSubCommand("DiscordLink", "Displays available trades by player, tag, item or store. Alias for /Moose Trades.", "DLT", ChatAuthorizationLevel.User)]
        public static async Task Trades(User callingUser, string searchName)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.Trades(ApplicationInterfaceType.Eco, callingUser, searchName);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Creates a live updated display of available trades by player, tag, item or store", ChatAuthorizationLevel.User)]
        public static async Task WatchTradeDisplay(User callingUser, string searchName)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.AddTradeWatcher(ApplicationInterfaceType.Eco, callingUser, searchName, Modules.ModuleArchetype.Display);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes the live updated display of available trades for the player, tag, item or store.", ChatAuthorizationLevel.User)]
        public static async Task UnwatchTradeDisplay(User callingUser, string searchName)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(ApplicationInterfaceType.Eco, callingUser, searchName, Modules.ModuleArchetype.Display);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Creates a feed where the bot will post trades filtered by the search query, as they occur ingame. The search query can filter by player, tag, item or store.", ChatAuthorizationLevel.User)]
        public static async Task WatchTradeFeed(User callingUser, string searchName)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.AddTradeWatcher(ApplicationInterfaceType.Eco, callingUser, searchName, Modules.ModuleArchetype.Feed);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes the trade watcher feed for a player, tag, item or store.", ChatAuthorizationLevel.User)]
        public static async Task UnwatchTradeFeed(User callingUser, string searchName)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(ApplicationInterfaceType.Eco, callingUser, searchName, Modules.ModuleArchetype.Feed);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Lists all trade watchers for the calling user.", ChatAuthorizationLevel.User)]
        public static async Task ListTradeWatchers(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.ListTradeWatchers(ApplicationInterfaceType.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Snippets

        [ChatSubCommand("DiscordLink", "Post a predefined snippet from Discord to Eco.", ChatAuthorizationLevel.User)]
        public static async Task Snippet(User callingUser, string snippetKey = "")
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                await SharedCommands.Snippet(ApplicationInterfaceType.Eco, callingUser, ApplicationInterfaceType.Eco, callingUser.Name, snippetKey);
            }, callingUser);
        }

        #endregion

        #region Message Relaying

        [ChatSubCommand("DiscordLink", "Announces a message to everyone or a specified user. Alias for /Moose Announce.", ChatAuthorizationLevel.Admin)]
        public static async Task Announce(User callingUser, string message, Moose.Data.Enums.MessageType messageType = Moose.Data.Enums.MessageType.Notification, User recipient = null)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                Moose.Plugin.Commands.Announce(callingUser, message, messageType.ToString(), recipient);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Opts the calling user out of chat synchronization.", ChatAuthorizationLevel.User)]
        public static async Task OptOut(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                if (DLConfig.Data.ChatSyncMode == ChatSyncMode.OptOut)
                {
                    if (DLStorage.PersistentData.OptedOutUsers.Any(user => user.HasAnyID(callingUser.SlgId, callingUser.SteamId)))
                    {
                        ReportCommandError(callingUser, "You have already opted out of chat synchronization.");
                    }
                    else
                    {
                        DLStorage.PersistentData.OptedOutUsers.Add(new EcoUser(callingUser.SlgId, callingUser.SteamId));
                        ReportCommandInfo(callingUser, "You have opted out of chat synchronization.");
                    }
                }
                else if (DLConfig.Data.ChatSyncMode == ChatSyncMode.OptIn)
                {
                    EcoUser optedInUser;
                    if ((optedInUser = DLStorage.PersistentData.OptedInUsers.FirstOrDefault(user => user.HasAnyID(callingUser.SlgId, callingUser.SteamId))) != null)
                    {
                        DLStorage.PersistentData.OptedInUsers.Remove(optedInUser);
                        ReportCommandInfo(callingUser, "You have opted back out of chat synchronization.");
                    }
                    else
                    {
                        ReportCommandError(callingUser, "This server is configured to use opt-in by default.");
                    }
                }

            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Opts the calling user into chat synchronization.", ChatAuthorizationLevel.User)]
        public static async Task Optin(User callingUser)
        {
            await ExecuteCommand<object>(async (lUser, args) =>
            {
                if (DLConfig.Data.ChatSyncMode == ChatSyncMode.OptOut)
                {
                    EcoUser optedOutUser;
                    if ((optedOutUser = DLStorage.PersistentData.OptedOutUsers.FirstOrDefault(user => user.HasAnyID(callingUser.SlgId, callingUser.SteamId))) != null)
                    {
                        DLStorage.PersistentData.OptedOutUsers.Remove(optedOutUser);
                        ReportCommandInfo(callingUser, "You have opted back into chat synchronization.");
                    }
                    else
                    {
                        ReportCommandError(callingUser, "This server is configured to use opt-out by default.");
                    }
                }
                else if (DLConfig.Data.ChatSyncMode == ChatSyncMode.OptIn)
                {
                    if (DLStorage.PersistentData.OptedInUsers.Any(user => user.HasAnyID(callingUser.SlgId, callingUser.SteamId)))
                    {
                        ReportCommandError(callingUser, "You have already opted into chat synchronization.");
                    }
                    else
                    {
                        DLStorage.PersistentData.OptedInUsers.Add(new EcoUser(callingUser.SlgId, callingUser.SteamId));
                        ReportCommandInfo(callingUser, "You have opted into chat synchronization.");
                    }
                }
            }, callingUser);
        }

        #endregion

#pragma warning restore CS4014 // Call not awaited
    }
}
