using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Eco.Plugins.DiscordLink
{
    /**
     * This Converter will provide a Dropdown List, with all to the Bot visible Discord Channels.
    */
    public class DiscordChannelPropertyConverter : TypeConverter
    {

        private string FormatChannelString(DiscordChannel discordChannel)
        {
            return $"#{discordChannel.Name} ({discordChannel.Id})";
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var foundChannelIds = DiscordLink.Obj?.Client?.Guild?.Channels?.Values?
                .Where(channel => channel.Type == DSharpPlus.ChannelType.Text)
                .OrderBy(channel => channel.Position)
                .Select(c => c.Id)
                .ToList();

            return new StandardValuesCollection(foundChannelIds ?? new List<ulong>());
        }
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
               return true;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
                return true;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value.GetType() == typeof(string))
            {
                // Match the Snowflake inside the Displayed String. Example: #ChannelName (111111111111111111)
                var matchIdPattern = @".*\((\d*?)\)";
                var match = Regex.Match(value.ToString(), matchIdPattern);

                if (match.Groups.Count > 0)
                {
                    // Always get last match, so Channels containing a String with '(NUMBER)' don't match first. 
                    return ulong.Parse(match.Groups[match.Groups.Count - 1].Value);
                }
                // Try to parse custom User Input
                if (ulong.TryParse(value.ToString(), out ulong result))
                {
                    return result;
                }
            }
            return base.ConvertFrom(context, culture, value);
         }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value != null && destinationType == typeof(string) && value.GetType() == typeof(ulong))
            {
                if (DiscordLink.Obj?.Client?.Guild?.Channels?.Values == null) 
                    return "No Connection";
                
                if ((ulong)value == 0) 
                    return "Select a Channel";

                var channels = DiscordLink.Obj.Client.Guild.Channels.Values;
                return channels.Where(channel => channel.Id == (ulong)value).Select(FormatChannelString).FirstOrDefault() ?? $"<Unknown Channel> ({value})";
            }
            return base.ConvertFrom(context, culture, value);
        }

    }
}
