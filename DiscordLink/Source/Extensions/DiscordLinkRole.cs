using DSharpPlus;
using DSharpPlus.Entities;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public class DiscordLinkRole
    {
        public DiscordLinkRole(string name, Permissions? permissions, DiscordColor? color, bool? hoist, bool? mentionable, string addReason)
        {
            Name = name;
            Permissions = permissions;
            Color = color;
            Hoist = hoist;
            Mentionable = mentionable;
            AddReason = addReason;
        }

        public string Name { get; private set; }
        public Permissions? Permissions { get; private set; }
        public DiscordColor? Color { get; private set; }
        public bool? Hoist { get; private set; }
        public bool? Mentionable { get; private set; }
        public string AddReason { get; private set; }
    }
}
