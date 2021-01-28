using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Utilities;
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
        public static string Restart()
        {
            DiscordLink plugin = DiscordLink.Obj;
            Logger.Info("Eco Restart command executed - Restarting client");
            _ = plugin.RestartClient();
            return "DiscordLink restarted";
        }

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

            EcoUtil.SendServerMessage("[" + senderName + "] " + message, permanent, recipient);
            return "Message delivered.";
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

            EcoUtil.SendPopupMessage("[" + senderName + "]\n\n" + message, recipient);
            return "Message delivered.";
        }

        public static string SendAnnouncement(string title, string message, string senderName, string recipientUserName)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "Title cannot be empty.";

            if (string.IsNullOrWhiteSpace(message))
                return "Message cannot be empty.";

            User recipient = null;
            if (!string.IsNullOrWhiteSpace(recipientUserName))
            {
                recipient = UserManager.OnlineUsers.FirstOrDefault(x => x.Name.ToLower() == recipientUserName);
                if (recipient == null)
                    return "No online user with the name \"" + recipientUserName + "\" could be found.";
            }

            EcoUtil.SendAnnouncementMessage(title, message + "\n\n[" + senderName + "]", recipient);
            return "Message delivered.";
        }

        public static string Invite(string ecoChannel)
        {
            var plugin = DiscordLink.Obj;
            DLConfigData config = DLConfig.Data;
            ServerInfo serverInfo = Networking.NetworkManager.GetServerInfo();

            string inviteMessage = config.InviteMessage;
            if (!inviteMessage.Contains(DLConfig.InviteCommandLinkToken) || string.IsNullOrEmpty(serverInfo.DiscordAddress))
                return "This server is not configured for using the /DiscordInvite command.";

            inviteMessage = Regex.Replace(inviteMessage, Regex.Escape(DLConfig.InviteCommandLinkToken), serverInfo.DiscordAddress);
            string formattedInviteMessage = $"#{(string.IsNullOrEmpty(ecoChannel) ? config.EcoCommandOutputChannel : ecoChannel) } {inviteMessage}";
            ChatManager.SendChat(formattedInviteMessage, plugin.EcoUser);
            return "Invite sent";
        }

        public static string Trades(string userOrItemName, out string title, out bool isItem, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers)
        {
            var plugin = DiscordLink.Obj;
            title = string.Empty;
            groupedBuyOffers = null;
            groupedSellOffers = null;
            isItem = false;

            if (string.IsNullOrEmpty(userOrItemName))
                return "Please provide the name of an item or player to search for.";

            List<string> entries = new List<string>();
            var match = TradeUtil.MatchItemOrUser(userOrItemName);
            if (match.Is<Item>())
            {
                var matchItem = match.Get<Item>();
                title = "Trades for " + matchItem.DisplayName;

                Func<StoreComponent, TradeOffer, bool> filter = (store, offer) => offer.Stack.Item == matchItem;
                var sellOffers = TradeUtil.SellOffers(filter);
                groupedSellOffers = sellOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
                var buyOffers = TradeUtil.BuyOffers(filter);
                groupedBuyOffers = buyOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);

                isItem = true;
            }
            else if (match.Is<User>())
            {
                var matchUser = match.Get<User>();
                title = matchUser.Name;

                Func<StoreComponent, TradeOffer, bool> filter = (store, offer) => store.Parent.Owners == matchUser;
                var sellOffers = TradeUtil.SellOffers(filter);
                groupedSellOffers = sellOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
                var buyOffers = TradeUtil.BuyOffers(filter);
                groupedBuyOffers = buyOffers.GroupBy(t => TradeUtil.StoreCurrencyName(t.Item1)).OrderBy(g => g.Key);
            }
            else
            {
                return $"No player or item with the name \"{userOrItemName}\" could be found.";
            }

            return string.Empty;
        }

        public static string ResetWorldData()
        {
            Logger.Info("ResetWorldData command invoked - Resetting world storage data");
            DLStorage.Instance.Reset();
            return "World storage data has been reset.";
        }
    }
}
