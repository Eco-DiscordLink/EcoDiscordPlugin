using DSharpPlus.Entities;
using System.IO;

namespace Eco.Plugins.DiscordLink
{
    public static class DLConstants
    {
        public static readonly DiscordColor DISCORD_EMBED_COLOR = DiscordColor.Green;
        public const string ECO_NAME_TAG_COLOR = "7289DAFF";

        public const string ECO_USER_STEAM_ID = "DiscordLinkSteam";
        public const string ECO_USER_SLG_ID = "DiscordLinkSlg";

        public const int ECO_PLOT_SIZE_M2 = 5 * 5;

        public const string INVITE_COMMAND_TOKEN = "[LINK]";
        public const string ECHO_COMMAND_TOKEN = "[ECHO]";
        public const string DEFAULT_CHAT_CHANNEL = "General";

        public const int DISCORD_MESSAGE_CHARACTER_LIMIT = 2000;
        public const int DISCORD_EMBED_TITLE_CHARACTER_LIMIT = 256;
        public const int DISCORD_EMBED_FOOTER_CHARACTER_LIMIT = 2048;
        public const int DISCORD_EMBED_FOOTER_DESCRIPTION_LIMIT = 2048;
        public const int DISCORD_EMBED_FIELD_CHARACTER_LIMIT = 1024;
        public const int DISCORD_EMBED_FIELD_COUNT_LIMIT = 25;
        public const int DISCORD_EMBED_FIELD_ALIGNED_COUNT_LIMIT = 24; // Discord embed fields align when there are three fields per row
        public const int DISCORD_EMBED_AUTHOR_NAME_CHARACTER_LIMIT = 256;
        public const int DISCORD_EMBED_TOTAL_CHARACTER_LIMIT = 6000;
        public const int DISCORD_EMBED_FIELDS_PER_ROW_LIMIT = 3;

        public const int MAX_TOP_CURRENCY_HOLDER_DISPLAY_LIMIT = 15;
        public const string CURRENCY_REPORT_COMMAND_MAX_CURRENCIES_PER_TYPE_DEFAULT = "3";
        public const string CURRENCY_REPORT_COMMAND_MAX_TOP_HOLDERS_PER_CURRENCY_DEFAULT = "5";

        public const int SECONDS_PER_MINUTE = 60;
        public const int SECONDS_PER_HOUR = SECONDS_PER_MINUTE * 60;
        public const int SECONDS_PER_DAY = SECONDS_PER_HOUR * 24;
        public const int SECONDS_PER_WEEK = SECONDS_PER_DAY * 7;

        public static string BasePath { get { return Directory.GetCurrentDirectory() + "/Configs/Mods/DiscordLink/"; } }
    }
}
