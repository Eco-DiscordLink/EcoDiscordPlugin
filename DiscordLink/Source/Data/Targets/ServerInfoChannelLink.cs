using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class ServerInfoChannel : ChannelLink
    {
        [Description("Display the server name.")]
        public bool UseName { get; set; } = true;

        [Description("Display the server description.")]
        public bool UseDescription { get; set; } = false;

        [Description("Display the server logo.")]
        public bool UseLogo { get; set; } = true;

        [Description("Display the connection information for the game server.")]
        public bool UseConnectionInfo { get; set; } = true;

        [Description("Display the web server address.")]
        public bool UseWebServerAddress { get; set; } = true;

        [Description("Display the number of online players.")]
        public bool UsePlayerCount { get; set; } = false;

        [Description("Display a list of online players.")]
        public bool UsePlayerList { get; set; } = true;

        [Description("Display how long the players in the player list has been logged in for.")]
        public bool UsePlayerListLoggedInTime { get; set; } = false;

        [Description("Display how long the players in the player list has left before they get exhausted.")]
        public bool UsePlayerListExhaustionTime { get; set; } = false;

        [Description("Display the current ingame time.")]
        public bool UseIngameTime { get; set; } = true;

        [Description("Display the time remaining until meteor impact.")]
        public bool UseTimeRemaining { get; set; } = true;

        [Description("Display the current server time.")]
        public bool UseServerTime { get; set; } = true;

        [Description("Display the server time at which exhaustion resets.")]
        public bool UseExhaustionResetServerTime { get; set; } = false;

        [Description("Display the time remaining before exhaustion resets.")]
        public bool UseExhaustionResetTimeLeft { get; set; } = false;

        [Description("Display the amount of exhausted players.")]
        public bool UseExhaustedPlayerCount { get; set; } = false;

        [Description("Display the number of settlements elections.")]
        public bool UseSettlementCount { get; set; } = false;

        [Description("Display a list of all active settlements.")]
        public bool UseSettlementList { get; set; } = true;

        [Description("Display the number of active elections.")]
        public bool UseElectionCount { get; set; } = false;

        [Description("Display a list of all active elections.")]
        public bool UseElectionList { get; set; } = true;

        [Description("Display the number of active laws.")]
        public bool UseLawCount { get; set; } = false;

        [Description("Display a list of all active laws.")]
        public bool UseLawList { get; set; } = true;
    }
}
