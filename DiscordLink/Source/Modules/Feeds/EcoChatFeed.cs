using DSharpPlus.Entities;
using Eco.Gameplay.GameActions;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class EcoChatFeed : Feed
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
                ForwardMessageToDiscordChannel(message, chatLink.Channel, chatLink.UseTimestamp, chatLink.HereAndEveryoneMentionPermission, chatLink.MentionPermissions);
            }
        }

        private void ForwardMessageToDiscordChannel(ChatSent chatMessage, DiscordChannel channel, bool useTimestamp, GlobalMentionPermission globalMentionPermission, ChatLinkMentionPermissions chatlinkPermissions)
        {
            Logger.DebugVerbose($"Sending Eco message to Discord channel {channel.Name}");

            bool allowGlobalMention = globalMentionPermission == GlobalMentionPermission.AnyUser
                || globalMentionPermission == GlobalMentionPermission.Admin && chatMessage.Citizen.IsAdmin;

            _ = DiscordLink.Obj.Client.SendMessageAsync(channel, MessageUtils.FormatMessageForDiscord(chatMessage.Message, channel, chatMessage.Citizen.Name, useTimestamp, allowGlobalMention, chatlinkPermissions));
            ++_opsCount;
        }
    }
}
