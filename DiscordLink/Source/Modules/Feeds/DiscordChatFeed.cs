using DSharpPlus.Entities;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
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

        protected override bool ShouldRun()
        {
            foreach(ChatChannelLink link in DLConfig.Data.ChatChannelLinks)
            {
                if (link.IsValid() && (link.Direction == ChatSyncDirection.DiscordToEco || link.Direction == ChatSyncDirection.Duplex))
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (!(data[0] is DiscordMessage message))
                return;

            ChatChannelLink channelLink = DLConfig.ChatLinkForEcoChannel(message.Channel.Name) ?? DLConfig.ChatLinkForEcoChannel(message.Channel.Id.ToString());
            if (channelLink == null)
                return;

            if (channelLink.Direction == ChatSyncDirection.DiscordToEco || channelLink.Direction == ChatSyncDirection.Duplex)
                await ForwardMessageToEcoChannel(plugin, message, channelLink.EcoChannel);
        }

        private async Task ForwardMessageToEcoChannel(DiscordLink plugin, DiscordMessage message, string ecoChannel)
        {
            Logger.DebugVerbose($"Sending Discord message to Eco channel: {ecoChannel}");
            ChatManager.SendChat(await MessageUtils.FormatMessageForEco(message, ecoChannel), plugin.EcoUser);
            ++_opsCount;
        }
    }
}
