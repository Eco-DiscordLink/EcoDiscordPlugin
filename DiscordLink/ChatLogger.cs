using DSharpPlus.Entities;
using Eco.Gameplay.GameActions;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Eco.Plugins.DiscordLink
{
    class ChatLogger
    {
        public bool Initialized { get { return _initialized; } }

        private const int CHATLOG_FLUSH_TIMER_INTERAVAL_MS = 60000; // 1 minute interval
        private bool _initialized = false;
        private StreamWriter _writer;
        private Timer _flushTimer = null;

        public void Start()
        {
            if (_initialized) return;

            DLConfigData config = DLConfig.Data;
            try
            {
                _writer = new StreamWriter(config.ChatlogPath, append: true);
                _initialized = true;
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred while attempting to initialize the chat logger using path \"" + config.ChatlogPath + "\". Error message: " + e);
            }

            if (_initialized)
            {
                _flushTimer = new Timer(async innerArgs =>
                {
                    Flush();
                }, null, 0, CHATLOG_FLUSH_TIMER_INTERAVAL_MS);
            }
        }

        public void Stop()
        {
            if (!_initialized) return;

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
            _initialized = false;
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void Write(DiscordMessage message)
        {
            if (!_initialized) return;

            DateTime time = DateTime.Now;
            int utcOffset = TimeZoneInfo.Local.GetUtcOffset(time).Hours;
            _writer.WriteLine("[Discord] [" + DateTime.Now.ToString("yyyy-MM-dd : HH:mm", CultureInfo.InvariantCulture) + " UTC " + (utcOffset != 0 ? (utcOffset >= 0 ? "+" : "-") + utcOffset : "") + "] "
                + $"{DiscordLink.StripTags(message.Author.Username) + ": " + DiscordLink.StripTags(message.Content)}");
        }

        public void Write(ChatSent message)
        {
            if (!_initialized) return;

            DateTime time = DateTime.Now;
            int utcOffset = TimeZoneInfo.Local.GetUtcOffset(time).Hours;
            _writer.WriteLine("[Eco] [" + DateTime.Now.ToString("yyyy-MM-dd : HH:mm", CultureInfo.InvariantCulture) + " UTC " + (utcOffset != 0 ? (utcOffset >= 0 ? "+" : "-") + utcOffset : "") + "] "
                + $"{DiscordLink.StripTags(message.Citizen.Name) + ": " + DiscordLink.StripTags(message.Message)}");
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
