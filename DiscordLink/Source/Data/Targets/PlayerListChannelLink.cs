using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class PlayerListChannelLink : ChannelLink
    {
        [Description("Display the number of online players in the message.")]
        public bool UsePlayerCount { get; set; } = true;

        [Description("Display how long the player has been logged in for.")]
        public bool UseLoggedInTime { get; set; } = false;
    }
}
