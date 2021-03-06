﻿using Eco.EM.Framework.ChatBase;
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
        public static IEnumerable<Law> ActiveLaws => CivicsData.Obj.Laws.All<Law>().Where(x => x.State == Shared.Items.ProposableState.Active);

        public static User GetUserbyName(string targetUserName)
        {
            return UserManager.FindUserByName(targetUserName);
        }

        public static User GetOnlineUserbyName(string targetUserName)
        {
            string lowerTargetUserName = targetUserName.ToLower();
            return UserManager.OnlineUsers.FirstOrDefault(user => user.Name.ToLower() == lowerTargetUserName);
        }

        public static bool SendServerMessage(string message, bool permanent = false, User user = null )
        {
            ChatBase.MessageType messageType = permanent ? ChatBase.MessageType.Permanent : ChatBase.MessageType.Temporary;
            return SendMessageOfType(null, message, messageType, user);
        }

        public static bool SendPopupMessage(string message, User user = null)
        {
            return SendMessageOfType(null, message, ChatBase.MessageType.Popup, user);
        }

        public static bool SendAnnouncementMessage(string title, string message, User user = null)
        {
            return SendMessageOfType(title, message, ChatBase.MessageType.Announcement, user);
        }

        private static bool SendMessageOfType(string title, string message, ChatBase.MessageType messageType, User user)
        {
            bool result = false;
            switch (messageType)
            {
                case ChatBase.MessageType.Temporary:
                case ChatBase.MessageType.Permanent:
                case ChatBase.MessageType.Popup:
                    if (user == null)
                        result = ChatBase.Send(new ChatBase.Message(message, messageType));
                    else
                        result = ChatBase.Send(new ChatBase.Message(message, user, messageType));
                    break;

                case ChatBase.MessageType.Announcement:
                    if (user == null)
                        result = ChatBase.Send(new ChatBase.Message(title, message));
                    else
                        result = ChatBase.Send(new ChatBase.Message(title, message, user));
                    break;
            }
            return result;
        }
    }
}
