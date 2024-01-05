using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Moose.Tools;
using Eco.Plugins.DiscordLink.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Eco.Plugins.DiscordLink
{
    public static class DLConstants
    {
        public static readonly Permissions[] REQUESTED_GUILD_PERMISSIONS = { Permissions.AccessChannels, Permissions.ManageRoles };
        public static readonly Permissions[] REQUESTED_CHANNEL_PERMISSIONS = { Permissions.AccessChannels, Permissions.SendMessages, Permissions.EmbedLinks, Permissions.AddReactions, Permissions.MentionEveryone, Permissions.ManageMessages, Permissions.ReadMessageHistory };
        public static readonly DiscordIntents[] REQUESTED_INTENTS = { DiscordIntents.AllUnprivileged, DiscordIntents.GuildMembers, DiscordIntents.MessageContents };

        public static readonly DiscordColor DISCORD_EMBED_COLOR = DiscordColor.Green;
        public const string DISCORD_COLOR = "7289DAFF";

        public const string INVISIBLE_EMBED_CHAR = "\u200e";

        public const string ECO_DISCORDLINK_ICON = "<ecoicon name=\"DiscordLinkLogo\">";

        public const string INVITE_COMMAND_TOKEN = "[LINK]";
        public const string ECHO_COMMAND_TOKEN = "[ECHO]";
        public const string DEFAULT_CHAT_CHANNEL = "General";

        public const int DISCORD_MESSAGE_CHARACTER_LIMIT = 2000;
        public const int DISCORD_EMBED_TITLE_CHARACTER_LIMIT = 256;
        public const int DISCORD_EMBED_FOOTER_CHARACTER_LIMIT = 2048;
        public const int DISCORD_EMBED_FOOTER_DESCRIPTION_LIMIT = 2048;
        public const int DISCORD_EMBED_FIELD_CHARACTER_LIMIT = 1024;
        public const int DISCORD_EMBED_FIELD_CHARACTER_PER_LINE_LIMIT = 25;
        public const int DISCORD_EMBED_FIELD_COUNT_LIMIT = 25;
        public const int DISCORD_EMBED_FIELD_ALIGNED_COUNT_LIMIT = 24; // Discord embed fields align when there are three fields per row
        public const int DISCORD_EMBED_SIZE_SMALL_FIELD_LIMIT = 3;
        public const int DISCORD_EMBED_SIZE_MEDIUM_FIELD_LIMIT = 12;
        public const int DISCORD_EMBED_SIZE_LARGE_FIELD_LIMIT = 24;
        public const int DISCORD_EMBED_AUTHOR_NAME_CHARACTER_LIMIT = 256;
        public const int DISCORD_EMBED_TOTAL_CHARACTER_LIMIT = 6000;
        public const int DISCORD_EMBED_FIELDS_PER_ROW_LIMIT = 3;
        public const int DISCORD_ACTIVITY_STRING_UPDATE_INTERVAL_MS = 900000; // 15 minutes

        public const int MAX_TOP_CURRENCY_HOLDER_DISPLAY_LIMIT = 15;
        public const int CURRENCY_REPORT_COMMAND_MAX_CURRENCIES_PER_TYPE_DEFAULT = 3;
        public const int CURRENCY_REPORT_COMMAND_MAX_TOP_HOLDERS_PER_CURRENCY_DEFAULT = 5;

        public const int POST_SERVER_CONNECTION_WAIT_MS = 3000;

        public const string ECO_PANEL_NOTIFICATION = "DLNotification";
        public const string ECO_PANEL_SIMPLE_LIST = "DLSimpleList";
        public const string ECO_PANEL_COMPLEX_LIST = "DL_ComplexList";
        public const string ECO_PANEL_DL_MESSAGE_MEDIUM = "DLMessageMedium";
        public const string ECO_PANEL_REPORT = "DLReport";
        public const string ECO_PANEL_DL_TRADES = "DLTrades";

        public static DiscordEmoji ACCEPT_EMOJI;
        public static DiscordEmoji DENY_EMOJI;

        public static DiscordEmoji DEBUG_LOG_EMOJI;
        public static DiscordEmoji WARNING_LOG_EMOJI;
        public static DiscordEmoji INFO_LOG_EMOJI;
        public static DiscordEmoji ERROR_LOG_EMOJI;

        public static readonly DiscordLinkRole ROLE_LINKED_ACCOUNT = new DiscordLinkRole("DiscordLinked", null, DiscordColor.Cyan, false, true, "Linked Discord account to Eco Server");

        public static string STORAGE_PATH_ABS { get { return Directory.GetCurrentDirectory() + "/Storage/Mods/DiscordLink/"; } }

        public static readonly Dictionary<string, string> DISCORD_EMOJI_SUBSTITUTION_MAP = new Dictionary<string, string>()
        {
            {$"❤{(char)65039}", "heart"},
            { $"{(char)55358}{(char)56631}{(char)8205}♂{(char)65039}", "man_shrugging"},
            { $"{(char)55358}{(char)56631}{(char)8205}♀{(char)65039}", "woman_shrugging"},
            { $"☝{(char)65039}", "point_up"},
            { "👍", "thumbsup"},
            { "👎", "thumbsdown"},
            { "🤔", "thinking" },
            { "✅", "white_check_mark" },
            { "❌", "red_cross_mark" },
            { "🙃", "upside_down"},
            { "🤘", "metal"},
            { "🤗", "hugging"},
            { "🥳", "partying_face"},
            { "😉", "wink"},
            { "🥱", "yawning_face"},
            { "😏", "smirk"},
            { "🥔", "potato"},
            { "😓", "sweat"},
            { "🥰", "smiling_face_with_3_hearts"},
            { "🤙", "call_me" },
            { "😮", "open_mouth"},
            { "😦", "frown"},
            { "👏", "clap" },
            { "👀", "eyes"},
            { "👋", "wave" },
            { "😆", "laughing" },
            { "🙂", "slight_smile" },
        };

        public static bool PostConnectionInit()
        {
            try
            {
                ACCEPT_EMOJI = DiscordEmoji.FromName(DiscordLink.Obj.Client.DSharpClient, ":white_check_mark:");
                DENY_EMOJI = DiscordEmoji.FromName(DiscordLink.Obj.Client.DSharpClient, ":x:");
                DEBUG_LOG_EMOJI = DiscordEmoji.FromName(DiscordLink.Obj.Client.DSharpClient, ":exclamation:");
                WARNING_LOG_EMOJI = DiscordEmoji.FromName(DiscordLink.Obj.Client.DSharpClient, ":small_orange_diamond:");
                INFO_LOG_EMOJI = DiscordEmoji.FromName(DiscordLink.Obj.Client.DSharpClient, ":white_small_square:");
                ERROR_LOG_EMOJI = DiscordEmoji.FromName(DiscordLink.Obj.Client.DSharpClient, ":small_red_triangle:");
                return true;
            }
            catch(Exception e)
            {
                Logger.Exception("Failed to initialize constants.", e);
                return false;
            }
        }
    }
}
