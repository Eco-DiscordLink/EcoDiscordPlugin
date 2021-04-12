using Eco.Core.Utils;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Localization;
using Eco.Shared.Networking;
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
        #region Plugin Management

        public static string ResetWorldData()
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
            string persistanceTypeLower = persistanceType.ToLower();
            if (persistanceTypeLower == "temporary")
                permanent = true;
            else if (persistanceTypeLower == "permanent")
                permanent = false;
            else
                return "Persistance type must either be \"Temporary\" or \"Permanent\".";

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserName))
            {
                recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.ToLower() == recipientUserName);
                if (recipient == null)
                    return "No online user with the name \"" + recipientUserName + "\" could be found.";
            }

            bool result = EcoUtil.SendServerMessage("[" + senderName + "] " + message, permanent, recipient);
            if (result)
                return "Message delivered.";
            else
                return "Failed to send message.";
        }

        public static string SendPopup(string message, string senderName, string recipientUserName)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Message cannot be empty.";

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserName))
            {
                recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.ToLower() == recipientUserName);
                if (recipient == null)
                    return "No online user with the name \"" + recipientUserName + "\" could be found.";
            }

            bool result = EcoUtil.SendPopupMessage("[" + senderName + "]\n\n" + message, recipient);
            if (result)
                return "Message delivered";
            else
                return "Failed to send message";
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
                recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.ToLower() == recipientUserName);
                if (recipient == null)
                    return "No online user with the name \"" + recipientUserName + "\" could be found.";
            }

            bool result = EcoUtil.SendAnnouncementMessage(title, message + "\n\n[" + senderName + "]", recipient);
            if (result)
                return "Message delivered";
            else
                return "Failed to send message";
        }

        #endregion

        #region Invites

        public static string DiscordInvite(User targetUser)
        {
            DLConfigData config = DLConfig.Data;
            ServerInfo serverInfo = Networking.NetworkManager.GetServerInfo();

            string inviteMessage = config.InviteMessage;
            if (!inviteMessage.Contains(DLConfig.InviteCommandLinkToken) || string.IsNullOrEmpty(serverInfo.DiscordAddress))
                return "This server is not configured for using the /DiscordInvite command.";

            inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConfig.InviteCommandLinkToken), serverInfo.DiscordAddress);
            bool result = EcoUtil.SendServerMessage(inviteMessage, permanent: true, targetUser);
            if (result)
                return "Invite sent";
            else
                return "Failed to send invite";
        }

        public static string BroadcastDiscordInvite(string ecoChannel)
        {
            DLConfigData config = DLConfig.Data;
            ServerInfo serverInfo = Networking.NetworkManager.GetServerInfo();

            string inviteMessage = config.InviteMessage;
            if (!inviteMessage.Contains(DLConfig.InviteCommandLinkToken) || string.IsNullOrEmpty(serverInfo.DiscordAddress))
                return "This server is not configured for using the /DiscordInvite command.";

            inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConfig.InviteCommandLinkToken), serverInfo.DiscordAddress);
            bool result = EcoUtil.SendServerMessage(inviteMessage, permanent: true);
            if (result)
                return "Invite sent";
            else
                return "Failed to send invite";
        }

        #endregion

        #region Trades

        public static string Trades(string searchName, out string matchedName, out TradeTargetType tradeType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers)
        {
            var plugin = DiscordLink.Obj;
            matchedName = string.Empty;
            groupedBuyOffers = null;
            groupedSellOffers = null;
            tradeType = TradeTargetType.Invalid;

            if (string.IsNullOrEmpty(searchName))
                return "Please provide the name of an item, a tag or a player to search for.";

            List<string> entries = new List<string>();
            var match = TradeUtil.MatchType(searchName);

            if (match.Is<Tag>())
            {
                var matchTag = match.Get<Tag>();
                matchedName = matchTag.Name;

                bool filter(StoreComponent store, TradeOffer offer) => offer.Stack.Item.Tags().Contains(matchTag);
                var sellOffers = TradeUtil.SellOffers(filter);
                groupedSellOffers = sellOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
                var buyOffers = TradeUtil.BuyOffers(filter);
                groupedBuyOffers = buyOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

                tradeType = TradeTargetType.Tag;
            }
            else if (match.Is<Item>())
            {
                var matchItem = match.Get<Item>();
                matchedName = matchItem.DisplayName;

                bool filter(StoreComponent store, TradeOffer offer) => offer.Stack.Item == matchItem;
                var sellOffers = TradeUtil.SellOffers(filter);
                groupedSellOffers = sellOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
                var buyOffers = TradeUtil.BuyOffers(filter);
                groupedBuyOffers = buyOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

                tradeType = TradeTargetType.Item;
            }
            else if (match.Is<User>())
            {
                var matchUser = match.Get<User>();
                matchedName = matchUser.Name;

                bool filter(StoreComponent store, TradeOffer offer) => store.Parent.Owners == matchUser;
                var sellOffers = TradeUtil.SellOffers(filter);
                groupedSellOffers = sellOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
                var buyOffers = TradeUtil.BuyOffers(filter);
                groupedBuyOffers = buyOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

                tradeType = TradeTargetType.User;
            }
            else
            {
                return $"No item, tag or player with the name \"{searchName}\" could be found.";
            }

            return string.Empty;
        }

        #endregion
    }
}
