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
    public class DiscordChatFeed : FeedModule
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
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (!(data[0] is DiscordMessage message))
                return;
            IEnumerable<ChatChannelLink> chatLinks = DLConfig.ChatLinksForDiscordChannel(message.GetChannel());

            foreach (ChatChannelLink chatLink in chatLinks
                .Where(link => link.Direction == ChatSyncDirection.EcoToDiscord || link.Direction == ChatSyncDirection.Duplex))
            {
                await ForwardMessageToEcoChannel(plugin, message, chatLink.EcoChannel);
            }
        }

        private async Task ForwardMessageToEcoChannel(DiscordLink plugin, DiscordMessage message, string ecoChannel)
        {
            Logger.DebugVerbose($"Sending Discord message to Eco channel: {ecoChannel}");
            EcoUtils.SendChatRaw(await MessageUtils.FormatMessageForEco(message, ecoChannel));
            ++_opsCount;
        }
    }
}
