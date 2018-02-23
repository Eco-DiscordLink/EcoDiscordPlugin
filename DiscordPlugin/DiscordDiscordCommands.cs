using System;
using System.Linq;
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
        private string FirstNonEmptyString(params string[] strings)
        {
            try
            {
                return strings.First(str => !String.IsNullOrEmpty(str));
            }
            catch (Exception e)
            {
                return "";
            }
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
            var plugin = DiscordPlugin.Obj;
            if (plugin == null)
            {
                await ctx.RespondAsync(
                    "The plugin was unable to be found on the server. Please report this to the plugin author.");
                return;
            }
            
            var pluginConfig = plugin.PluginConfig.GetConfig() as DiscordConfig;        
            var serverInfo = NetworkManager.GetServerInfo();

            string name = FirstNonEmptyString(pluginConfig.ServerName, serverInfo.Name);
            string description = FirstNonEmptyString(pluginConfig.ServerDescription, serverInfo.Description,
                "No server description is available.");
     
            string serverAddress = (serverInfo.Address ?? "0.0.0.0") + ":" + serverInfo.WebPort;
            
            string players = serverInfo.OnlinePlayers + "/" + serverInfo.TotalPlayers;
            
            var timeRemainingSpan = new TimeSpan(0, 0, (int) serverInfo.TimeLeft);
            string timeRemaining = String.Format("{0} Days, {1} hours, {2} minutes", 
                timeRemainingSpan.Days, timeRemainingSpan.Hours, timeRemainingSpan.Minutes);
            
            var timeSinceStartSpan = new TimeSpan(0, 0, (int) serverInfo.TimeSinceStart);
            string timeSinceStart = String.Format("{0} Days, {1} hours, {2} minutes", 
                timeSinceStartSpan.Days, timeSinceStartSpan.Hours, timeSinceStartSpan.Minutes);
            
            string leader = String.IsNullOrEmpty(serverInfo.Leader) ? "No leader" : serverInfo.Leader;

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Green)
                .WithTitle("**" + name + " Server Status**")
                .WithDescription(description)
                .AddField("Online Players", players)
                .AddField("Address", serverAddress)
                .AddField("Time Left until Meteor", timeRemaining)
                .AddField("Time Since Game Start", timeSinceStart)
                .AddField("Current Leader", leader);

            builder = String.IsNullOrEmpty(pluginConfig.ServerLogo)
                ? builder
                : builder.WithThumbnailUrl(pluginConfig.ServerLogo);

            await ctx.RespondAsync("Current status of the server:", false, builder.Build());
        }
    }
}