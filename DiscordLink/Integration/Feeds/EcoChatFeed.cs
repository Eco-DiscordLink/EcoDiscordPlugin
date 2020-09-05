using Eco.Gameplay.GameActions;
using Eco.Plugins.DiscordLink.Utilities;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public class EcoChatFeed : Feed
    {
        public EcoChatFeed()
        {
            _triggerTypeField = TriggerType.EcoMessage;
        }

        protected override void UpdateInternal(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (!(data is ChatSent message)) return;

            // Remove the # character from the start.
            var channelLink = plugin.GetLinkForDiscordChannel(message.Tag.Substring(1));
            var channel = channelLink?.DiscordChannel;
            var guild = channelLink?.DiscordGuild;

            if (!string.IsNullOrWhiteSpace(channel) && !string.IsNullOrWhiteSpace(guild))
            {
                ForwardMessageToDiscordChannel(plugin, message, channel, guild, channelLink.HereAndEveryoneMentionPermission);
            }
        }

        private void ForwardMessageToDiscordChannel(DiscordLink plugin, ChatSent chatMessage, string channelNameOrId, string guildNameOrId, GlobalMentionPermission globalMentionPermission)
        {
            Logger.DebugVerbose("Sending Eco message to Discord channel " + channelNameOrId + " in guild " + guildNameOrId);
            var guild = plugin.GuildByNameOrId(guildNameOrId);
            if (guild == null)
            {
                Logger.Error("Failed to forward Eco message from user " + MessageUtil.StripEcoTags(chatMessage.Citizen.Name) + " as no guild with the name or ID " + guildNameOrId + " exists");
                return;
            }
            var channel = guild.ChannelByNameOrId(channelNameOrId);
            if (channel == null)
            {
                Logger.Error("Failed to forward Eco message from user " + MessageUtil.StripEcoTags(chatMessage.Citizen.Name) + " as no channel with the name or ID " + channelNameOrId + " exists in the guild " + guild.Name);
                return;
            }

            bool allowGlobalMention = (globalMentionPermission == GlobalMentionPermission.AnyUser
                || globalMentionPermission == GlobalMentionPermission.Admin && chatMessage.Citizen.IsAdmin);

            _ = DiscordUtil.SendAsync(channel, MessageUtil.FormatMessageForDiscord(chatMessage.Message, channel, chatMessage.Citizen.Name, allowGlobalMention));
        }
    }
}
