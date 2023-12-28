using DSharpPlus.Entities;
using Eco.Moose.Tools;
using Eco.Gameplay.GameActions;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class EcoChatFeed : FeedModule
    {
        public override string ToString()
        {
            return "Eco Chat Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.EcoMessageSent;
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (ChatChannelLink link in DLConfig.Data.ChatChannelLinks)
            {
                if (link.IsValid() && link.Direction == ChatSyncDirection.EcoToDiscord || link.Direction == ChatSyncDirection.Duplex)
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (!(data[0] is ChatSent message))
                return;

            string ecoChannel = message.Tag.Substring(1); // Remove the # character from the start.
            IEnumerable<ChatChannelLink> chatLinks = DLConfig.ChatLinksForEcoChannel(ecoChannel);

            foreach (ChatChannelLink chatLink in chatLinks
                .Where(link => link.Direction == ChatSyncDirection.EcoToDiscord || link.Direction == ChatSyncDirection.Duplex))
            {
                await ForwardMessageToDiscordChannel(message, chatLink.Channel, chatLink.UseTimestamp, chatLink.HereAndEveryoneMentionPermission, chatLink.MentionPermissions);
            }
        }

        private async Task ForwardMessageToDiscordChannel(ChatSent chatMessage, DiscordChannel channel, bool useTimestamp, GlobalMentionPermission globalMentionPermission, ChatLinkMentionPermissions chatlinkPermissions)
        {
            Logger.DebugVerbose($"Sending Eco message to Discord channel {channel.Name}");

            bool blocked = false;
            string forwardedMessage = string.Empty;
            if (DLConfig.Data.ChatSyncMode == Enums.ChatSyncMode.OptOut)
            {
                if (DLStorage.PersistentData.OptedOutUsers.Any(u => (u.SteamID != string.Empty && u.SteamID == chatMessage.Citizen.SteamId) || (u.SlgID != string.Empty && u.SlgID == chatMessage.Citizen.SlgId)))
                {
                    forwardedMessage = $"[Blocked Message - Author opted out]";
                    blocked = true;
                }
            }
            else if (DLConfig.Data.ChatSyncMode == Enums.ChatSyncMode.OptIn)
            {
                if (DLStorage.PersistentData.OptedInUsers.None(u => (u.SteamID != string.Empty && u.SteamID == chatMessage.Citizen.SteamId) || (u.SlgID != string.Empty && u.SlgID == chatMessage.Citizen.SlgId)))
                {
                    forwardedMessage = $"[Blocked Message - Author not opted in]";
                    blocked = true;
                }
            }

            if (!blocked)
                forwardedMessage = chatMessage.Message;

            bool allowGlobalMention = globalMentionPermission == GlobalMentionPermission.AnyUser
                || globalMentionPermission == GlobalMentionPermission.Admin && chatMessage.Citizen.IsAdmin;

            await DiscordLink.Obj.Client.SendMessageAsync(channel, MessageUtils.FormatMessageForDiscord(forwardedMessage, channel, chatMessage.Citizen.Name, useTimestamp, allowGlobalMention, chatlinkPermissions));
            ++_opsCount;
        }
    }
}
