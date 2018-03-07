using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Plugins.Networking;
using Eco.Shared.Services;

namespace Eco.Plugins.DiscordLink
{
    /**
     * Handles commands coming from Discord.
     */
    public class DiscordDiscordCommands
    {
        public static DiscordColor EmbedColor = DiscordColor.Green;
        
        private string FirstNonEmptyString(params string[] strings)
        {
            return strings.FirstOrDefault(str => !String.IsNullOrEmpty(str)) ?? "";
        }

        [Command("ping")]
        [Description("Checks if the bot is online.")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync("Pong " + ctx.User.Mention);
        }

        [Command("ecostatus")]
        [Description("Retrieves the current status of the Eco Server")]
        public async Task EcoStatus(CommandContext ctx)
        {
            var plugin = DiscordLink.Obj;
            if (plugin == null)
            {
                await ctx.RespondAsync(
                    "The plugin was unable to be found on the server. Please report this to the plugin author.");
                return;
            }

            var pluginConfig = plugin.DiscordPluginConfig;       
            var serverInfo = NetworkManager.GetServerInfo();

            var name = FirstNonEmptyString(pluginConfig.ServerName, serverInfo.Name);
            var description = FirstNonEmptyString(pluginConfig.ServerDescription, serverInfo.Description,
                "No server description is available.");

            var addr = IPUtil.GetInterNetworkIP().FirstOrDefault();
            var serverAddress = String.IsNullOrEmpty(pluginConfig.ServerIP)
                ? addr == null ? "No Configured Address"
                : addr + ":" + serverInfo.WebPort
                : pluginConfig.ServerIP;
            
            var players = $"{serverInfo.OnlinePlayers}/{serverInfo.TotalPlayers}";
            
            var timeRemainingSpan = new TimeSpan(0, 0, (int) serverInfo.TimeLeft);
            var timeRemaining = $"{timeRemainingSpan.Days} Days, {timeRemainingSpan.Hours} hours, {timeRemainingSpan.Minutes} minutes";
            
            var timeSinceStartSpan = new TimeSpan(0, 0, (int) serverInfo.TimeSinceStart);
            var timeSinceStart = $"{timeSinceStartSpan.Days} Days, {timeSinceStartSpan.Hours} hours, {timeSinceStartSpan.Minutes} minutes";
            
            var leader = String.IsNullOrEmpty(serverInfo.Leader) ? "No leader" : serverInfo.Leader;

            var builder = new DiscordEmbedBuilder()
                .WithColor(EmbedColor)
                .WithTitle($"**{name} Server Status**")
                .WithDescription(description)
                .AddField($"Online Players {players}")
                .AddField($"Address {serverAddress}")
                .AddField($"Time Left until Meteor {timeRemaining}")
                .AddField($"Time Since Game Start {timeSinceStart}")
                .AddField($"Current Leader {leader}");

            builder = String.IsNullOrWhiteSpace(pluginConfig.ServerLogo)
                ? builder
                : builder.WithThumbnailUrl(pluginConfig.ServerLogo);

            await ctx.RespondAsync("Current status of the server:", false, builder.Build());
        }

        
        [Command("players")]
        [Description("Lists the players currently online on the server")]
        public async Task PlayerList(CommandContext ctx)
        {
            var plugin = DiscordLink.Obj;
            if (plugin == null)
            {
                await ctx.RespondAsync(
                    "The plugin was unable to be found on the server. Please report this to the plugin author.");
                return;
            }

            var onlineUsers = UserManager.OnlineUsers.Select(user => user.Name);
            var numberOnline = onlineUsers.Count();
            var message = $"{numberOnline} Online Players:\n";
            var playerList = String.Join("\n", onlineUsers);
            var embed = new DiscordEmbedBuilder()
                .WithColor(EmbedColor)
                .WithTitle(message)
                .WithDescription(playerList);

            await ctx.RespondAsync("Displaying Online Players", false, embed);
        }
    }
}
