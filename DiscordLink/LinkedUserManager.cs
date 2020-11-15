using Eco.Gameplay.Players;
using Newtonsoft.Json;

namespace Eco.Plugins.DiscordLink
{
    public sealed class LinkedUserManager
    {
        public static LinkedUser AddLinkedUser(User user, string discordId)
        {
            LinkedUser linkedUser = new LinkedUser(user.SlgId, user.SteamId, discordId);
            DLStorage.PersistantData.LinkedUsers.Add(linkedUser);
            DLStorage.Instance.Write();
            return linkedUser;
        }

        public static bool RemoveLinkedUser(User user)
        {
            bool deleted = false;
            LinkedUser linkedUser = LinkedUserByEcoUser(user, requireVerification: false);
            if (linkedUser != null)
            {
                DLStorage.PersistantData.LinkedUsers.Remove(linkedUser);
                deleted = true;
                DLStorage.Instance.Write();
            }
            return deleted;
        }

        public static LinkedUser LinkedUserByDiscordId(string DiscordId, bool requireVerification = true)
        {
            if (string.IsNullOrEmpty(DiscordId))
                return null;

            return DLStorage.PersistantData.LinkedUsers.Find(linkedUser => (linkedUser.DiscordId == DiscordId) && linkedUser.Verified || !requireVerification);
        }

        public static LinkedUser LinkedUserByEcoID(string SlgOrSteamId, bool requireVerification = true)
        {
            if (string.IsNullOrEmpty(SlgOrSteamId))
                return null;

            return DLStorage.PersistantData.LinkedUsers.Find(linkedUser => (linkedUser.SlgId == SlgOrSteamId || linkedUser.SteamId == SlgOrSteamId) && (linkedUser.Verified || !requireVerification));
        }

        public static LinkedUser LinkedUserByEcoUser(User user, bool requireVerification = true)
        {
            if (user.SlgId != null)
                return DLStorage.PersistantData.LinkedUsers.Find(linkedUser => linkedUser.SlgId == user.SlgId && (linkedUser.Verified || !requireVerification));
            else if (user.SteamId != null)
                return DLStorage.PersistantData.LinkedUsers.Find(linkedUser => linkedUser.SteamId == user.SteamId && (linkedUser.Verified || !requireVerification));    
            else
                return null;
        }
    }

    public class LinkedUser
    {
        [JsonIgnore]
        public string EcoId { get { return !string.IsNullOrEmpty(SlgId) ? SlgId : SteamId; } }

        public readonly string SlgId = string.Empty;
        public readonly string SteamId = string.Empty;
        public readonly string DiscordId = string.Empty;
        public bool Verified = false;

        public LinkedUser(string SlgID, string Steam64Id, string DiscordId)
        {
            this.SlgId = SlgID;
            this.SteamId = Steam64Id;
            this.DiscordId = DiscordId;
        }
    }
}
