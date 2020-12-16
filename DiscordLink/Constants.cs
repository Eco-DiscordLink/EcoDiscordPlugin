using System.IO;

namespace Eco.Plugins.DiscordLink
{
    public static class DLConstants
    {
        public const string ECHO_COMMAND_TOKEN = "[ECHO]";

        public const int EMBED_CONTENT_CHARACTER_LIMIT = 5000;
        public const int EMBED_FIELD_CHARACTER_LIMIT = 900;

        public static string BasePath { get { return Directory.GetCurrentDirectory() + "\\Mods\\DiscordLink\\"; } }
    }
}
