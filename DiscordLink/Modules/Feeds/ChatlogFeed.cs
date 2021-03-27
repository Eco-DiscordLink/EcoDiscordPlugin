using DSharpPlus.Entities;
using Eco.Gameplay.GameActions;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class ChatlogFeed : Feed
    {
        public bool Initialized { get; private set; } = false;

        private const int CHATLOG_FLUSH_TIMER_INTERAVAL_MS = 60000; // 1 minute interval
        private StreamWriter _writer;
        private Timer _flushTimer = null;

        public override string ToString()
        {
            return "Chat Log Feed";
        }

        protected override DLEventType GetTriggers()
        {
            return DLEventType.EcoMessage | DLEventType.DiscordMessage;
        }

        protected override bool ShouldRun()
        {
            return DLConfig.Data.LogChat;
        }

        protected override async Task Initialize()
        {
            StartLogging();
            await base.Initialize();
        }

        protected override async Task Shutdown()
        {
            await base.Shutdown();
            StopLogging();
        }

        protected override async Task OnConfigChanged(object sender, EventArgs e)
        {
            using (await _overlapLock.LockAsync()) // Avoid crashes caused by data being manipulated and used simultaneously
            {
                StopLogging();
                StartLogging();
            }
            await base.OnConfigChanged(sender, e);
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DLEventType trigger, params object[] data)
        {
            if (!Initialized) return;

            string username;
            string content;
            if (data[0] is ChatSent ecoMessage)
            {
                username = ecoMessage.Citizen.Name;
                content = ecoMessage.Message;
            }
            else if(data[0] is DiscordMessage discordMessage)
            {
                username = discordMessage.Author.Username;
                content = discordMessage.Content;
            }
            else
            {
                return;
            }

            DateTime time = DateTime.Now;
            int utcOffset = TimeZoneInfo.Local.GetUtcOffset(time).Hours;
            _writer.WriteLine("[Discord] [" + DateTime.Now.ToString("yyyy-MM-dd : HH:mm", CultureInfo.InvariantCulture) + " UTC " + (utcOffset != 0 ? (utcOffset >= 0 ? "+" : "-") + utcOffset : "") + "] "
                + $"{MessageUtil.StripTags(username) + ": " + MessageUtil.StripTags(content)}");
            ++_opsCount;
        }

        private void StartLogging()
        {
            if (Initialized) return;

            DLConfigData config = DLConfig.Data;
            try
            {
                SystemUtil.EnsurePathExists(config.ChatlogPath);
                _writer = new StreamWriter(config.ChatlogPath, append: true);
                Initialized = true;
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to initialize the chat logger using path \"" + config.ChatlogPath + "\". Error message: " + e);
            }

            if (Initialized)
            {
                _flushTimer = new Timer(innerArgs =>
                {
                    lock (_overlapLock)
                    {
                        Flush();
                    }
                }, null, 0, CHATLOG_FLUSH_TIMER_INTERAVAL_MS);
            }
        }

        private void StopLogging()
        {
            if (!Initialized) return;

            SystemUtil.StopAndDestroyTimer(ref _flushTimer);
            try
            {
                _writer.Close();
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to close the chatlog file writer. Error message: " + e);
            }

            _writer = null;
            Initialized = false;
        }

        private void Flush()
        {
            try
            {
                _writer.Flush();
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to write the chatlog to file. Error message: " + e);
            }
        }
    }
}
