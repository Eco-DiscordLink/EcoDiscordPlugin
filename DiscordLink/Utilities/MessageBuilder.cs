using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Plugins.Networking;

namespace Eco.Plugins.DiscordLink.Utilities
{
    static class MessageBuilder
    {
        public enum EcoStatusComponentFlag
        {
            Name = 1 << 0,
            Description = 1 << 1,
            Logo = 1 << 2,
            ServerAddress = 1 << 3,
            PlayerCount = 1 << 4,
            PlayerList = 1 << 5,
            TimeSinceStart = 1 << 6,
            TimeRemaining = 1 << 7,
            MeteorHasHit = 1 << 8,
            All = ~0
        }

        public static DiscordColor EmbedColor = DiscordColor.Green;

        public static string EmbedToText(string textContent, DiscordEmbed embedContent)
        {
            string message = "";
            if (!string.IsNullOrEmpty(textContent))
            {
                message += textContent + "\n\n";
            }
            if (embedContent != null)
            {
                if (!String.IsNullOrEmpty(embedContent.Title))
                {
                    message += embedContent.Title + "\n\n";
                }

                foreach (DiscordEmbedField field in embedContent.Fields)
                {
                    message += "**" + field.Name + "**\n" + field.Value + "\n\n";
                }

                if (!String.IsNullOrEmpty(embedContent.Footer?.Text))
                {
                    message += embedContent.Footer.Text;
                }
            }
            return message.Trim();
        }

        public static DiscordEmbed GetEcoStatus(EcoStatusComponentFlag flag, bool isLiveMessage)
        {
            var plugin = DiscordLink.Obj;
            if (plugin == null) { return null; }

            var config = DLConfig.Data;
            var serverInfo = NetworkManager.GetServerInfo();

            var builder = new DiscordEmbedBuilder();

            builder.WithColor(EmbedColor);

            if (flag.HasFlag(EcoStatusComponentFlag.Name))
            {
                builder.WithTitle($"**{FirstNonEmptyString(config.ServerName, serverInfo.Name)} " + (isLiveMessage ? "Live Server Status" : "Server Status") + "**\n" + DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString());
            }
            else
            {
                DateTime time = DateTime.Now;
                int utcOffset = TimeZoneInfo.Local.GetUtcOffset(time).Hours;
                builder.WithTitle("**" + (isLiveMessage ? "Live Server Status" : "Server Status") + "**\n" + "[" + DateTime.Now.ToString("yyyy-MM-dd : HH:mm", CultureInfo.InvariantCulture) + " UTC " + (utcOffset != 0 ? (utcOffset >= 0 ? "+" : "-") + utcOffset : "") + "]");
            }

            if (flag.HasFlag(EcoStatusComponentFlag.Description))
            {
                builder.WithDescription(FirstNonEmptyString(config.ServerDescription, serverInfo.Description, "No server description is available."));
            }

            if (flag.HasFlag(EcoStatusComponentFlag.Logo) && !String.IsNullOrWhiteSpace(config.ServerLogo))
            {
                try
                {
                    builder.WithThumbnail(config.ServerLogo);
                }
                catch (UriFormatException)
                { }
            }

            if (flag.HasFlag(EcoStatusComponentFlag.ServerAddress))
            {
                string fieldText = "-- No address configured --";
                if (!string.IsNullOrEmpty(config.ServerAddress))
                {
                    fieldText = config.ServerAddress;
                }
                else if (!string.IsNullOrEmpty(serverInfo.Address))
                {
                    fieldText = serverInfo.Address;
                }
                builder.AddField("Server Address", fieldText);
            }

            if (flag.HasFlag(EcoStatusComponentFlag.PlayerCount))
            {
                builder.AddField("Online Players", $"{UserManager.OnlineUsers.Where(user => user.Client.Connected).Count()}/{serverInfo.TotalPlayers}");
            }

            if (flag.HasFlag(EcoStatusComponentFlag.PlayerList))
            {
                IEnumerable<String> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).Select(user => user.Name);
                string playerList = onlineUsers.Count() > 0 ? String.Join("\n", onlineUsers) : "-- No players online --";
                builder.AddField("Online Players", playerList);
            }

            if (flag.HasFlag(EcoStatusComponentFlag.TimeSinceStart))
            {
                TimeSpan timeSinceStartSpan = new TimeSpan(0, 0, (int)serverInfo.TimeSinceStart);
                builder.AddField("Time Since Game Start", $"{timeSinceStartSpan.Days} Days, {timeSinceStartSpan.Hours} hours, {timeSinceStartSpan.Minutes} minutes");
            }

            if (flag.HasFlag(EcoStatusComponentFlag.TimeRemaining))
            {
                TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                bool meteorHasHit = timeRemainingSpan.Seconds < 0;
                timeRemainingSpan = meteorHasHit ? new TimeSpan(0, 0, 0) : timeRemainingSpan;
                builder.AddField("Time Left Until Meteor", $"{timeRemainingSpan.Days} Days, {timeRemainingSpan.Hours} hours, {timeRemainingSpan.Minutes} minutes");
            }

            if (flag.HasFlag(EcoStatusComponentFlag.MeteorHasHit))
            {
                TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                builder.AddField("Meteor Has Hit", timeRemainingSpan.Seconds < 0 ? "Yes" : "No");
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
