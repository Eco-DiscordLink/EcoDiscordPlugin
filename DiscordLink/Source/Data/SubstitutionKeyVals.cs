using System;
using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class DemographicRoleSubstitution : ICloneable
    {
        public DemographicRoleSubstitution() { }

        public DemographicRoleSubstitution(string demographicName, string roleName)
        {
            this.DemographicName = demographicName;
            this.RoleName = roleName;
        }

        [Description("The name of the demographic.")]
        public string DemographicName { get; set; } = string.Empty;
        [Description("The name of the Discord role to use for the Demographic.")]
        public string RoleName { get; set; } = string.Empty;

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(DemographicName) && !string.IsNullOrWhiteSpace(RoleName);
        }
    }

    public class EmoteIconSubstitution : ICloneable
    {
        public EmoteIconSubstitution() { }

        public EmoteIconSubstitution(string discordEmoteKey, string ecoIconKey)
        {
            this.DiscordEmoteKey = discordEmoteKey;
            this.EcoIconKey = ecoIconKey;
        }

        [Description("The lower case name of the Discord custom emote without surrounding colons.")]
        public string DiscordEmoteKey { get; set; } = string.Empty;
        [Description("The name of the substitute Eco icon to show ingame.")]
        public string EcoIconKey { get; set; } = string.Empty;

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(DiscordEmoteKey) && !string.IsNullOrWhiteSpace(EcoIconKey);
        }
    }
}
