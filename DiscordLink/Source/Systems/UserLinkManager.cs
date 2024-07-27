using DSharpPlus.Entities;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.Message;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using User = Eco.Gameplay.Players.User;

namespace Eco.Plugins.DiscordLink
{
    public sealed class UserLinkManager
    {
        public static event EventHandler<LinkedUser> OnLinkedUserVerified;
        public static event EventHandler<LinkedUser> OnLinkedUserRemoved;

        public static async void Initialize()
        {
            await LoadMembers();
            PruneObsoleteMemberReferences();
        }

        public static LinkedUser LinkedUserByDiscordUser(DiscordUser user, object caller = null, string callingReason = null, bool requireValid = true)
        {
            string DiscordIdStr = user.Id.ToString();
            LinkedUser result = DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.DiscordID == DiscordIdStr && (linkedUser.Valid || !requireValid));
            if (result == null && caller != null && callingReason != null)
                ReportLinkLookupFailure(caller, callingReason);

            // Ensure that the user exists both in Eco and in Discord
            if (requireValid && result != null && (result.EcoUser == null || result.DiscordMember == null))
                return null;

            return result;
        }

        public static LinkedUser LinkedUserByDiscordID(ulong DiscordId, object caller = null, string callingReason = null, bool requireValid = true)
        {
            string DiscordIdStr = DiscordId.ToString();
            LinkedUser result = DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.DiscordID == DiscordIdStr && (linkedUser.Valid || !requireValid));
            if (result == null && caller != null && callingReason != null)
                ReportLinkLookupFailure(caller, callingReason);

            // Ensure that the user exists both in Eco and in Discord
            if (requireValid && result != null && (result.EcoUser == null || result.DiscordMember == null))
                return null;

