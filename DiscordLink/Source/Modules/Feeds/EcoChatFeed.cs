using DSharpPlus.Entities;
using Eco.Gameplay.GameActions;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
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

        protected override bool ShouldRun()
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

            ChatChannelLink chatLink = DLConfig.ChatLinkForEcoChannel(message.Tag.Substring(1)); // Remove the # character from the start.
            if (chatLink == null)
                return;

            if (chatLink.Direction == ChatSyncDirection.EcoToDiscord || chatLink.Direction == ChatSyncDirection.Duplex)
                ForwardMessageToDiscordChannel(message, chatLink.Guild, chatLink.Channel, chatLink.HereAndEveryoneMentionPermission);
        }

        private void ForwardMessageToDiscordChannel(ChatSent chatMessage, DiscordGuild guild, DiscordChannel channel, GlobalMentionPermission globalMentionPermission)
        {
            Logger.DebugVerbose($"Sending Eco message to Discord channel {channel.Name} in guild {guild.Name}");

            bool allowGlobalMention = (globalMentionPermission == GlobalMentionPermission.AnyUser
                || globalMentionPermission == GlobalMentionPermission.Admin && chatMessage.Citizen.IsAdmin);

            _ = DiscordLink.Obj.Client.SendMessageAsync(channel, MessageUtil.FormatMessageForDiscord(chatMessage.Message, channel, chatMessage.Citizen.Name, allowGlobalMention));
            ++_opsCount;
        }
    }
}
