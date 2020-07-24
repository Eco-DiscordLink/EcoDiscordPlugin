using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Plugins.Networking;

namespace Eco.Plugins.DiscordLink
{
    static class MessageBuilder
    {
        public enum EcoStatusComponentFlag
        {
            Name            = 1 << 0,
            Description     = 1 << 1,
            Logo            = 1 << 2,
            IPAddress       = 1 << 3,
            PlayerCount     = 1 << 4,
            PlayerList      = 1 << 5,
            TimeSinceStart  = 1 << 6,
            TimeRemaining   = 1 << 7,
            MeteorHasHit    = 1 << 8,
            WorldLeader     = 1 << 9,
            All             = ~0
        }

        public static DiscordColor EmbedColor = DiscordColor.Green;

        public static DiscordEmbed GetEcoStatus(EcoStatusComponentFlag flag)
        {
            var plugin = DiscordLink.Obj;
            if (plugin == null) { return null; }

            var pluginConfig = plugin.DiscordPluginConfig;
            var serverInfo = NetworkManager.GetServerInfo();

            var builder = new DiscordEmbedBuilder();

            builder.WithColor(EmbedColor);

            if (flag.HasFlag(EcoStatusComponentFlag.Name))
            {
                builder.WithTitle($"**{FirstNonEmptyString(pluginConfig.ServerName, serverInfo.Name)} Server Status**\n" + DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString());
            }
            else
            {
                builder.WithTitle("**Server Status**\n" + DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToShortTimeString());
            }

            if (flag.HasFlag(EcoStatusComponentFlag.Description))
            {
                builder.WithDescription(FirstNonEmptyString(pluginConfig.ServerDescription, serverInfo.Description, "No server description is available."));
            }

            if (flag.HasFlag(EcoStatusComponentFlag.Logo) && !String.IsNullOrWhiteSpace(pluginConfig.ServerLogo))
            {
                try
                {
                    builder.WithThumbnail(pluginConfig.ServerLogo);
                }
                catch (UriFormatException)
                { }
            }

            if (flag.HasFlag(EcoStatusComponentFlag.IPAddress))
            {
                string addr = serverInfo.Address;
                string serverAddress = String.IsNullOrEmpty(pluginConfig.ServerIP)
                    ? addr == null ? "No Configured Address"
                    : addr + ":" + serverInfo.WebPort
                    : pluginConfig.ServerIP;
                builder.AddField("Server Address", serverAddress);
            }

            if(flag.HasFlag(EcoStatusComponentFlag.PlayerCount))
            {
                builder.AddField("Online Players", $"{serverInfo.OnlinePlayers}/{serverInfo.TotalPlayers}");
            }

            if(flag.HasFlag(EcoStatusComponentFlag.PlayerList))
            {
                IEnumerable<String> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).Select(user => user.Name);
                string playerList = onlineUsers.Count() > 0 ? String.Join("\n", onlineUsers) : "-- No players online --";
                builder.AddField("Online Players", playerList);
            }

            if(flag.HasFlag(EcoStatusComponentFlag.TimeSinceStart))
            {
                TimeSpan timeSinceStartSpan = new TimeSpan(0, 0, (int)serverInfo.TimeSinceStart);
                builder.AddField("Time Since Game Start", $"{timeSinceStartSpan.Days} Days, {timeSinceStartSpan.Hours} hours, {timeSinceStartSpan.Minutes} minutes");
            }

            if(flag.HasFlag(EcoStatusComponentFlag.TimeRemaining))
            {
                TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                bool meteorHasHit = timeRemainingSpan.Seconds < 0;
                timeRemainingSpan = meteorHasHit ? new TimeSpan(0, 0, 0) : timeRemainingSpan;
                builder.AddField("Time Left Until Meteor", $"{timeRemainingSpan.Days} Days, {timeRemainingSpan.Hours} hours, {timeRemainingSpan.Minutes} minutes");
            }

            if(flag.HasFlag(EcoStatusComponentFlag.MeteorHasHit))
            {
                TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                builder.AddField("Meteor Has Hit", timeRemainingSpan.Seconds < 0 ? "Yes" : "No");
            }

            if(flag.HasFlag(EcoStatusComponentFlag.WorldLeader))
            {
                string leader = String.IsNullOrEmpty(serverInfo.Leader) ? "No leader" : serverInfo.Leader;
                builder.AddField("Current Leader", leader);
            }

            return builder.Build();
        }

        public static DiscordEmbedBuilder GetPlayerList()
        {
            IEnumerable<String> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).Select(user => user.Name);
            int numberOnline = onlineUsers.Count();
            string messageText = $"{numberOnline} Online Players:\n";
            string playerList = String.Join("\n", onlineUsers);
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(EmbedColor)
                .WithTitle(messageText)
                .WithDescription(playerList);
            return embed;
        }

        private static string FirstNonEmptyString(params string[] strings)
        {
            return strings.FirstOrDefault(str => !String.IsNullOrEmpty(str)) ?? "";
        }
    }
}
