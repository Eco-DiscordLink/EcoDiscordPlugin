using Eco.EM.Framework.ChatBase;
using Eco.Gameplay.Civics;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Laws;
using Eco.Gameplay.Players;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class EcoUtil
    {
        // Getters
        public static IEnumerable<Election> ActiveElections => ElectionManager.Obj.CurrentElections.Where(x => x.Valid() && x.State == Shared.Items.ProposableState.Active);
        public static IEnumerable<Law> Laws => CivicsData.Obj.Laws.All<Law>();

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
