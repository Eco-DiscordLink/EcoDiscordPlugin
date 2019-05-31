using System;
using System.Collections.Generic;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Interfaces;
using Eco.Shared.Services;

public class IChatMessageProviderChatServerWrapper : IChatMessageProvider
{
    public IEnumerable<ChatMessage> GetChatMessages(double startTime, double endTime)
    {
        return ChatServer.GetPlayerMessages(startTime, endTime);
    }
}