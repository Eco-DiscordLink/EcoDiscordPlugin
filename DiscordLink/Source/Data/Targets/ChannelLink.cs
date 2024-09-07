using DSharpPlus.Entities;
using Eco.Shared.Serialization;
using System;
using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class ChannelLink : DiscordTarget, ICloneable
    {
        [Browsable(false), JsonIgnore]
        public DiscordChannel Channel { get; private set; } = null;

        [Description("Discord channel by id.")]
        [TypeConverter(typeof(DiscordChannelPropertyConverter))]
        public ulong DiscordChannelId { get; set; } = 0;

        public override string ToString()
        {
            return IsValid() ? $"#{Channel.Name}" : $"<Unknown Channel Name> ({DiscordChannelId})";
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override bool IsValid() => DiscordChannelId != 0 && Channel != null;

        public virtual bool Initialize()
        {
            if (DiscordChannelId == 0)
                return false;

            DiscordChannel channel = DiscordLink.Obj.Client.Guild.GetChannel(DiscordChannelId);
            if (channel == null)
                return false;

            Channel = channel;
            return true;
        }

        public virtual bool MakeCorrections()
        {
            return false;
        }

        public bool IsChannel(DiscordChannel channel)
        {  
            return DiscordChannelId == channel.Id;
        }
    }
}
