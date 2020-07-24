using System;
using System.Linq;
using System.Threading;
using Eco.Core.Utils;
using Eco.Gameplay.Systems.Chat;
using Eco.Plugins.DiscordLink.Interfaces;
using Eco.Shared.Services;
using Eco.Shared.Utils;
using Eco.Simulation.Time;

namespace Eco.Plugins.DiscordLink
{
    public class ChatNotifier
    {
        public ThreadSafeAction<ChatMessage> OnMessageReceived { get; set; } = new ThreadSafeAction<ChatMessage>();

        private IChatMessageProvider chatMessageProvider;
        
        private double _lastCheckTime = Double.MaxValue;
        private const int POLL_DELAY = 500;

        public ChatNotifier(IChatMessageProvider chatMessageProvider)
        {
            this.chatMessageProvider = chatMessageProvider;
        }

        public void Initialize()
        {
            _lastCheckTime = WorldTime.Seconds;
        }

        public void ProcessMessagesAfterTime(double startTime)
        {
            var newMessages = chatMessageProvider.GetChatMessages(startTime);
            
            newMessages.ForEach(message =>
            {
                OnMessageReceived.Invoke(message);
            });
        }
        
        public void Run()
        {
            while (true)
            {
                var getMessagesAfterTime = _lastCheckTime;
                _lastCheckTime = WorldTime.Seconds;
                
                ProcessMessagesAfterTime(getMessagesAfterTime);
                
                Thread.Sleep(POLL_DELAY);
            }
        }
    }
}
