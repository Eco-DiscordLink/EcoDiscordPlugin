using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink
{
    public class Logger
    {
        public static void Debug(string message)
        {
            Log.Write("DISCORDLINK DEBUG:" + message);
        }

        public static void Info(string message)
        {
            Log.Write("DISCORDLINK: " + message);
        }

        public static void Error(string message)
        {
            Log.Write("DISCORDLINK ERROR:" + message);
        }
    }
}