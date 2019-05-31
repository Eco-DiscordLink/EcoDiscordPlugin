using System;
using System.Collections.Generic;
using Eco.Shared.Services;

namespace Eco.Plugins.DiscordLink.Interfaces
{
    public interface IChatMessageProvider
    {
        IEnumerable<ChatMessage> GetChatMessages(double startTime = Double.MinValue, double endTime = Double.MaxValue);
    }
}