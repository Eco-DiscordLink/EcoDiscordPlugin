using Eco.Plugins.DiscordLink.Utilities;
using System;
using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class ChannelLink : ICloneable
    {
        [Description("Discord Guild (Server) by name or ID.")]
        public string DiscordGuild { get; set; }

        [Description("Discord Channel by name or ID.")]
        public string DiscordChannel { get; set; }

        public override string ToString()
        {
            return DiscordGuild + " - " + DiscordChannel;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        virtual public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(DiscordGuild) && !string.IsNullOrWhiteSpace(DiscordChannel);
        }

        virtual public bool Verify()
        {
            if (string.IsNullOrWhiteSpace(DiscordGuild) || string.IsNullOrWhiteSpace(DiscordChannel)) return false;

            var guild = DiscordLink.Obj.GuildByNameOrId(DiscordGuild);
            if (guild == null)
            {
                return false; // The channel will always fail if the guild fails
            }
            var channel = guild.ChannelByNameOrId(DiscordChannel);
            if (channel == null)
            {
                return false;
            }

            return true;
        }

        virtual public bool MakeCorrections()
        {
            if (string.IsNullOrWhiteSpace(DiscordChannel)) return false;

            bool correctionMade = false;
            string original = DiscordChannel;
            if (DiscordChannel != DiscordChannel.ToLower()) // Discord channels are always lowercase
            {
                DiscordChannel = DiscordChannel.ToLower();
            }

            if (DiscordChannel.Contains(" "))
            {
                DiscordChannel = DiscordChannel.Replace(' ', '-'); // Discord channels always replace spaces with dashes
            }

            if (DiscordChannel != original)
            {
                correctionMade = true;
                Logger.Info("Corrected Discord channel name with Guild name/ID \"" + DiscordGuild + "\" from \"" + original + "\" to \"" + DiscordChannel + "\"");
            }

            return correctionMade;
        }
    }

    public class EcoChannelLink : ChannelLink
    {
        [Description("Eco Channel (with # omitted) to use.")]
        public string EcoChannel { get; set; }
        public override string ToString()
        {
            return DiscordGuild + " - " + DiscordChannel + " <--> " + EcoChannel;
        }

        public override bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(DiscordGuild) && !string.IsNullOrWhiteSpace(DiscordChannel) && !string.IsNullOrWhiteSpace(EcoChannel);
        }

        public override bool MakeCorrections()
        {
            bool correctionMade = base.MakeCorrections();
            string original = EcoChannel;
            EcoChannel = EcoChannel.Trim('#');
            if (EcoChannel != original)
            {
                correctionMade = true;
                Logger.Info("Corrected Eco channel name with Guild name/ID \"" + DiscordGuild + "\" and Discord Channel name/ID \"" + DiscordChannel + "\" from \"" + original + "\" to \"" + EcoChannel + "\"");
            }
            return correctionMade;
        }
    }
}
