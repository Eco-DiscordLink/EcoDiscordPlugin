﻿using DSharpPlus.Entities;
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
            return DLEventType.DiscordMessage;
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

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, object data)
        {
            if (!(data is DiscordMessage message)) return;

            ChatChannelLink channelLink = plugin.GetLinkForEcoChannel(message.Channel.Name) ?? plugin.GetLinkForEcoChannel(message.Channel.Id.ToString());
            string channel = channelLink?.EcoChannel;
            if (string.IsNullOrWhiteSpace(channel)) return;

            if (channelLink.Direction == ChatSyncDirection.DiscordToEco || channelLink.Direction == ChatSyncDirection.Duplex)
            {
                await ForwardMessageToEcoChannel(plugin, message, channel);
            }
        }

        private async Task ForwardMessageToEcoChannel(DiscordLink plugin, DiscordMessage message, string ecoChannel)
        {
            Logger.DebugVerbose("Sending Discord message to Eco channel: " + ecoChannel);
            ChatManager.SendChat(await MessageUtil.FormatMessageForEco(message, ecoChannel),  plugin.EcoUser);
            ++_opsCount;
        }
    }
}
