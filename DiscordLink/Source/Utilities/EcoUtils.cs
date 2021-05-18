using Eco.Core.Systems;
using Eco.EM.Framework.ChatBase;
using Eco.Gameplay.Civics;
using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Laws;
using Eco.Gameplay.Civics.Titles;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Shared.Items;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class EcoUtils
    {
        public enum BoxMessageType
        {
            Info,
            Warning,
            Error
        }

        #region Lookups

        public static User UserByName(string userName) => UserManager.FindUserByName(userName);
        public static User UserByEcoID(int userID) => UserManager.FindUserByID(userID);
        public static User UserByNameOrEcoID(string userNameOrID) => int.TryParse(userNameOrID, out int ID) ? UserByEcoID(ID) : UserByName(userNameOrID);
        public static User UserBySteamOrSLGID(string steamID, string slgID) => UserManager.FindUserById(steamID, slgID);
        public static User OnlineUserByName(string userName) => UserManager.OnlineUsers.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(userName));
        public static User OnlineUserByEcoID(int userID) => UserManager.OnlineUsers.FirstOrDefault(user => user.Id == userID);
        public static User OnlineUserByNameEcoID(string userNameOrID) => int.TryParse(userNameOrID, out int ID) ? OnlineUserByEcoID(ID) : OnlineUserByName(userNameOrID);
        public static User OnlineUserBySteamOrSLGDID(string steamID, string slgID) => UserManager.OnlineUsers.FirstOrDefault(user => user.SteamId.Equals(steamID) || user.SlgId.Equals(slgID));

        public static IEnumerable<Election> ActiveElections => ElectionManager.Obj.CurrentElections.Where(election => election.Valid() && election.State == Shared.Items.ProposableState.Active);
        public static Election ActiveElectionByName(string electionName) => ActiveElections.FirstOrDefault(election => election.Name.EqualsCaseInsensitive(electionName));
        public static Election ActiveElectionByID(int electionID) => ActiveElections.FirstOrDefault(election => election.Id == electionID);
        public static Election ActiveElectionByNameOrID(string electionNameOrID) => int.TryParse(electionNameOrID, out int ID) ? ActiveElectionByID(ID) : ActiveElectionByName(electionNameOrID);

        public static IEnumerable<Law> ActiveLaws => CivicsData.Obj.Laws.All<Law>().Where(law => law.State == ProposableState.Active);
        public static Law ActiveLawByName(string lawName) => ActiveLaws.FirstOrDefault(law => law.Name.EqualsCaseInsensitive(lawName));
        public static Law ActiveLawByID(int lawID) => ActiveLaws.FirstOrDefault(law => law.Id == lawID);
        public static Law ActiveLawByNameByNameOrID(string lawNameOrID) => int.TryParse(lawNameOrID, out int ID) ? ActiveLawByID(ID) : ActiveLawByName(lawNameOrID);

        public static IEnumerable<WorkParty> ActiveWorkParties => Registrars.Get<WorkParty>().All<WorkParty>().NonNull().Where(wp => wp.State == ProposableState.Active);
        public static WorkParty ActiveWorkPartyByName(string workPartyName) => ActiveWorkParties.FirstOrDefault(wp => wp.Name.EqualsCaseInsensitive(workPartyName));
        public static WorkParty ActiveWorkPartyByID(int workPartyID) => ActiveWorkParties.FirstOrDefault(wp => wp.Id == workPartyID);
        public static WorkParty ActiveWorkPartyByNameOrID(string workPartyNameOrID) => int.TryParse(workPartyNameOrID, out int ID) ? ActiveWorkPartyByID(ID) : ActiveWorkPartyByName(workPartyNameOrID);

        public static IEnumerable<Demographic> ActiveDemographics => DemographicManager.Obj.ActiveAndValidDemographics;
        public static Demographic ActiveDemographicByName(string demographicName) => ActiveDemographics.FirstOrDefault(demographic => demographic.Name.EqualsCaseInsensitive(demographicName));
        public static Demographic ActiveDemographicByID(int demographicID) => ActiveDemographics.FirstOrDefault(demographic => demographic.Id == demographicID);
        public static Demographic ActiveDemographicByNameOrID(string demographicNameOrID) => int.TryParse(demographicNameOrID, out int ID) ? ActiveDemographicByID(ID) : ActiveDemographicByName(demographicNameOrID);

        public static IEnumerable<Title> ActiveTitles => TitleManager.Obj.ActiveTitles;
        public static Title ActiveTitleByName(string titleName) => ActiveTitles.FirstOrDefault(title => title.Name.EqualsCaseInsensitive(titleName));
        public static Title ActiveTitleByID(int titleID) => ActiveTitles.FirstOrDefault(title => title.Id == titleID);
        public static Title ActiveTitleByNameOrID(string titleNameOrID) => int.TryParse(titleNameOrID, out int ID) ? ActiveTitleByID(ID) : ActiveTitleByName(titleNameOrID);

        public static IEnumerable<Currency> Currencies => CurrencyManager.Currencies;
        public static Currency CurrencyByName(string currencyName) => Currencies.FirstOrDefault(c => c.Name.EqualsCaseInsensitive(currencyName));
        public static Currency CurrencyByID(int currencyID) => Currencies.FirstOrDefault(c => c.Id == currencyID);
        public static Currency CurrencyByNameOrID(string currencyNameOrID) => int.TryParse(currencyNameOrID, out int ID) ? CurrencyByID(ID) : CurrencyByName(currencyNameOrID);

        public static IEnumerable<Deed> Deeds => PropertyManager.Obj.Deeds;
        public static Deed DeedByName(string deedName) => Deeds.FirstOrDefault(deed => deed.Name.EqualsCaseInsensitive(deedName));
        public static Deed DeedByID(long deedID) => Deeds.FirstOrDefault(deed => deed.Id.ToLong() == deedID);
        public static Deed DeedByNameOrID(string deedNameOrID) => long.TryParse(deedNameOrID, out long ID) ? DeedByID(ID) : DeedByName(deedNameOrID);

        #endregion

        #region Message Sending

        public static bool SendServerMessageToUser(User user, bool permanent, string message)
        {
            ChatBase.MessageType messageType = permanent ? ChatBase.MessageType.Permanent : ChatBase.MessageType.Temporary;
            return ChatBaseExtended.CBChat(message, user, messageType);
        }

        public static bool SendServerMessageToAll(bool permanent, string message)
        {
            ChatBase.MessageType messageType = permanent ? ChatBase.MessageType.Permanent : ChatBase.MessageType.Temporary;
            return ChatBaseExtended.CBChat(message, messageType);
        }

        public static bool SendOKBoxToUser(User user, string message)
        {
            return ChatBaseExtended.CBOkBox(message, user);
        }

        public static bool SendOKBoxToAll(string message)
        {
            return ChatBaseExtended.CBOkBox(message);
        }

        public static bool SendInfoBoxToUser(User user, string message)
        {
            return ChatBaseExtended.CBInfoBox(message, user);
        }

        public static bool SendInfoBoxToAll(string message)
        {
            return ChatBaseExtended.CBInfoBox(message);
        }

        public static bool SendWarningBoxToAll(string message)
        {
            return ChatBaseExtended.CBWarning(message);
        }

        public static bool SendWarningBoxToUser(User user, string message)
        {
            return ChatBaseExtended.CBWarning(message, user);
        }

        public static bool SendErrorBoxToUser(User user, string message)
        {
            return ChatBaseExtended.CBError(message, user);
        }

        public static bool SendErrorBoxToAll(string message)
        {
            return ChatBaseExtended.CBError(message);
        }

        public static bool SendNotificationToUser(User user, string message)
        {
            return ChatBaseExtended.CBMail(message, user);
        }

        public static bool SendNotificationToAll(string message)
        {
            return ChatBaseExtended.CBMail(message);
        }

        public static bool SendInfoPanelToUser(User user, string title, string message)
        {
            return ChatBaseExtended.CBInfoPane(title, message, user, messageType: true);
        }

        public static bool SendInfoPanelToAll(string title, string message)
        {
            return ChatBaseExtended.CBInfoPane(title, message, messageType: true);
        }

        #endregion
    }
}
