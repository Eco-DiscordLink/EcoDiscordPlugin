using Eco.EM.Framework.ChatBase;
using Eco.Gameplay.Civics;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Laws;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Players;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class EcoUtils
    {
        public static IEnumerable<Election> ActiveElections => ElectionManager.Obj.CurrentElections.Where(x => x.Valid() && x.State == Shared.Items.ProposableState.Active);
        public static IEnumerable<Law> ActiveLaws => CivicsData.Obj.Laws.All<Law>().Where(x => x.State == Shared.Items.ProposableState.Active);

        public static User GetUserbyName(string targetUserName) => UserManager.FindUserByName(targetUserName);
        public static User GetOnlineUserbyName(string targetUserName) => UserManager.OnlineUsers.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(targetUserName));

        public static Currency CurrencyByName(string currencyName) => CurrencyManager.Currencies.FirstOrDefault(c => c.Name.EqualsCaseInsensitive(currencyName));
        public static Currency CurrencyByID(int currencyID) => CurrencyManager.Currencies.FirstOrDefault(c => c.Id == currencyID);
        public static Currency CurrencyByNameOrID(string currencyNameOrID) => int.TryParse(currencyNameOrID, out int ID) ? CurrencyByID(ID) : CurrencyByName(currencyNameOrID);

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
