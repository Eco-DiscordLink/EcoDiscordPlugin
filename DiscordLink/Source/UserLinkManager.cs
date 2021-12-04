using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Eco.Plugins.DiscordLink
{
    public sealed class UserLinkManager
    {
        public static event EventHandler<LinkedUser> OnLinkedUserVerified;
        public static event EventHandler<LinkedUser> OnLinkedUserRemoved;

        public static LinkedUser LinkedUserByDiscordUser(DiscordUser user, object caller = null, string callingReason = null, bool requireVerification = true)
        {
            string DiscordIdStr = user.Id.ToString();
            LinkedUser result = DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.DiscordID == DiscordIdStr && (linkedUser.Verified || !requireVerification));
            if (result == null && caller != null && callingReason != null)
                ReportLinkLookupFailure(caller, callingReason);

            return result;
        }

        public static LinkedUser LinkedUserByDiscordID(ulong DiscordId, object caller = null, string callingReason = null, bool requireVerification = true)
        {
            string DiscordIdStr = DiscordId.ToString();
            LinkedUser result = DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.DiscordID == DiscordIdStr && (linkedUser.Verified || !requireVerification));
            if (result == null && caller != null && callingReason != null)
                ReportLinkLookupFailure(caller, callingReason);

            return result;
        }

        public static LinkedUser LinkedUserByEcoID(string SlgOrSteamId, object caller = null, string callingReason = null, bool requireVerification = true)
        {
            if (string.IsNullOrEmpty(SlgOrSteamId))
                return null;

            LinkedUser result = DLStorage.PersistentData.LinkedUsers.Find(linkedUser => (linkedUser.SlgID == SlgOrSteamId || linkedUser.SteamID == SlgOrSteamId) && (linkedUser.Verified || !requireVerification));
            if (result == null && caller != null && callingReason != null)
                ReportLinkLookupFailure(caller, callingReason);

            return result;
        }

        public static LinkedUser LinkedUserByEcoUser(User user, object caller = null, string callingReason = null, bool requireVerification = true)
        {
            LinkedUser result = null;

            if (user.SlgId != null)
                result = DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.SlgID == user.SlgId && (linkedUser.Verified || !requireVerification));
            else if (user.SteamId != null)
                result = DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.SteamID == user.SteamId && (linkedUser.Verified || !requireVerification));
            else
                return null;

            if (result == null && caller != null && callingReason != null)
                ReportLinkLookupFailure(caller, callingReason);

            return result;
        }

        public static void ReportLinkLookupFailure(object caller, string callingContext)
        {
            if (caller is User user)
                EcoUtils.SendErrorBoxToUser(user, $"{callingContext} Failed\nYou have not linked your Discord Account to DiscordLink on this Eco Server.\nUse the `\\DL-Link` command to initiate account linking.");
            else if (caller is DiscordMember member)
                _ = DiscordLink.Obj.Client.SendDMAsync(member, $"**{callingContext} Failed**\nYou have not linked your Discord Account to DiscordLink on this Eco Server.\nUse the `\\DL-Link` command in Eco to initiate account linking.");
            else
                Logger.Error("Attempted to fetch a linked user using an invalid caller argument");
        }

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
            LinkedUser user = LinkedUserByDiscordID(discordUserId, requireVerification: false);
            bool result = user != null && !user.Verified;
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
                RemoveLinkedUser(linkedUser);
                deleted = true;
            }
            return deleted;
        }

        public static void RemoveLinkedUser(LinkedUser linkedUser)
        {
            if (linkedUser.Verified)
                OnLinkedUserRemoved?.Invoke(null, linkedUser);

            DLStorage.PersistentData.LinkedUsers.Remove(linkedUser);
            DLStorage.Instance.Write();
        }

        public static void HandleEvent(DLEventType eventType, params object[] data)
        {
            switch(eventType)
            {
                case DLEventType.DiscordReactionAdded:
                    DiscordUser user = data[0] as DiscordUser;
                    DiscordMessage message = data[1] as DiscordMessage;
                    DiscordEmoji emoji = data[2] as DiscordEmoji;

                    if (!message.Channel.IsPrivate)
                        return;

                    if (emoji != DLConstants.AcceptEmoji && emoji != DLConstants.DenyEmoji)
                        return;

                    string response = string.Empty;
                    LinkedUser linkedUser = LinkedUserByDiscordUser(user, requireVerification: false);
                    if(linkedUser != null)
                    {
                        if (emoji == DLConstants.DenyEmoji)
                        {
                            response = "Link removed";
                            RemoveLinkedUser(linkedUser);
                        }
                        else if (emoji == DLConstants.AcceptEmoji)
                        {
                            if (VerifyLinkedUser(user.Id))
                                response = "Link verified";
                            else
                                response = "Link verification failed - Unknown error";
                        }
                    }
                    else
                    {
                        response = "Link verification failed - No outstanding link request";
                    }

                    DLDiscordClient client = DiscordLink.Obj.Client;
                    client.SendMessageAsync(message.Channel, response).Wait();
                    _ = client.DeleteMessageAsync(message);
                    break;

                default:
                    break;
            }
        }
    }

    public class LinkedUser
    {
        [JsonIgnore]
        public User EcoUser { get { return EcoUtils.UserBySteamOrSLGID(SteamID, SlgID); } }

        [JsonIgnore]
        public DiscordMember DiscordMember { get { return !string.IsNullOrEmpty(DiscordID) ? DiscordLink.Obj.Client.GuildByNameOrID(GuildID)?.Members.Values.FirstOrDefault(member => member.Id.ToString() == DiscordID) : null; } }

        public LinkedUser(string slgID, string steamID, string discordID, string guildID)
        {
            this.SlgID = slgID;
            this.SteamID = steamID;
            this.DiscordID = discordID;
            this.GuildID = guildID;
        }

        public readonly string SlgID = string.Empty;
        public readonly string SteamID = string.Empty;
        public readonly string DiscordID = string.Empty;
        public readonly string GuildID = string.Empty;
        public bool Verified = false;
    }
}
