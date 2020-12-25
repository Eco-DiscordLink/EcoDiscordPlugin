using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Localization;
using Eco.Shared.Networking;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
    }
}
