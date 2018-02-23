using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Eco.Plugins.Networking;
using Eco.Shared.Services;

namespace Eco.Spoffy
{
    /**
     * Handles commands coming from Discord.
     */
    public class DiscordDiscordCommands
    {
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
            ServerInfo info = NetworkManager.GetServerInfo();

            string description = String.IsNullOrEmpty(info.Description)
                ? "No description available for this Eco server."
                : info.Description;
            string serverAddress = (info.Address ?? "0.0.0.0") + ":" + info.WebPort;
            string players = info.OnlinePlayers + "/" + info.TotalPlayers;
            var timeRemainingSpan = new TimeSpan(0, 0, (int) info.TimeLeft);
            string timeRemaining = String.Format("{0} Days, {1} hours, {2} minutes", 
                timeRemainingSpan.Days, timeRemainingSpan.Hours, timeRemainingSpan.Minutes);
            var timeSinceStartSpan = new TimeSpan(0, 0, (int) info.TimeSinceStart);
            string timeSinceStart = String.Format("{0} Days, {1} hours, {2} minutes", 
                timeSinceStartSpan.Days, timeSinceStartSpan.Hours, timeSinceStartSpan.Minutes);
            string leader = String.IsNullOrEmpty(info.Leader) ? "No leader" : info.Leader;

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Green)
                .WithTitle("" + info.Name + " Server Status")
                .WithDescription(description)
                .AddField("Players", players)
                .AddField("Address", serverAddress)
                .AddField("Time Left until Meteor", timeRemaining)
                .AddField("Time Since Game Start", timeSinceStart)
                .AddField("Current Leader", leader);
            await ctx.RespondAsync("Current status of the server:", false, builder.Build())
                .ContinueWith(async (message) =>
                {
                    if (message.Result == null)
                    {
                        await ctx.RespondAsync(
                            "Unable to send server status. If this persists, report it to the developers.");
                    }
                });
        }
    }
}