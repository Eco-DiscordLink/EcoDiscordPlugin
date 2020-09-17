using Eco.Shared.Localization;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public class Logger
    {
        public static void DebugVerbose(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.DebugVerbose)
            {
                Log.Write(new LocString("[DiscordLink] DEBUG: " + message + "\n"));
            }
        }

        public static void Debug(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Debug)
            {
                Log.Write(new LocString("[DiscordLink] DEBUG: " + message + "\n"));
            }
        }

        public static void Warning(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Warning)
            {
                Log.Write(new LocString("[DiscordLink] WARNING: " + message + "\n"));
            }
        }

        public static void Info(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Information)
            {
                Log.Write(new LocString("[DiscordLink] " + message + "\n"));
            }
        }

        public static void Error(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Error)
            {
                Log.Write(new LocString("[DiscordLink] ERROR: " + message + "\n"));
            }
        }
    }
}
