using System;
using System.Linq;
using System.Threading;
using Eco.Core.Utils;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Services;
using Eco.Shared.Utils;
using Eco.Simulation.Time;

namespace Eco.Plugins.DiscordLink
{
    public class ChatNotifier
    {
        public ThreadSafeAction<ChatMessage> OnMessageReceived { get; set; } = new ThreadSafeAction<ChatMessage>();

        private double lastCheckTime = Double.MaxValue;
        private const int POLL_DELAY = 500;

        public void Initialize()
        {
            lastCheckTime = WorldTime.Seconds;
        }
        
        public void Run()
        {
            while (true)
            {
                var newMessages = ChatServer.GetPlayerMessages(lastCheckTime);
                newMessages.ForEach(message =>
                {
                    Log.Write("Message found, invoking callback.");
                    OnMessageReceived.Invoke(message);
                });
                lastCheckTime = WorldTime.Seconds;
                Thread.Sleep(POLL_DELAY);
            }
        }
    }
}