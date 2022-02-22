using DSharpPlus.Entities;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class DiscordRelayFeed : Feed
    {
        public override string ToString()
        {
            return "Discord Relay Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.DiscordMessageSent;
        }

        protected override async Task<bool> ShouldRun()
        {
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
            IEnumerable<RelayChannelLink> relayLinks = DLConfig.GetRelayChannelLinks(message.GetChannel());

            foreach (RelayChannelLink relayLink in relayLinks)
            {
                ChannelLinkMentionPermissions chatlinkPermissions = new()
                {
                    AllowRoleMentions = relayLink.AllowRoleMentions,
                    AllowMemberMentions = relayLink.AllowUserMentions,
                    AllowChannelMentions = relayLink.AllowChannelMentions,
                };

                var originId = message.GetChannel().Id;

                if (originId != relayLink.Channel.Id)
                    ForwardMessageToDiscordChannel(message, relayLink.SecondChannel, relayLink.HereAndEveryoneMentionPermission, chatlinkPermissions);
                else if (originId != relayLink.SecondChannel.Id)
                    ForwardMessageToDiscordChannel(message, relayLink.SecondChannel, relayLink.HereAndEveryoneMentionPermission, chatlinkPermissions);
            }
        }

        private async void ForwardMessageToDiscordChannel(DiscordMessage message, DiscordChannel channel, GlobalMentionPermission globalMentionPermission, ChannelLinkMentionPermissions chatlinkPermissions)
        {
            Logger.DebugVerbose($"Relaying message from Discord channel {message.Channel.Name} to Discord channel {channel.Name}");
            
            bool allowGlobalMention = globalMentionPermission == GlobalMentionPermission.AnyUser
                || globalMentionPermission == GlobalMentionPermission.Admin && await DiscordLink.Obj.Client.UserIsAdmin(message.Author, channel.Guild);

            _ = DiscordLink.Obj.Client.SendMessageAsync(channel, MessageUtils.FormatMessageForDiscord(message.Content, channel, message.Author.Username, allowGlobalMention, chatlinkPermissions));
            ++_opsCount;
        }
    }
}
