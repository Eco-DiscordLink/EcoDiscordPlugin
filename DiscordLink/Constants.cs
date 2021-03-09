using System.IO;

namespace Eco.Plugins.DiscordLink
{
    public static class DLConstants
    {
        public const string ECHO_COMMAND_TOKEN = "[ECHO]";
        public const string DEFAULT_CHAT_CHANNEL = "General";

        public const int DISCORD_MESSAGE_CHARACTER_LIMIT = 2000;
        public const int DISCORD_EMBED_TITLE_CHARACTER_LIMIT = 256;
        public const int DISCORD_EMBED_FOOTER_CHARACTER_LIMIT = 2048;
        public const int DISCORD_EMBED_FOOTER_DESCRIPTION_LIMIT = 2048;
        public const int DISCORD_EMBED_FIELD_CHARACTER_LIMIT = 1024;
        public const int DISCORD_EMBED_FIELD_COUNT_LIMIT = 25;
        public const int DISCORD_EMBED_AUTHOR_NAME_CHARACTER_LIMIT = 256;
        public const int DISCORD_EMBED_TOTAL_CHARACTER_LIMIT = 6000;

        public const int MAX_TOP_CURRENCY_HOLDER_DISPLAY_LIMIT = 15;

        public static string BasePath { get { return Directory.GetCurrentDirectory() + "/Configs/Mods/DiscordLink/"; } }
    }
}
