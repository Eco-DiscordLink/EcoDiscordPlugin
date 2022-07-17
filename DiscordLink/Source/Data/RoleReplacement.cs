using System;
using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class DemographicRoleReplacement : ICloneable
    {
        public DemographicRoleReplacement() { }

        public DemographicRoleReplacement(string demographicName, string roleName)
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
}
