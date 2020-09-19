using Eco.Shared.Localization;
using Eco.Shared.Utils;
using System.IO;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class Logger
    {
        private static StreamWriter _writer = new StreamWriter(Directory.GetCurrentDirectory() + "\\Mods\\DiscordLink\\Pluginlog.txt", append: true)
        {
            AutoFlush = true,
        };

        public static void DebugVerbose(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.DebugVerbose)
            {
                Log.Write(new LocString("[DiscordLink] DEBUG: " + message + "\n"));
            }
        }

        public static void Debug(string message)
        {
            string fullMessage = "[DiscordLink] DEBUG: " + message + "\n";
            _writer?.Write(fullMessage);
            if (DLConfig.Data.LogLevel <= LogLevel.Debug)
            {
                Log.Write(new LocString(fullMessage));
            }
        }

        public static void Warning(string message)
        {
            string fullMessage = "[DiscordLink] WARNING: " + message + "\n";
            _writer?.Write(fullMessage);
            if (DLConfig.Data.LogLevel <= LogLevel.Warning)
            {
                Log.Write(new LocString(fullMessage));   
            }
        }

        public static void Info(string message)
        {
            string fullMessage = "[DiscordLink] " + message + "\n";
            _writer?.Write(fullMessage);
            if (DLConfig.Data.LogLevel <= LogLevel.Information)
            {
                Log.Write(new LocString(fullMessage));   
            }
        }

        public static void Error(string message)
        {
            string fullMessage = "[DiscordLink] ERROR: " + message + "\n";
            _writer?.Write(fullMessage);
            if (DLConfig.Data.LogLevel <= LogLevel.Error)
            {
                Log.Write(new LocString(fullMessage));   
            }
        }
    }
}
