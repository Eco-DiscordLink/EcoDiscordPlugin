using System;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public static class PlatformExtensions
    {
        public static bool TryParseSnowflakeId(this string idString, out ulong Id) => ulong.TryParse(idString, out Id) && Id > 0xFFFFFFFFFFFFFUL;

        public static Int64 ToUnixTime(this DateTime time) => (Int64)time.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds;

        /// <summary>
        ///d => Month/Day/Year
        ///D => Month Day, Year
        ///f => Month Day, Year Time
        ///F => Weekday, Month Day, Year Time
        ///t => Time
        ///T => Hours:Minutes:Seconds
        ///R => Time until/since
        /// </summary>
        public static string ToDiscordTimeStamp(this DateTime time, char discordTimeFormat = 'f') => $"<t:{time.ToUnixTime()}:{discordTimeFormat}>";
    }
}
