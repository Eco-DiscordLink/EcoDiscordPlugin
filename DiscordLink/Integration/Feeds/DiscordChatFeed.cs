using DSharpPlus.Entities;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Utilities;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public class DiscordChatFeed : Feed
    {
        protected override TriggerType GetTriggers()
        {
            return TriggerType.DiscordMessage;
        }

        protected override void UpdateInternal(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (!(data is DiscordMessage message)) return;

            var channelLink = plugin.GetLinkForEcoChannel(message.Channel.Name) ?? plugin.GetLinkForEcoChannel(message.Channel.Id.ToString());
            var channel = channelLink?.EcoChannel;
            if (!string.IsNullOrWhiteSpace(channel))
            {
                ForwardMessageToEcoChannel(plugin, message, channel);
            }
        }

        private async void ForwardMessageToEcoChannel(DiscordLink plugin, DiscordMessage message, string ecoChannel)
        {
            Logger.DebugVerbose("Sending Discord message to Eco channel: " + ecoChannel);
            ChatManager.SendChat(MessageUtil.FormatMessageForEco(message, ecoChannel),  plugin.EcoUser);
        }
    }
}
