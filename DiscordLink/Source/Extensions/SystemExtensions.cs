using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public static class SystemExtensions
    {
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
