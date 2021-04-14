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
            LinkedUser user = LinkedUserByDiscordID(discordUserId, false);
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

        public static LinkedUser LinkedUserByDiscordID(ulong DiscordId, bool requireVerification = true)
        {
            string DiscordIdStr = DiscordId.ToString();
            return DLStorage.PersistentData.LinkedUsers.Find(linkedUser => (linkedUser.DiscordID == DiscordIdStr) && linkedUser.Verified || !requireVerification);
        }

        public static LinkedUser LinkedUserByEcoID(string SlgOrSteamId, bool requireVerification = true)
        {
            if (string.IsNullOrEmpty(SlgOrSteamId))
                return null;

            return DLStorage.PersistentData.LinkedUsers.Find(linkedUser => (linkedUser.SlgID == SlgOrSteamId || linkedUser.SteamID == SlgOrSteamId) && (linkedUser.Verified || !requireVerification));
        }

        public static LinkedUser LinkedUserByEcoUser(User user, bool requireVerification = true)
        {
            if (user.SlgId != null)
                return DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.SlgID == user.SlgId && (linkedUser.Verified || !requireVerification));
            else if (user.SteamId != null)
                return DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.SteamID == user.SteamId && (linkedUser.Verified || !requireVerification));    
            else
                return null;
        }
    }

    public class LinkedUser
    {
        [JsonIgnore]
        public string EcoID { get { return !string.IsNullOrEmpty(SlgID) ? SlgID : SteamID; } }

        public readonly string SlgID = string.Empty;
        public readonly string SteamID = string.Empty;
        public readonly string DiscordID = string.Empty;
        public readonly string GuildID = string.Empty;
        public bool Verified = false;

        public LinkedUser(string slgID, string steamID, string discordID, string guildID)
        {
            this.SlgID = slgID;
            this.SteamID = steamID;
            this.DiscordID = discordID;
            this.GuildID = guildID;
        }
    }
}
