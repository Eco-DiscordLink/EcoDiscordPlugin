using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink
{
    public class Logger
    {
        public static void Debug(string message)
        {
            Log.Write("DISCORDLINK DEBUG:" + message + "\n");
        }

        public static void DebugVerbose(string message)
        {
            if (DiscordLink.Obj?.DiscordPluginConfig.Debug == true)
            {
                Log.Write("DISCORDLINK DEBUG:" + message + "\n");
            }
        }

        public static void Info(string message)
        {
            Log.Write("DISCORDLINK: " + message + "\n");
        }

        public static void Error(string message)
        {
            Log.Write("DISCORDLINK ERROR: " + message + "\n");
        }
    }
}