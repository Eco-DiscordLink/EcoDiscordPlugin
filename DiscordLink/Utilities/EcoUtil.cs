using Eco.EM.Framework.ChatBase;
using Eco.Gameplay.Players;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class EcoUtil
    {
        public static void SendServerMessage(string message, bool permanent = false, User user = null )
        {
            ChatBase.MessageType messageType = permanent ? ChatBase.MessageType.Permanent : ChatBase.MessageType.Temporary;
            SendMessageOfType(null, message, messageType, user);
        }

        public static void SendPopupMessage(string message, User user = null)
        {
            SendMessageOfType(null, message, ChatBase.MessageType.Popup, user);
        }

        public static void SendAnnouncementMessage(string title, string message, User user = null)
        {
            SendMessageOfType(title, message, ChatBase.MessageType.Announcement, user);
        }

        private static void SendMessageOfType(string title, string message, ChatBase.MessageType messageType, User user )
        {
            switch(messageType)
            {
                case ChatBase.MessageType.Temporary:
                case ChatBase.MessageType.Permanent:
                case ChatBase.MessageType.Popup:
                    if (user == null)
                    {
                        ChatBase.Send(new ChatBase.Message(message, messageType));
                    }
                    else
                    {
                        ChatBase.Send(new ChatBase.Message(message, user, messageType));
                    }
                    break;

                case ChatBase.MessageType.Announcement:
                    if (user == null)
                    {
                        ChatBase.Send(new ChatBase.Message(title, message));
                    }
                    else
                    {
                        ChatBase.Send(new ChatBase.Message(title, message, user));
                    }
                    break;
            }
        }
    }
}
