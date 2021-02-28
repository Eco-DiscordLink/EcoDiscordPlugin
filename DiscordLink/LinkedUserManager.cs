using Eco.Gameplay.Players;
using Newtonsoft.Json;
using System;

namespace Eco.Plugins.DiscordLink
{
    public sealed class LinkedUserManager
    {
        public static event EventHandler<LinkedUser> OnLinkedUserVerified;
        public static event EventHandler<LinkedUser> OnLinkedUserRemoved;

        public static LinkedUser AddLinkedUser(User user, string discordId, string guildId)
        {
            LinkedUser linkedUser = new LinkedUser(user.SlgId, user.SteamId, discordId, guildId);
            DLStorage.PersistentData.LinkedUsers.Add(linkedUser);
            DLStorage.Instance.Write();
            return linkedUser;
        }

        public static bool VerifyLinkedUser(ulong discordUserId)
        {
            // Find the linked user for the sender and mark them as verified
            LinkedUser user = LinkedUserByDiscordId(discordUserId, false);
            bool result = user != null;
            if (result)
            {
                user.Verified = true;
                DLStorage.Instance.Write();

                OnLinkedUserVerified?.Invoke(null, user);
            }
            return result;
        }

        public static bool RemoveLinkedUser(User user)
        {
            bool deleted = false;
            LinkedUser linkedUser = LinkedUserByEcoUser(user, requireVerification: false);
            if (linkedUser != null)
            {
                if(linkedUser.Verified)
                    OnLinkedUserRemoved?.Invoke(null, linkedUser);

                DLStorage.PersistentData.LinkedUsers.Remove(linkedUser);
                deleted = true;
                DLStorage.Instance.Write();
            }
            return deleted;
        }

        public static LinkedUser LinkedUserByDiscordId(ulong DiscordId, bool requireVerification = true)
        {
            string DiscordIdStr = DiscordId.ToString();
            return DLStorage.PersistentData.LinkedUsers.Find(linkedUser => (linkedUser.DiscordId == DiscordIdStr) && linkedUser.Verified || !requireVerification);
        }

        public static LinkedUser LinkedUserByEcoID(string SlgOrSteamId, bool requireVerification = true)
        {
            if (string.IsNullOrEmpty(SlgOrSteamId))
                return null;

            return DLStorage.PersistentData.LinkedUsers.Find(linkedUser => (linkedUser.SlgId == SlgOrSteamId || linkedUser.SteamId == SlgOrSteamId) && (linkedUser.Verified || !requireVerification));
        }

        public static LinkedUser LinkedUserByEcoUser(User user, bool requireVerification = true)
        {
            if (user.SlgId != null)
                return DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.SlgId == user.SlgId && (linkedUser.Verified || !requireVerification));
            else if (user.SteamId != null)
                return DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.SteamId == user.SteamId && (linkedUser.Verified || !requireVerification));    
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
        public readonly string GuildId = string.Empty;
        public bool Verified = false;

        public LinkedUser(string slgID, string steam64Id, string discordId, string guildId)
        {
            this.SlgId = slgID;
            this.SteamId = steam64Id;
            this.DiscordId = discordId;
            this.GuildId = guildId;
        }
    }
}
