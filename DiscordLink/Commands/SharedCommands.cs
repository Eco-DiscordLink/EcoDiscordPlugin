using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Platform independent implementations of commands used in both Discord and Eco.
     */
    public static class SharedCommands
    {
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
    }
}