            return result;
        }

        public static LinkedUser LinkedUserByEcoID(string SlgOrSteamId, object caller = null, string callingReason = null, bool requireValid = true)
        {
            if (string.IsNullOrEmpty(SlgOrSteamId))
                return null;

            LinkedUser result = DLStorage.PersistentData.LinkedUsers.Find(linkedUser => (linkedUser.StrangeID == SlgOrSteamId || linkedUser.SteamID == SlgOrSteamId) && (linkedUser.Valid || !requireValid));
            if (result == null && caller != null && callingReason != null)
                ReportLinkLookupFailure(caller, callingReason);

            // Ensure that the user exists both in Eco and in Discord
            if (requireValid && result != null && (result.EcoUser == null || result.DiscordMember == null))
                return null;

            return result;
        }

        public static LinkedUser LinkedUserByEcoUser(User user, object caller = null, string callingReason = null, bool requireValid = true)
        {
            LinkedUser result = null;
            result = DLStorage.PersistentData.LinkedUsers.Find(linkedUser => linkedUser.HasAnyID(user.StrangeId, user.SteamId) && (linkedUser.Valid || !requireValid));

            if (result == null && caller != null && callingReason != null)
                ReportLinkLookupFailure(caller, callingReason);

            // Ensure that the user exists both in Eco and in Discord
            if (requireValid && result != null && (result.EcoUser == null || result.DiscordMember == null))
                return null;

            return result;
        }

        public static void ReportLinkLookupFailure(object caller, string callingContext)
        {
            if (caller is User user)
                Message.SendErrorBoxToUser(user, $"{callingContext} Failed\nYou have not linked your Discord Account to DiscordLink on this Eco Server.\nUse the `/DL LinkAccount` command to initiate account linking.");
            else if (caller is DiscordMember member)
                _ = DiscordLink.Obj.Client.SendDMAsync(member, $"**{callingContext} Failed**\nYou have not linked your Discord Account to DiscordLink on this Eco Server.\nUse the `/DL LinkAccount` command in Eco to initiate account linking.");
            else
                Logger.Error("Attempted to fetch a linked user using an invalid caller argument");
        }

        public static LinkedUser AddLinkedUser(User user, string discordId, string guildId)
        {
            LinkedUser linkedUser = new LinkedUser(user.StrangeId, user.SteamId, discordId, guildId);
            DLStorage.PersistentData.LinkedUsers.Add(linkedUser);
            DLStorage.Instance.Write();
            return linkedUser;
        }

        public static async Task<bool> VerifyLinkedUser(ulong discordUserId)
        {
            // Find the linked user for the sender and mark them as verified
            LinkedUser user = LinkedUserByDiscordID(discordUserId, requireValid: false);
            bool result = user != null && !user.Verified;
            if (result)
            {
                await user.LoadDiscordMember();
                user.Verified = true;
                DLStorage.Instance.Write();

                if (user.DiscordMember != null)
                    Message.SendInfoBoxToUser(user.EcoUser, $"Discord link to {user.DiscordMember.DisplayName} verified!");
                else
                    Logger.Error($"Newly verified account link between {user.EcoUser} and Discord account with ID {user.DiscordID} failed to load Discord member");

                OnLinkedUserVerified?.Invoke(null, user);
            }
            return result;
        }

        public static bool RemoveLinkedUser(User user)
        {
            bool deleted = false;
            LinkedUser linkedUser = LinkedUserByEcoUser(user, requireValid: false);
            if (linkedUser != null)
            {
                RemoveLinkedUser(linkedUser);
                deleted = true;
            }
            return deleted;
        }

        public static void RemoveLinkedUser(LinkedUser linkedUser, bool shouldFlush = true)
        {
            if (linkedUser.Valid)
                OnLinkedUserRemoved?.Invoke(null, linkedUser);

            DLStorage.PersistentData.LinkedUsers.Remove(linkedUser);
            if(shouldFlush)
            {
                DLStorage.Instance.Write();
            }
        }

        public static async Task HandleEvent(DLEventType eventType, params object[] data)
        {
            switch (eventType)
            {
                case DLEventType.DiscordReactionAdded:
                    {
                        DiscordUser user = data[0] as DiscordUser;
                        DiscordMessage message = data[1] as DiscordMessage;
                        DiscordEmoji emoji = data[2] as DiscordEmoji;

                        DiscordClient client = DiscordLink.Obj.Client;
                        DiscordChannel channel = message.GetChannel();
                        if (channel == null || !channel.IsPrivate)
                            return;

                        if (emoji != DLConstants.ACCEPT_EMOJI && emoji != DLConstants.DENY_EMOJI)
                            return;

                        string response = string.Empty;
                        LinkedUser linkedUser = LinkedUserByDiscordUser(user, requireValid: false);
                        if (linkedUser != null)
                        {
                            if (emoji == DLConstants.DENY_EMOJI)
                            {
                                response = "Link removed";
                                RemoveLinkedUser(linkedUser);
                            }
                            else if (emoji == DLConstants.ACCEPT_EMOJI)
                            {
                                if (await VerifyLinkedUser(user.Id))
                                    response = "Link verified";
                                else
                                    response = "Link verification failed - Unknown error";
                            }
                        }
                        else
                        {
                            response = "Link verification failed - No outstanding link request";
                        }

                        client.SendMessageAsync(channel, response).Wait();
                        _ = client.DeleteMessageAsync(message);
                        break;
                    }

                case DLEventType.DiscordMemberRemoved:
                    {
                        DiscordMember member = data[0] as DiscordMember;
                        LinkedUser linkedUser = LinkedUserByDiscordID(member.Id);
                        if(linkedUser != null)
                        {
                            Logger.Debug($"Removing linked user {member.Username} due to leaving guild");
                            RemoveLinkedUser(linkedUser);
                        }
                        break;
                    }

                default:
                    break;
            }
        }

        private static async Task LoadMembers()
        {
            foreach (LinkedUser user in DLStorage.PersistentData.LinkedUsers)
            {
                if (!user.Verified || string.IsNullOrEmpty(user.DiscordID))
                {
                    ++user.FailedInitializationCount;
                    continue;
                }

                await user.LoadDiscordMember();
            }
        }

        private static void PruneObsoleteMemberReferences()
        {
            List<LinkedUser> toRemove = new List<LinkedUser>();
            foreach (LinkedUser user in DLStorage.PersistentData.LinkedUsers)
            {
                if (user.HasPassedFailedLookupThreshold())
                    toRemove.Add(user);
            }

            foreach (LinkedUser user in toRemove)
            {
                Logger.Debug($"Pruned obsolete reference to linked user with ID {user.SlgID}");
                RemoveLinkedUser(user, false);
            }

            if (toRemove.Count > 0)
            {
                DLStorage.Instance.Write();
            }
        }
    }

    public class LinkedUser
    {
        [JsonIgnore]
        public User EcoUser { get { return Lookups.UserByStrangeOrSteamID(StrangeID, SteamID); } }

        [JsonIgnore]
        public DiscordMember DiscordMember { get; private set; }

        [JsonIgnore]
        public bool Valid => Verified && EcoUser != null && DiscordMember != null;

        public readonly string StrangeID = string.Empty;
        public readonly string SteamID = string.Empty;
        public readonly string DiscordID = string.Empty;
        public readonly string GuildID = string.Empty;
        public bool Verified = false;
        public uint FailedInitializationCount = 0;

        public LinkedUser(string slgID, string steamID, string discordID, string guildID)
        {
            this.StrangeID = slgID;
            this.SteamID = steamID;
            this.DiscordID = discordID;
            this.GuildID = guildID;
        }

        public async Task LoadDiscordMember()
        {
            try
            {
                DiscordMember = await DiscordLink.Obj.Client.Guild.GetMemberAsync(ulong.Parse(DiscordID));
                FailedInitializationCount = 0;
            }
            catch (DSharpPlus.Exceptions.NotFoundException)
            {
                ++FailedInitializationCount;
                Logger.Debug($"Failed to load linked Discord member with ID: {DiscordID}. Fail count = {FailedInitializationCount}");
            }
        }

        public bool HasAnyID(string SlgID, string SteamID)
        {
            return (!string.IsNullOrEmpty(this.SteamID) && this.SteamID == SteamID) || (!string.IsNullOrEmpty(this.StrangeID) && this.StrangeID == SlgID);
        }

        public bool HasPassedFailedLookupThreshold()
        {
            return FailedInitializationCount >= DLConstants.USER_LINK_FAILED_LOOKUP_REMOVAL_THRESHOLD;
        }
    }

    public class EcoUser
    {
        public EcoUser(string SlgID, string SteamID)
        {
            this.StrangeID = SlgID;
            this.SteamID = SteamID;
        }

        public bool HasAnyID(string SlgID, string SteamID)
        {
            return (!string.IsNullOrEmpty(this.SteamID) && this.SteamID == SteamID) || (!string.IsNullOrEmpty(this.StrangeID) && this.StrangeID == SlgID);
        }

        [JsonIgnore]
        public User GetUser { get { return Lookups.UserByStrangeOrSteamID(StrangeID, SteamID); } }

        public readonly string StrangeID = string.Empty;
        public readonly string SteamID = string.Empty;
    }
}
