using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class DiscordChatFeed : Feed
    {
        public override string ToString()
        {
            return "Discord Chat Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordMessageSent;
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (ChatChannelLink link in DLConfig.Data.ChatChannelLinks)
            {
                if (link.IsValid() && (link.Direction == ChatSyncDirection.DiscordToEco || link.Direction == ChatSyncDirection.Duplex))
                    return true;
            }
            foreach (RelayChannelLink link in DLConfig.Data.RelayChannelLinks)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (!(data[0] is DiscordMessage message))
                return;

            // to Eco
            IEnumerable<ChatChannelLink> chatLinks = DLConfig.ChatLinksForDiscordChannel(message.GetChannel())
                .Where(link => link.Direction == ChatSyncDirection.EcoToDiscord || link.Direction == ChatSyncDirection.Duplex);
            foreach (ChatChannelLink chatLink in chatLinks)
            {
                await ForwardMessageToEcoChannel(message, chatLink.EcoChannel);
            }

            // Relay to other Discord channel
            IEnumerable<RelayChannelLink> relayLinks = DLConfig.GetRelayChannelLinks(message.GetChannel());
            var originId = message.GetChannel().Id;
            Logger.DebugVerbose($"[Relay-Module] message origin: {originId}");

            Logger.DebugVerbose($"[Relay-Module] number of RelayChannelLinks: {relayLinks.Count()}");
            foreach (RelayChannelLink relayLink in relayLinks)
            {
                ChannelLinkMentionPermissions chatlinkPermissions = new()
                {
                    AllowRoleMentions = relayLink.AllowRoleMentions,
                    AllowMemberMentions = relayLink.AllowUserMentions,
                    AllowChannelMentions = relayLink.AllowChannelMentions,
                };

                if (originId != relayLink.Channel.Id)
                {
                    Logger.DebugVerbose($"[Relay-Module] should relay to channel: {relayLink.Channel.Name}");
                    ForwardMessageToDiscordChannel(plugin, message, relayLink.Channel, relayLink.HereAndEveryoneMentionPermission, chatlinkPermissions);
                }
                else if (originId != relayLink.SecondChannel.Id)
                {
                    Logger.DebugVerbose($"[Relay-Module] should relay to channel: {relayLink.Channel.Name}");
                    ForwardMessageToDiscordChannel(plugin, message, relayLink.SecondChannel, relayLink.HereAndEveryoneMentionPermission, chatlinkPermissions);
                }
            }
        }

        private async Task ForwardMessageToEcoChannel(DiscordMessage message, string ecoChannel)
        {
            Logger.DebugVerbose($"Sending Discord message to Eco channel: {ecoChannel}");
            EcoUtils.SendChatRaw(await MessageUtils.FormatMessageForEco(message, ecoChannel));
            ++_opsCount;
        }

        private async void ForwardMessageToDiscordChannel(DiscordLink plugin, DiscordMessage message, DiscordChannel channel, GlobalMentionPermission globalMentionPermission, ChannelLinkMentionPermissions chatlinkPermissions)
        {
            Logger.DebugVerbose($"Relaying message from Discord channel {message.Channel.Name} to Discord channel {channel.Name}");

            bool allowGlobalMention = globalMentionPermission == GlobalMentionPermission.AnyUser
                || globalMentionPermission == GlobalMentionPermission.Admin && await plugin.Client.UserIsAdmin(message.Author, channel.Guild);

            _ = plugin.Client.SendMessageAsync(channel, MessageUtils.FormatMessageForDiscord(message.Content, channel, message.Author.Username, allowGlobalMention, chatlinkPermissions));
            ++_opsCount;
        }
    }
}
