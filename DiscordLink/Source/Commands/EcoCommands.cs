using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Collections.Generic;
using Eco.Plugins.DiscordLink.Extensions;

namespace Eco.Plugins.DiscordLink
{
    public class EcoCommands : IChatCommandHandler
    {
#pragma warning disable CS4014 // Call not awaited (Shared commands are async but Eco commands can't be)

        #region Commands Base

        private delegate void EcoCommand(User callingUser, params string[] parameters);

        private static void ExecuteCommand<TRet>(EcoCommand command, User callingUser, params string[] parameters)
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
                command(callingUser, parameters);
            }
            catch (Exception e)
            {
                ChatManager.ServerMessageToPlayer(new LocString($"Error occurred while attempting to run that command. Error message: {e}"), callingUser);
                Logger.Error($"An exception occured while attempting to execute a command.\nCommand name: \"{commandName}\"\nCalling user: \"{MessageUtils.StripTags(callingUser.Name)}\"\nError message: {e}");
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

        public static void ReportCommandError(User callingUser, string message)
        {
            EcoUtils.SendErrorBoxToUser(callingUser, message);
        }

        public static void ReportCommandInfo(User callingUser, string message)
        {
            EcoUtils.SendInfoBoxToUser(callingUser, message);
        }

        public static void DisplayCommandData(User callingUser, string panelInstance, string title, string data)
        {
            EcoUtils.SendInfoPanelToUser(callingUser, panelInstance, title, data);
        }

        #endregion

        #region Plugin Management

        [ChatSubCommand("DiscordLink", "Restarts the plugin.", "DL-Restart", ChatAuthorizationLevel.Admin)]
        public static void Restart(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.Restart(SharedCommands.CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes all persistent storage data.", "DL-ResetPersistentData", ChatAuthorizationLevel.Admin)]
        public static void ResetPersistentData(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.ResetPersistentData(SharedCommands.CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Resets world data as if a new world had been created.", "DL-ResetWorldData", ChatAuthorizationLevel.Admin)]
        public static void ResetWorldData(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.ResetWorldData(SharedCommands.CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Meta

        [ChatSubCommand("DiscordLink", "Displays information about the DiscordLink plugin.", "DL-About", ChatAuthorizationLevel.User)]
        public static void About(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_DL_MESSAGE_MEDIUM, $"About DiscordLink {Plugins.DiscordLink.DiscordLink.Obj.PluginVersion}", MessageBuilder.Shared.GetAboutMessage());
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Shows the plugin status.", "DL-Status", ChatAuthorizationLevel.Admin)]
        public static void PluginStatus(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_COMPLEX_LIST, "DiscordLink Status", MessageBuilder.Shared.GetDisplayStringAsync(verbose: false).Result);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Shows the plugin status including verbose debug level information.", "DL-StatusVerbose", ChatAuthorizationLevel.Admin)]
        public static void PluginStatusVerbose(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_COMPLEX_LIST, "DiscordLink Status Verbose", MessageBuilder.Shared.GetDisplayStringAsync(verbose: true).Result);
            }, callingUser);
        }

        #endregion

        #region Lookups

        [ChatSubCommand("DiscordLink", "Lists Discord servers the bot is in.", ChatAuthorizationLevel.Admin)]
        public static void ListGuilds(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;
                string joinedGuildNames = string.Join("\n", plugin.Client.DiscordClient.GuildNames());

                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_SIMPLE_LIST, "Connected Discord Servers", joinedGuildNames);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Lists channels available to the bot in a specific server.", ChatAuthorizationLevel.Admin)]
        public static void ListChannels(User callingUser, string guildNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;

                DiscordGuild guild = string.IsNullOrEmpty(guildNameOrID)
                    ? plugin.Client.DiscordClient.DefaultGuild()
                    : plugin.Client.GuildByNameOrID(guildNameOrID);

                // Can happen if DefaultGuild is not configured.
                if (guild == null)
                    ReportCommandError(callingUser, $"Failed to find guild with name \"{guildNameOrID}\"");

                string joinedChannelNames = string.Join("\n", guild.TextChannelNames());
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_SIMPLE_LIST, "Connected Discord Servers", joinedChannelNames);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player Report for the given player.", "DL-PlayerReport", ChatAuthorizationLevel.User)]
        public static void PlayerReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player Online Status Report for the given player.", "DL-PlayerOnline", ChatAuthorizationLevel.User)]
        public static void PlayerOnlineReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerOnlineReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player Time Report for the given player.", "DL-PlayerTime", ChatAuthorizationLevel.User)]
        public static void PlayerTimeReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerTimeReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player Permissions Report for the given player.", "DL-PlayerPermissions", ChatAuthorizationLevel.User)]
        public static void PlayerPermissionsReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerPermissionsReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player WhiteList/Ban/Mute Report for the given player.", "DL-PlayerAccess", ChatAuthorizationLevel.User)]
        public static void PlayerAccessReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerAccessReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player Discord Report for the given player.", "DL-PlayerDiscord", ChatAuthorizationLevel.User)]
        public static void PlayerDiscordReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                User ecoUser = EcoUtils.UserByNameOrEcoID(playerNameOrID);
                if (ecoUser == null)
                {
                    ReportCommandError(ecoUser, $"No player with the name or ID \"{playerNameOrID}\" could be found.\nNote that Discord usernames or IDs can't be looked up from Eco.");
                    return;
                }

                DiscordLinkEmbed report = MessageBuilder.Discord.GetPlayerReport(ecoUser, MessageBuilder.PlayerReportComponentFlag.DiscordInfo).Result;
                DisplayCommandData(ecoUser, DLConstants.ECO_PANEL_REPORT, $"Player Discord report for {ecoUser}", report.AsText());
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player Reputation Report for the given player.", "DL-PlayerReputation", ChatAuthorizationLevel.User)]
        public static void PlayerReputationReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerReputationReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player XP Report for the given player.", "DL-PlayerXP", ChatAuthorizationLevel.User)]
        public static void PlayerXPReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerXPReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player Skills Report for the given player.", "DL-PlayerSkills", ChatAuthorizationLevel.User)]
        public static void PlayerSkillsReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerSkillsReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player Demographics Report for the given player.", "DL-PlayerDemographics", ChatAuthorizationLevel.User)]
        public static void PlayerDemographicsReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerDemographicsReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player Titles Report for the given player.", "DL-PlayerTitles", ChatAuthorizationLevel.User)]
        public static void PlayerTitlesReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerTitlesReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Player Property Report for the given player.", "DL-PlayerProperty", ChatAuthorizationLevel.User)]
        public static void PlayerPropertyReport(User callingUser, string playerNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.PlayerPropertiesReport(SharedCommands.CommandInterface.Eco, callingUser, playerNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Currency Report for the given currency.", "DL-CurrencyReport", ChatAuthorizationLevel.User)]
        public static void CurrencyReport(User callingUser, string currencyNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.CurrencyReport(SharedCommands.CommandInterface.Eco, callingUser, currencyNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the top used currencies.", "DL-Currencies", ChatAuthorizationLevel.User)]
        public static void CurrenciesReport(User callingUser, string currencyType = "all",
            string maxCurrenciesPerType = DLConstants.CURRENCY_REPORT_COMMAND_MAX_CURRENCIES_PER_TYPE_DEFAULT,
            string holdersPerCurrency = DLConstants.CURRENCY_REPORT_COMMAND_MAX_TOP_HOLDERS_PER_CURRENCY_DEFAULT)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.CurrenciesReport(SharedCommands.CommandInterface.Eco, callingUser, currencyType, maxCurrenciesPerType, holdersPerCurrency);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Election Report for the given election.", "DL-Election", ChatAuthorizationLevel.User)]
        public static void ElectionReport(User callingUser, string electionNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.ElectionReport(SharedCommands.CommandInterface.Eco, callingUser, electionNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the currently active elections.", "DL-Elections", ChatAuthorizationLevel.User)]
        public static void ElectionsReport(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.ElectionsReport(SharedCommands.CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays the Work Party Report for the given work party.", "DL-WorkParty", ChatAuthorizationLevel.User)]
        public static void WorkPartyReport(User callingUser, string workPartyNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.WorkPartyReport(SharedCommands.CommandInterface.Eco, callingUser, workPartyNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays a report for the currently active work parties.", "DL-WorkParties", ChatAuthorizationLevel.User)]
        public static void WorkPartiesReport(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.WorkPartiesReport(SharedCommands.CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Message Relaying

        [ChatSubCommand("DiscordLink", "Sends a message to a specific server and channel.", ChatAuthorizationLevel.Admin)]
        public static void SendMessageToDiscordChannel(User callingUser, string guildNameOrID, string channelNameOrID, string message)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;

                DiscordGuild guild = plugin.Client.GuildByNameOrID(guildNameOrID);
                if (guild == null)
                {
                    ReportCommandError(callingUser, $"No guild with the name or ID \"{guildNameOrID}\" could be found.");
                    return;
                }

                DiscordChannel channel = guild.ChannelByNameOrID(channelNameOrID);
                if (channel == null)
                {
                    ReportCommandError(callingUser, $"No channel with the name or ID \"{channelNameOrID}\" could be found in the guild \"{guild.Name}\".");
                    return;
                }

                _ = plugin.Client.SendMessageAsync(channel, $"**{callingUser.Name.Replace("@", "")}**: {message}");
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an Eco server message to all online users.", "DL-ServerMessage", ChatAuthorizationLevel.Admin)]
        public static void ServerMessageToAll(User callingUser, string message)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendServerMessage(SharedCommands.CommandInterface.Eco, callingUser, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an Eco server message to the specified user.", "DL-ServermessageUser", ChatAuthorizationLevel.Admin)]
        public static void ServerMessageToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendServerMessage(SharedCommands.CommandInterface.Eco, callingUser, message, recipientUserNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an info box message to all online users.", "DL-Announce", ChatAuthorizationLevel.Admin)]
        public static void AnnouncementToAll(User callingUser, string message)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Info, SharedCommands.CommandInterface.Eco, callingUser, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an info box message to the specified user.", "DL-AnnounceUser", ChatAuthorizationLevel.Admin)]
        public static void AnnouncementToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Info, SharedCommands.CommandInterface.Eco, callingUser, message, recipientUserNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends a warning box message to all online users.", "DL-Warning", ChatAuthorizationLevel.Admin)]
        public static void WarningToAll(User callingUser, string message)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Warning, SharedCommands.CommandInterface.Eco, callingUser, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends a warning box message to the specified user.", "DL-WarnUser", ChatAuthorizationLevel.Admin)]
        public static void WarningToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Warning, SharedCommands.CommandInterface.Eco, callingUser, message, recipientUserNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an error box message to all online users.", "DL-Error", ChatAuthorizationLevel.Admin)]
        public static void ErrorToAll(User callingUser, string message)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Warning, SharedCommands.CommandInterface.Eco, callingUser, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an error box message to the specified user.", "DL-ErrorUser", ChatAuthorizationLevel.Admin)]
        public static void ErrorToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendBoxMessage(EcoUtils.BoxMessageType.Error, SharedCommands.CommandInterface.Eco, callingUser, message, recipientUserNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends a notification message to all online and conditionally offline users.", "DL-Notify", ChatAuthorizationLevel.Admin)]
        public static void NotificationToAll(User callingUser, string message, bool includeOfflineUsers = true)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendNotification(SharedCommands.CommandInterface.Eco, callingUser, message, string.Empty, includeOfflineUsers);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends a notification message to the specified user.", "DL-NotifyUser", ChatAuthorizationLevel.Admin)]
        public static void NotificationToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendNotification(SharedCommands.CommandInterface.Eco, callingUser, message, recipientUserNameOrID, includeOfflineUsers: true);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an OK box popup message to all online users.", "DL-Popup", ChatAuthorizationLevel.Admin)]
        public static void PopupToAll(User callingUser, string message)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendPopup(SharedCommands.CommandInterface.Eco, callingUser, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Sends an OK box popup message to the specified user.", "DL-PopupUser", ChatAuthorizationLevel.Admin)]
        public static void PopupToUser(User callingUser, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendPopup(SharedCommands.CommandInterface.Eco, callingUser, message, recipientUserNameOrID);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays an info panel to all online users.", "DL-InfoPanel", ChatAuthorizationLevel.Admin)]
        public static void InfoPanelToAll(User callingUser, string title, string message)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendInfoPanel(SharedCommands.CommandInterface.Eco, callingUser, DLConstants.ECO_PANEL_NOTIFICATION, title, message, string.Empty);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Displays an info panel to the specified user.", "DL-InfoPanelUser", ChatAuthorizationLevel.Admin)]
        public static void InfoPanelToUser(User callingUser, string title, string message, string recipientUserNameOrID)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.SendInfoPanel(SharedCommands.CommandInterface.Eco, callingUser, DLConstants.ECO_PANEL_NOTIFICATION, title, message, recipientUserNameOrID);
            }, callingUser);
        }

        #endregion

        #region Invites

        [ChatSubCommand("DiscordLink", "Posts the Discord invite message to the target user. The invite will be broadcasted if no target user is specified.", "DL-Invite", ChatAuthorizationLevel.User)]
        public static void Invite(User callingUser, string targetUserName = "")
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.DiscordInvite(SharedCommands.CommandInterface.Eco, callingUser, targetUserName);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Posts the Discord invite message to the Eco chat.", "DL-BroadcastInvite", ChatAuthorizationLevel.User)]
        public static void BroadcastInvite(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.DiscordInvite(SharedCommands.CommandInterface.Eco, callingUser, string.Empty);
            }, callingUser);
        }

        #endregion

        #region Account Linking

        [ChatSubCommand("DiscordLink", "Presents information about account linking.", "DL-LinkInfo", ChatAuthorizationLevel.User)]
        public static void LinkInformation(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                DisplayCommandData(callingUser, DLConstants.ECO_PANEL_DL_MESSAGE_MEDIUM, $"Eco --> Discord Account Linking", MessageBuilder.Shared.GetLinkAccountInfoMessage(SharedCommands.CommandInterface.Eco));
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Links the calling user account to a Discord account.", "DL-Link", ChatAuthorizationLevel.User)]
        public static void LinkDiscordAccount(User callingUser, string discordName)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                var plugin = Plugins.DiscordLink.DiscordLink.Obj;

                if (!plugin.Client.BotHasIntent(DiscordIntents.GuildMembers))
                {
                    ReportCommandError(callingUser, $"This server is not configured to use account linking as the bot lacks the elevated Guild Members Intent.");
                    return;
                }

                // Find the Discord user
                DiscordMember matchingMember = null;
                IReadOnlyCollection<DiscordMember> guildMembers = plugin.Client.GetGuildMembersAsync().Result;
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
                    ReportCommandError(callingUser, $"No Discord account with the name \"{discordName}\" could be found.\nUse {MessageUtils.GetCommandTokenForContext(SharedCommands.CommandInterface.Eco)}DL-LinkInfo for linking instructions.");
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
                            ReportCommandInfo(callingUser, $"Eco account is already linked to this Discord account.\nUse {MessageUtils.GetCommandTokenForContext(SharedCommands.CommandInterface.Eco)}DL-Unlink to remove the existing link.");
                        else
                            ReportCommandInfo(callingUser, $"Eco account is already linked to a different Discord account.\nUse {MessageUtils.GetCommandTokenForContext(SharedCommands.CommandInterface.Eco)}DL-Unlink to remove the existing link.");
                        return;
                    }
                    else if (linkedUser.DiscordID == matchingMember.Id.ToString())
                    {
                        ReportCommandError(callingUser, "Discord account is already linked to a different Eco account.");
                        return;
                    }
                }

                // Create a linked user from the combined Eco and Discord info
                UserLinkManager.AddLinkedUser(callingUser, matchingMember.Id.ToString(), matchingMember.Guild.Id.ToString());

                // Notify the Discord account that a link has been made and ask for verification
                DiscordMessage message = plugin.Client.SendDMAsync(matchingMember, null, MessageBuilder.Discord.GetVerificationDM(callingUser)).Result;
                _ = message.CreateReactionAsync(DLConstants.ACCEPT_EMOJI);
                _ = message.CreateReactionAsync(DLConstants.DENY_EMOJI);

                // Notify the Eco user that the link has been created and that verification is required
                ReportCommandInfo(callingUser, $"Your account has been linked.\nThe link requires verification before becoming active.\nInstructions have been sent to the linked Discord account.");
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Unlinks the calling user account from a linked Discord account.", "DL-Unlink", ChatAuthorizationLevel.User)]
        public static void UnlinkDiscordAccount(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
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

        [ChatSubCommand("DiscordLink", "Displays available trades by player, tag, item or store.", "DL-Trades", ChatAuthorizationLevel.User)]
        public static void Trades(User callingUser, string searchName)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.Trades(SharedCommands.CommandInterface.Eco, callingUser, searchName);
            }, callingUser);
        }

        // Wrapper for the Trades command in order to facilitate more command aliases
        [ChatSubCommand("DiscordLink", "Displays available trades by player, tag, item or store.", "DLT", ChatAuthorizationLevel.User)]
        public static void Trade(User user, string searchName)
        {
            Trades(user, searchName);
        }

        [ChatSubCommand("DiscordLink", "Creates a live updated display of available trades by player, tag, item or store", "DL-WatchTradeDisplay", ChatAuthorizationLevel.User)]
        public static void AddTradeWatcherDisplay(User callingUser, string searchName)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.AddTradeWatcher(SharedCommands.CommandInterface.Eco, callingUser, searchName, Modules.ModuleType.Display);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes the live updated display of available trades for the player, tag, item or store.", "DL-UnwatchTradeDisplay", ChatAuthorizationLevel.User)]
        public static void RemoveTradeWatcherDisplay(User callingUser, string searchName)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.RemoveTradeWatcher(SharedCommands.CommandInterface.Eco, callingUser, searchName, Modules.ModuleType.Display);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Creates a feed where the bot will post trades filtered by the search query, as they occur ingame. The search query can filter by player, tag, item or store.", "DL-WatchTradeFeed", ChatAuthorizationLevel.User)]
        public static void AddTradeWatcherFeed(User callingUser, string searchName)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.AddTradeWatcher(SharedCommands.CommandInterface.Eco, callingUser, searchName, Modules.ModuleType.Feed);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Removes the trade watcher feed for a player, tag, item or store.", "DL-UnwatchTradeFeed", ChatAuthorizationLevel.User)]
        public static void RemoveTradeWatcherFeed(User callingUser, string searchName)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.RemoveTradeWatcher(SharedCommands.CommandInterface.Eco, callingUser, searchName, Modules.ModuleType.Feed);
            }, callingUser);
        }

        [ChatSubCommand("DiscordLink", "Lists all trade watchers for the calling user.", "DL-TradeWatchers", ChatAuthorizationLevel.User)]
        public static void ListTradeWatchers(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.ListTradeWatchers(SharedCommands.CommandInterface.Eco, callingUser);
            }, callingUser);
        }

        #endregion

        #region Snippets

        [ChatSubCommand("DiscordLink", "Post a predefined snippet from Discord.", "DL-Snippet", ChatAuthorizationLevel.User)]
        public static void Snippet(User callingUser, string snippetKey = "")
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                SharedCommands.Snippet(SharedCommands.CommandInterface.Eco, callingUser, SharedCommands.CommandInterface.Eco, callingUser.Name, snippetKey);
            }, callingUser);
        }

        #endregion

#pragma warning restore CS4014 // Call not awaited
    }
}
