using Eco.Moose.Tools.Logger;
using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class ServerLogFeedChannelLink : ChannelLink
    {
        [Description("Determines what log message types will be printed to the channel log. All message types below the selected one will be printed as well.")]
        public Logger.LogLevel LogLevel { get; set; } = Logger.LogLevel.Information;
    }
}
