using System;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public static class PlatformExtensions
    {
        public static bool TryParseSnowflakeID(this string StringID, out ulong ID) => ulong.TryParse(StringID, out ID) && ID > 0xFFFFFFFFFFFFFUL;

        public static Int64 ToUnixTime(this DateTime time) => (Int64)time.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds;

        /// <summary>
        ///d => Month/Day/Year
        ///D => Month Day, Year
        ///f => Month Day, Year Time
        ///F => Weekday, Month Day, Year Time
        ///t => Time
        ///T => Hours:Minutes:Seconds
        ///R => Time since
        /// </summary>
        public static string ToDiscordTimeStamp(this DateTime time, char discordTimeFormat = 'f') => $"<t:{time.ToUnixTime()}:{discordTimeFormat}>";
    }
}
