using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DSharpPlus.Entities;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Laws;
using Eco.Gameplay.Players;
using Eco.Plugins.Networking;
using Eco.Shared.Networking;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink.Utilities
{
    static class MessageBuilder
    {
        public enum ServerInfoComponentFlag
        {
            Name                = 1 << 0,
            Description         = 1 << 1,
            Logo                = 1 << 2,
            ServerAddress       = 1 << 3,
            PlayerCount         = 1 << 4,
            PlayerList          = 1 << 5,
            TimeSinceStart      = 1 << 6,
            TimeRemaining       = 1 << 7,
            MeteorHasHit        = 1 << 8,
            ActiveElectionCount = 1 << 9,
            ActiveElectionList  = 1 << 10,
            LawCount            = 1 << 11,
            LawList             = 1 << 12,
            All                 = ~0
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
                if (!string.IsNullOrEmpty(embedContent.Title))
                {
                    message += embedContent.Title + "\n\n";
                }

                foreach (DiscordEmbedField field in embedContent.Fields)
                {
                    message += "**" + field.Name + "**\n" + field.Value + "\n\n";
                }

                if (!string.IsNullOrEmpty(embedContent.Footer?.Text))
                {
                    message += embedContent.Footer.Text;
                }
            }
            return message.Trim();
        }

        public static DiscordEmbed GetServerInfo(ServerInfoComponentFlag flag)
        {
            var plugin = DiscordLink.Obj;
            if (plugin == null) return null;

            var config = DLConfig.Data;
            var serverInfo = NetworkManager.GetServerInfo();

            var builder = new DiscordEmbedBuilder();
            builder.WithColor(EmbedColor);
            builder.WithFooter(GetStandardEmbedFooter());

            if (flag.HasFlag(ServerInfoComponentFlag.Name))
            {
                builder.WithTitle($"**{FirstNonEmptyString(config.ServerName, MessageUtil.StripTags(serverInfo.Description), "[Server Title Missing]")} " + "Server Status" + "**\n" + DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString());
            }
            else
            {
                DateTime time = DateTime.Now;
                int utcOffset = TimeZoneInfo.Local.GetUtcOffset(time).Hours;
                builder.WithTitle("**" + "Server Status" + "**\n" + "[" + DateTime.Now.ToString("yyyy-MM-dd : HH:mm", CultureInfo.InvariantCulture) + " UTC " + (utcOffset != 0 ? (utcOffset >= 0 ? "+" : "-") + utcOffset : "") + "]");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.Description))
            {
                builder.WithDescription(FirstNonEmptyString(config.ServerDescription, MessageUtil.StripTags(serverInfo.Description), "No server description is available."));
            }

            if (flag.HasFlag(ServerInfoComponentFlag.Logo) && !string.IsNullOrWhiteSpace(config.ServerLogo))
            {
                try
                {
                    builder.WithThumbnail(config.ServerLogo);
                }
                catch (UriFormatException e)
                {
                    Logger.Debug("Failed to include thumbnail in Server Info embed. Error: " + e);
                }
            }

            if (flag.HasFlag(ServerInfoComponentFlag.ServerAddress))
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

            if (flag.HasFlag(ServerInfoComponentFlag.PlayerCount))
            {
                builder.AddField("Online Players Count", $"{UserManager.OnlineUsers.Where(user => user.Client.Connected).Count()}/{serverInfo.TotalPlayers}");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.PlayerList))
            {
                IEnumerable<string> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).Select(user => user.Name);
                string playerList = onlineUsers.Count() > 0 ? string.Join("\n", onlineUsers) : "-- No players online --";
                builder.AddField("Online Players", GetPlayerList());
            }

            if (flag.HasFlag(ServerInfoComponentFlag.TimeSinceStart))
            {
                TimeSpan timeSinceStartSpan = new TimeSpan(0, 0, (int)serverInfo.TimeSinceStart);
                builder.AddField("Time Since Game Start", $"{timeSinceStartSpan.Days} Days, {timeSinceStartSpan.Hours} hours, {timeSinceStartSpan.Minutes} minutes");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.TimeRemaining))
            {
                TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                bool meteorHasHit = timeRemainingSpan.Seconds < 0;
                timeRemainingSpan = meteorHasHit ? new TimeSpan(0, 0, 0) : timeRemainingSpan;
                builder.AddField("Time Left Until Meteor", $"{timeRemainingSpan.Days} Days, {timeRemainingSpan.Hours} hours, {timeRemainingSpan.Minutes} minutes");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.MeteorHasHit))
            {
                TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                builder.AddField("Meteor Has Hit", timeRemainingSpan.Seconds < 0 ? "Yes" : "No");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.ActiveElectionCount))
            {
                builder.AddField("Active Elections Count", $"{EcoUtil.ActiveElections.Count()}");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.ActiveElectionList))
            {
                string electionList = string.Empty;
                foreach (Election election in EcoUtil.ActiveElections)
                {
                    electionList += $"{election.Name} **[{election.TotalVotes} Votes]**\n";
                }

                if (string.IsNullOrEmpty(electionList))
                    electionList = "-- No active elections --";

                builder.AddField("Active Elections", electionList);
            }

            if (flag.HasFlag(ServerInfoComponentFlag.LawCount))
            {
                builder.AddField("Law Count", $"{EcoUtil.Laws.Count()}");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.LawList))
            {
                string lawList = string.Empty;
                foreach (Law law in EcoUtil.Laws)
                {
                    lawList += $"{law.Name}\n";
                }

                if (string.IsNullOrEmpty(lawList))
                    lawList = "-- No active laws --";

                builder.AddField("Laws", lawList);
            }

            return builder.Build();
        }

        public static DiscordEmbed GetVerificationDM(User ecoUser)
        {
            DLConfigData config = DLConfig.Data;
            ServerInfo serverInfo = NetworkManager.GetServerInfo();
            string serverName = !string.IsNullOrWhiteSpace(config.ServerName) ? DLConfig.Data.ServerName : serverInfo.Description;

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.WithColor(EmbedColor);
            builder.WithTitle("Account Linking Verification");
            builder.AddField("Initiator", ecoUser.Name);
            builder.AddField("Description", $"Your Eco account has been linked to your Discord account on the server \"{serverName}\".");
            builder.AddField("Action Required", $"If you initiated this action, use the command `{config.DiscordCommandPrefix}verifylink` to verify that these accounts should be linked.");
            builder.WithFooter("If you did not initiate this action, notify a server admin.\nThe account link cannot be used until verified.");
            return builder.Build();
        }

        public static string GetPlayerCount()
        {
            IEnumerable<string> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).Select(user => user.Name);
            int numberTotal = NetworkManager.GetServerInfo().TotalPlayers;
            int numberOnline = onlineUsers.Count();
            return $"{numberOnline}/{numberTotal}";
        }

        public static string GetPlayerList(bool useOnlineTime = false)
        {
            string playerList = string.Empty;
            IEnumerable<User> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected);
            foreach(User player in onlineUsers)
            {
                if(useOnlineTime)
                    playerList += $"{player.Name} [{GetTimespan(Simulation.Time.WorldTime.Seconds - player.LoginTime, TimespanStringComponent.Hour | TimespanStringComponent.Minute)}]\n";
                else
                    playerList += $"{player.Name}\n";
            }

            if (string.IsNullOrEmpty(playerList))
                playerList = "-- No players online --";

            return playerList;
        }

        public static string GetAboutMessage()
        {
            return "DiscordLink is a plugin mod that runs on this server." +
                "\nIt connects the game server to a Discord bot in order to perform seamless communication between Eco and Discord." +
                "\nThis enables you to chat with players who are currently not online in Eco, but are available on Discord." +
                "\nDiscordLink can also be used to display information about the Eco server in Discord, such as who is online and what items are available on the market." +
                "\n\nFor more information, visit \"www.github.com/Eco-DiscordLink/EcoDiscordPlugin\".";
        }

        public enum TimespanStringComponent
        {
            Day     = 1 << 0,
            Hour    = 1 << 1,
            Minute  = 1 << 2,
            Second  = 1 << 3,
        }

        public static string GetTimeStamp()
        {
            double seconds = Simulation.Time.WorldTime.Seconds;
            return $"{((int)TimeUtil.SecondsToHours(seconds) % 24).ToString("00") }" +
                $":{((int)(TimeUtil.SecondsToMinutes(seconds) % 60)).ToString("00")}" +
                $":{((int)seconds % 60).ToString("00")}";
        }

        public static string GetTimespan(double seconds, TimespanStringComponent flag = TimespanStringComponent.Day | TimespanStringComponent.Hour | TimespanStringComponent.Minute | TimespanStringComponent.Second)
        {
            StringBuilder builder = new StringBuilder();
            if ((flag & TimespanStringComponent.Day) != 0)
            {
                builder.Append(((int)TimeUtil.SecondsToDays(seconds)).ToString("00"));
            }

            if ((flag & TimespanStringComponent.Hour) != 0)
            {
                if (builder.Length != 0)
                    builder.Append(":");
                builder.Append(((int)TimeUtil.SecondsToHours(seconds) % 24).ToString("00"));
            }

            if ((flag & TimespanStringComponent.Minute) != 0)
            {
                if (builder.Length != 0)
                    builder.Append(":");
                builder.Append(((int)(TimeUtil.SecondsToMinutes(seconds) % 60)).ToString("00"));
            }

            if ((flag & TimespanStringComponent.Second) != 0)
            {
                if (builder.Length != 0)
                    builder.Append(":");
                builder.Append(((int)seconds % 60).ToString("00"));
            }
            return builder.ToString();
        }

        public static string GetStandardEmbedFooter()
        {
            string serverName = FirstNonEmptyString(DLConfig.Data.ServerName, MessageUtil.StripTags(NetworkManager.GetServerInfo().Description), "[Server Title Missing]");
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            return $"By DiscordLink @ {serverName} [{timestamp}]";
        }

        private static string FirstNonEmptyString(params string[] strings)
        {
            return strings.FirstOrDefault(str => !string.IsNullOrEmpty(str)) ?? "";
        }
    }
}
