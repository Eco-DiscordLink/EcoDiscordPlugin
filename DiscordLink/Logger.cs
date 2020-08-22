using Eco.Shared.Localization;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink
{
    public class Logger
    {
        public static void Debug(string message)
        {
            Log.Write(new LocString("[DiscordLink] DEBUG: " + message + "\n"));
        }

        public static void DebugVerbose(string message)
        {
            if (DLConfig.Data.Debug)
            {
                Log.Write(new LocString("[DiscordLink] DEBUG: " + message + "\n"));
            }
        }

        public static void Info(string message)
        {
            Log.Write(new LocString("[DiscordLink] " + message + "\n"));
        }

        public static void Error(string message)
        {
            Log.Write(new LocString("[DiscordLink] ERROR: " + message + "\n"));
        }
    }
}
