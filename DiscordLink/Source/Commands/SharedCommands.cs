using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Networking;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Platform independent implementations of commands used in both Discord and Eco.
     */
    public static class SharedCommands
    {
        #region Commands Base

        public enum CommandSource
        {
            Eco,
            Discord
        }

        private static void ReportCommandError(CommandSource source, object callContext, string message)
        {
            if (source == CommandSource.Eco)
                EcoCommands.ReportCommandError(callContext as User, message);
            else
                DiscordCommands.ReportCommandError(callContext as CommandContext, message);
        }

        private static void ReportCommandInfo(CommandSource source, object callContext, string message)
        {
            if (source == CommandSource.Eco)
                EcoCommands.ReportCommandInfo(callContext as User, message);
            else
                DiscordCommands.ReportCommandInfo(callContext as CommandContext, message);
        }

        private static void DisplayCommandData(CommandSource source, object callContext, string title, object data)
        {
            if (source == CommandSource.Eco)
                EcoCommands.DisplayCommandData(callContext as User, title, data as string);
            else
                DiscordCommands.DisplayCommandData(callContext as CommandContext, title, data as DiscordLinkEmbed);
        }

        #endregion

        #region Plugin Management

        public static string ResetWorldData()
        public static void Restart(CommandSource source, object callContext)
        {
            DiscordLink plugin = DiscordLink.Obj;
            Logger.Info("Restart command executed - Restarting");
            bool restarted = plugin.RestartClient().Result;

            string result;
            if (restarted)
            {
                result = "Restarting...";
                ReportCommandInfo(source, callContext, result);
            }
            else
            {
                result = "Restart failed or a restart was already in progress";
                ReportCommandError(source, callContext, result);
            }

            Logger.Info(result);
        }
        {
            Logger.Info("ResetWorldData command invoked - Resetting world storage data");
            DLStorage.Instance.Reset();
            return "World storage data has been reset.";
        }

        #endregion

        #region Message Relaying

        public static string SendServerMessage(string message, string senderName, string recipientUserName, string persistanceType)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Message cannot be empty.";

            bool permanent;
            if (persistanceType.EqualsCaseInsensitive("temporary"))
                permanent = true;
            else if (persistanceType.EqualsCaseInsensitive("permanent"))
                permanent = false;
            else
                return "Persistance type must either be \"Temporary\" or \"Permanent\".";

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserName))
            {
                recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.EqualsCaseInsensitive(recipientUserName));
                if (recipient == null)
                    return $"No online user with the name \"{recipientUserName}\" could be found.";
            }

            bool result = EcoUtil.SendServerMessage($"[{senderName}] {message}", permanent, recipient);
            return result ? "Message delivered." : "Failed to send message.";
        }

        public static string SendPopup(string message, string senderName, string recipientUserName)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Message cannot be empty.";

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserName))
            {
                recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.EqualsCaseInsensitive(recipientUserName));
                if (recipient == null)
                    return $"No online user with the name \"{recipientUserName}\" could be found.";
            }

            bool result = EcoUtil.SendPopupMessage($"[{senderName}]\n\n{message}", recipient);
            return result ? "Message delivered." : "Failed to send message.";
        }

        public static string SendAnnouncement(string title, string message, string senderName, string recipientUserName)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "Title cannot be empty";

            if (string.IsNullOrWhiteSpace(message))
                return "Message cannot be empty";

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserName))
            {
                recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.EqualsCaseInsensitive(recipientUserName));
                if (recipient == null)
                    return $"No online user with the name \"{recipientUserName}\" could be found.";
            }

            bool result = EcoUtil.SendAnnouncementMessage(title, $"{message}\n\n[{senderName}]", recipient);
            return result ? "Message delivered." : "Failed to send message.";
        }

        #endregion

        #region Invites

        public static bool DiscordInvite(CommandSource source, object callContext, string targetUserName)
        {
            DLConfigData config = DLConfig.Data;
            ServerInfo serverInfo = Networking.NetworkManager.GetServerInfo();

            string inviteMessage = config.InviteMessage;
            if (!inviteMessage.Contains(DLConfig.InviteCommandLinkToken) || string.IsNullOrEmpty(serverInfo.DiscordAddress))
            {
                ReportCommandError(source, callContext, "This server is not configured for using the Invite commands.");
                return false;
            }

            User targetUser = null; // If no target user is specified; use broadcast
            if (!string.IsNullOrEmpty(targetUserName))
            {
                targetUser = UserManager.FindUserByName(targetUserName);

                if (targetUser == null)
                {
                    User offlineUser = EcoUtil.GetUserbyName(targetUserName);
                    if (offlineUser != null)
                        ReportCommandError(source, callContext, $"{MessageUtil.StripTags(offlineUser.Name)} is not online");
                    else
                        ReportCommandError(source, callContext, $"Could not find user with name {targetUserName}");
                    return false;
                }
            }

            inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConfig.InviteCommandLinkToken), serverInfo.DiscordAddress);

            bool sent = EcoUtil.SendServerMessage(inviteMessage, permanent: true, targetUser);
            if (sent)
                ReportCommandInfo(source, callContext, "Invite sent.");
            else
                ReportCommandError(source, callContext, "Failed to send invite.");

            return sent;
        }

        #endregion

        #region Trades

        public static bool Trades(CommandSource source, object callContext, string searchName, out string matchedName)
        {
            matchedName = string.Empty;

            if (string.IsNullOrWhiteSpace(searchName))
            {
                ReportCommandInfo(source, callContext, "Please provide the name of an item, a tag or a player to search for.");
                return false;
            }

            matchedName = TradeUtil.GetMatchAndOffers(searchName, out TradeTargetType offerType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
            if(offerType == TradeTargetType.Invalid)
            {
                ReportCommandError(source, callContext, $"No item, tag or player with the name \"{searchName}\" could be found.");
                return false;
            }

            if(source == CommandSource.Eco)
            {
                MessageBuilder.Eco.FormatTrades(offerType, groupedBuyOffers, groupedSellOffers, out string message);
                DisplayCommandData(source, callContext, matchedName, message);
            }
            else
            {
                MessageBuilder.Discord.FormatTrades(matchedName, offerType, groupedBuyOffers, groupedSellOffers, out DiscordLinkEmbed embed );
                DisplayCommandData(source, callContext, matchedName, embed);
            }

            return true;
        }

        public static bool TrackTrades(CommandSource source, object callContext, string userOrItemName)
        {
            LinkedUser linkedUser = source == CommandSource.Eco
                ? LinkedUserManager.LinkedUserByEcoUser(callContext as User)
                : LinkedUserManager.LinkedUserByDiscordID((callContext as CommandContext).Member.Id);
            if (linkedUser == null)
            {
                ReportCommandError(source, callContext, $"You have not linked your Discord Account to DiscordLink on this Eco Server.\nUse the `\\dl-link` command to initialize account linking.");
                return false;
            }

            int trackedTradesCount = DLStorage.WorldData.GetTrackedTradesCountForUser(ulong.Parse(linkedUser.DiscordID));
            if (trackedTradesCount >= DLConfig.Data.MaxTrackedTradesPerUser)
            {
                ReportCommandError(source, callContext, $"You are already tracking {trackedTradesCount} trades and the limit is {DLConfig.Data.MaxTrackedTradesPerUser} tracked trades per user.\nUse the `\\dl-StopTrackTrades` command to remove a tracked trade to make space if you wish to add a new one.");
                return false;
            }

            // Fetch trade data using the trades command once to see that the command parameters are valid and get the name of the matched target
            if (!Trades(source, callContext, userOrItemName, out string matchedName))
                return false;

            bool added = DLStorage.WorldData.AddTrackedTradeItem(ulong.Parse(linkedUser.DiscordID), matchedName).Result;
            if (added)
            {
                ReportCommandInfo(source, callContext, $"Tracking all trades for {matchedName}.");
                return true;
            }
            else
            {
                ReportCommandError(source, callContext, $"Failed to start tracking trades for {matchedName}.");
                return false;
            }
        }

        public static bool StopTrackTrades(CommandSource source, object callContext, string userOrItemName)
        {
            LinkedUser linkedUser = source == CommandSource.Eco
                ? LinkedUserManager.LinkedUserByEcoUser(callContext as User)
                : LinkedUserManager.LinkedUserByDiscordID((callContext as CommandContext).Member.Id);
            if (linkedUser == null)
            {
                ReportCommandError(source, callContext, $"You have not linked your Discord Account to DiscordLink on this Eco Server.\nLog into the game and use the `\\dl-link` command to initialize account linking.");
                return false;
            }

            bool removed = DLStorage.WorldData.RemoveTrackedTradeItem(ulong.Parse(linkedUser.DiscordID), userOrItemName).Result;
            if (removed)
            {
                ReportCommandInfo(source, callContext, $"Stopped tracking trades for {userOrItemName}.");
                return true;
            }
            else
            {
                ReportCommandError(source, callContext, $"Failed to stop tracking trades for {userOrItemName}.\nUse `\\dl-ListTrackedStores` to see what is currently being tracked.");
                return false;
            }
        }

        public static bool ListTrackedTrades(CommandSource source, object callContext)
        {
            LinkedUser linkedUser = source == CommandSource.Eco
                ? LinkedUserManager.LinkedUserByEcoUser(callContext as User)
                : LinkedUserManager.LinkedUserByDiscordID((callContext as CommandContext).Member.Id);
            if (linkedUser == null)
            {
                ReportCommandError(source, callContext, $"You have not linked your Discord Account to DiscordLink on this Eco Server.\nLog into the game and use the `\\dl-link` command to initialize account linking.");
                return false;
            }

            DisplayCommandData(source, callContext, "Tracked Trades", DLStorage.WorldData.ListTrackedTrades(ulong.Parse(linkedUser.DiscordID)));
            return true;
        }

        #endregion
    }
}
