using System.IO;

namespace Eco.Plugins.DiscordLink
{
    public static class DLConstants
    {
        public const string ECHO_COMMAND_TOKEN = "[ECHO]";

        public const int DISCORD_MESSAGE_CHARACTER_LIMIT = 2000;
        public const int DISCORD_EMBED_CONTENT_CHARACTER_LIMIT = 5000;
        public const int DISCORD_EMBED_FIELD_CHARACTER_LIMIT = 900;
        public const int MAX_TOP_CURRENCY_HOLDER_DISPLAY_LIMIT = 15;

        public static string BasePath { get { return Directory.GetCurrentDirectory() + "\\Mods\\DiscordLink\\"; } }
    }
}
