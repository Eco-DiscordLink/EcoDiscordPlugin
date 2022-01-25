using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Core.Utils;
using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Laws;
using Eco.Gameplay.Civics.Titles;
using Eco.Gameplay.Components;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems.Balance;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.Networking;
using Eco.Simulation.Types;
using Eco.Shared.Networking;
using Eco.Shared.Utils;
using Eco.Shared;
using Eco.Shared.Items;

using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;
using StoreOfferGroup = System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.StoreComponent, Eco.Gameplay.Components.TradeOffer>>;

namespace Eco.Plugins.DiscordLink.Utilities
{
    static class MessageBuilder
    {
        #pragma warning disable format
        public enum ServerInfoComponentFlag
        {
            Name                        = 1 << 0,
            Description                 = 1 << 1,
            Logo                        = 1 << 2,
            ConnectionInfo              = 1 << 3,
            WebServerAddress            = 1 << 4,
            PlayerCount                 = 1 << 5,
            PlayerList                  = 1 << 6,
            PlayerListLoginTime         = 1 << 7,
            PlayerListExhaustionTime    = 1 << 8,
            ExhaustionResetTime         = 1 << 9,
            ExhaustionResetTimeLeft     = 1 << 10,
            ExhaustedPlayerCount        = 1 << 11,
            IngameTime                  = 1 << 12,
            MeteorTimeRemaining         = 1 << 13,
            ServerTime                  = 1 << 14,
            ActiveElectionCount         = 1 << 15,
            ActiveElectionList          = 1 << 16,
            LawCount                    = 1 << 17,
            LawList                     = 1 << 18,
            All                         = ~0
        }

        public enum PlayerReportComponentFlag
        {
            OnlineStatus    = 1 << 0,
            PlayTime        = 1 << 1,
            Exhaustion      = 1 << 2,
            Permissions     = 1 << 3,
            AccessLists     = 1 << 4,
            DiscordInfo     = 1 << 5,
            Reputation      = 1 << 6,
            Experience      = 1 << 7,
            Skills          = 1 << 8,
            Demographics    = 1 << 9,
            Titles          = 1 << 10,
            Properties      = 1 << 11,
            All             = ~0
        }
        #pragma warning disable format

        private class StoreOffer
        {
            public StoreOffer(string title, string description, Currency currency, bool buying)
            {
                this.Title = title;
                this.Description = description;
                this.Currency = currency;
                this.Buying = buying;
            }

            public string Title { get; private set; }
            public string Description { get; private set; }
            public Currency Currency { get; private set; }
            public bool Buying { get; private set; }
        }


        public static class Shared
        {
            public static object Items { get; internal set; }

            public static string GetAboutMessage()
            {
                return $"This server is running the DiscordLink plugin version {DiscordLink.Obj.PluginVersion}." +
                    "\nIt connects the game server to a Discord bot in order to perform seamless communication between Eco and Discord." +
                    "\nThis enables you to chat with players who are currently not online in Eco, but are available on Discord." +
                    "\nDiscordLink can also be used to display information about the Eco server in Discord, such as who is online and what items are available on the market." +
                    "\n\nFor more information, visit \"www.github.com/Eco-DiscordLink/EcoDiscordPlugin\".";
            }

            public static string GetLinkAccountInfoMessage(SharedCommands.CommandInterface context )
            {
                return "By linking your Eco account to your Discord account on this server, you can enable the following features:" +
                    $"\n* Trade Watcher Displays - An always up to date view of a {MessageUtils.GetCommandTokenForContext(context)}DLT command in your DMs with the DiscordLink bot." +
                    "\n* Discord election voting - Vote in elections directly via Discord." +
                    "\n* DiscordLinked Role and roles matching your demographics and specializations." +
                    "\n" +
                    "\nLink instructions" +
                    $"\n1. Use /DL-Link <UserName> in Eco. The username parameter is your Discord account name (not nickname) with or without the #xxxx at the end. Example: {MessageUtils.GetCommandTokenForContext(context)}DL-Link Monzun#1234" +
                    "\n2. If your account could be found on the Discord server, the bot will send you a DM." +
                    "\n3. Click the approve button on the message to verify that you are the owner of both the Eco and Discord account." +
                    "\n4. Your account is now linked!" +
                    "\n" +
                    "\nUnlinking" +
                    $"\nIf you no longer wish to have your account linked or you need to reset it for some reason, you can use {MessageUtils.GetCommandTokenForContext(context)}DL-Unlink." +
                    "\n" +
                    "\nAdditional Information" +
                    "\n* Your account link is only valid for one combination of Eco and Discord servers. If you join a new server, you will need to link your account on that server as well." +
                    "\n* Your account link remains active over world resets. It is only removed if you use the /dl-unlink command or if the server host deletes the persistent storage data.";
            }

            public static async Task<string> GetDisplayStringAsync(bool verbose)
            {
                DiscordLink plugin = DiscordLink.Obj;
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"DiscordLink {plugin.PluginVersion}");
                if (verbose)
                {
                    builder.AppendLine($"Server Name: {MessageUtils.FirstNonEmptyString(DLConfig.Data.ServerName, MessageUtils.StripTags(NetworkManager.GetServerInfo().Description), "[Server Title Missing]")}");
                    builder.AppendLine($"Server Version: {EcoVersion.VersionNumber}");
                    if (DiscordLink.Obj.Client.ConnectionStatus == DLDiscordClient.ConnectionState.Connected)
                        builder.AppendLine($"D# Version: {plugin.Client.DiscordClient.VersionString}");
                }
                builder.AppendLine($"Plugin Status: {plugin.GetStatus()}");
                builder.AppendLine($"Discord Client Status: {plugin.Client.Status}");
                TimeSpan elapssedTime = DateTime.Now.Subtract(plugin.InitTime);
                if (verbose)
                    builder.AppendLine($"Start Time: {plugin.InitTime:yyyy-MM-dd HH:mm}");
                builder.AppendLine($"Running Time: {(int)elapssedTime.TotalDays}:{elapssedTime.Hours}:{elapssedTime.Minutes}");

                if (DiscordLink.Obj.Client.ConnectionStatus != DLDiscordClient.ConnectionState.Connected)
                    return builder.ToString();

                if (verbose)
                    builder.AppendLine($"Connection Time: {plugin.Client.LastConnectionTime:yyyy-MM-dd HH:mm}");

                builder.AppendLine();
                builder.AppendLine("--- User Data ---");
                builder.AppendLine($"Linked users: {DLStorage.PersistentData.LinkedUsers.Count}");
                builder.AppendLine();
                builder.AppendLine("--- Modules ---");

                string moduleDisplayText = plugin.Modules.Select(m => m.GetDisplayText(string.Empty, verbose)).DoubleNewlineList();
                builder.AppendLine(moduleDisplayText);

                if (verbose)
                {
                    builder.AppendLine();
                    builder.AppendLine("--- Config ---");
                    builder.AppendLine($"Name: {plugin.Client.DiscordClient.CurrentUser.Username}");
                    builder.AppendLine($"Has GuildMembers Intent: {plugin.Client.BotHasIntent(DiscordIntents.GuildMembers)}");

                    builder.AppendLine();
                    builder.AppendLine("--- Storage - Persistent ---");
                    builder.AppendLine("Linked User Data:");
                    foreach (LinkedUser linkedUser in DLStorage.PersistentData.LinkedUsers)
                    {
                        User ecoUser = linkedUser.EcoUser;
                        string ecoUserName = (ecoUser != null) ? MessageUtils.StripTags(ecoUser.Name) : "[Uknown Eco User]";

                        DiscordUser discordUser = linkedUser.DiscordMember;
                        string discordUserName = (discordUser != null) ? discordUser.Username : "[Unknown Discord User]";

                        string verified = (linkedUser.Verified) ? "Verified" : "Unverified";
                        builder.AppendLine($"{ecoUserName} <--> {discordUserName} - {verified}");
                    }

                    builder.AppendLine();
                    builder.AppendLine("--- Storage - World ---");
                    if (DLStorage.WorldData.TradeWatchers.Count > 0)
                    {
                        builder.AppendLine("Trade Watchers:");
                        foreach (var userTradeWatchers in DLStorage.WorldData.TradeWatchers)
                        {
                            DiscordUser discordUser = await plugin.Client.GetUserAsync(userTradeWatchers.Key);
                            if (discordUser == null)
                                continue;

                            builder.AppendLine($"[{discordUser.Username}]");
                            foreach (TradeWatcherEntry tradeWatcher in userTradeWatchers.Value)
                            {
                                builder.AppendLine($"- {tradeWatcher}");
                            }
                        }
                    }

                    builder.AppendLine();
                    builder.AppendLine("Cached Guilds:");
                    foreach (DiscordGuild guild in plugin.Client.DiscordClient.Guilds.Values)
                    {
                        builder.AppendLine($"- {guild.Name} ({guild.Id})");
                        builder.AppendLine("   Cached Channels");
                        foreach (DiscordChannel channel in guild.Channels.Values)
                        {
                            builder.AppendLine($"  - {channel.Name} ({channel.Id})");
                            builder.AppendLine($"      Permissions:");
                            builder.AppendLine($"          Read Messages:          {plugin.Client.ChannelHasPermission(channel, Permissions.ReadMessageHistory)}");
                            builder.AppendLine($"          Send Messages:          {plugin.Client.ChannelHasPermission(channel, Permissions.SendMessages)}");
                            builder.AppendLine($"          Manage Messages:        {plugin.Client.ChannelHasPermission(channel, Permissions.ManageMessages)}");
                            builder.AppendLine($"          Embed Links:            {plugin.Client.ChannelHasPermission(channel, Permissions.EmbedLinks)}");
                            builder.AppendLine($"          Mention Everyone/Here:  {plugin.Client.ChannelHasPermission(channel, Permissions.MentionEveryone)}");
                        }
                    }
                }

                return builder.ToString();
            }

            public static string GetPlayerCount()
            {
                return $"{EcoUtils.NumOnlinePlayers}/{EcoUtils.NumTotalPlayers}";
            }

            public static string GetOnlinePlayerList()
            {
                string playerList = string.Join("\n", EcoUtils.OnlineUsersAlphabetical.Select(u => MessageUtils.StripTags(u.Name)));
                if (string.IsNullOrEmpty(playerList))
                    playerList = "-- No players online --";

                return playerList;
            }

            public static void GetActiveElectionsList(out string electionList, out string votesList, out string timeRemainingList)
            {
                electionList = string.Empty;
                votesList = string.Empty;
                timeRemainingList = string.Empty;
                foreach (Election election in EcoUtils.ActiveElections)
                {
                    electionList += $"{MessageUtils.StripTags(election.Name)}\n";
                    votesList += $"{election.TotalVotes} Votes\n";
                    timeRemainingList += $"{TimeFormatter.FormatSpan(election.TimeLeft)}\n";
                }
            }

            public static void GetActiveElectionsList(out string lawList, out string creatorList)
            {
                lawList = string.Empty;
                creatorList = string.Empty;
                foreach (Law law in EcoUtils.ActiveLaws)
                {
                    lawList += $"{MessageUtils.StripTags(law.Name)}\n";
                    creatorList += law.Creator != null ? $"{MessageUtils.StripTags(law.Creator.Name)}\n" : "Unknown\n";
                }
            }

            public static string GetPlayerSessionTimeList()
            {
                return string.Join("\n", EcoUtils.OnlineUsersAlphabetical.Select(u => GetTimeDescription(u.GetSecondsSinceLogin(), TimespanStringComponent.Hour | TimespanStringComponent.Minute)));
            }

            public static string GetPlayerExhaustionTimeList()
            {
                return string.Join("\n", EcoUtils.OnlineUsersAlphabetical.Select(u => u.ExhaustionMonitor.IsExhausted ? "Exhausted" : GetTimeDescription(u.GetSecondsLeftUntilExhaustion(), TimespanStringComponent.Hour | TimespanStringComponent.Minute)));
            }

            public enum TimespanStringComponent
            {
                Day = 1 << 0,
                Hour = 1 << 1,
                Minute = 1 << 2,
                Second = 1 << 3,
            }

            public static string GetGameTimeStamp()
            {
                double seconds = Simulation.Time.WorldTime.Seconds;
                return $"{((int)TimeUtil.SecondsToHours(seconds) % 24).ToString("00") }" +
                    $":{((int)(TimeUtil.SecondsToMinutes(seconds) % 60)).ToString("00")}" +
                    $":{((int)seconds % 60).ToString("00")}";
            }

            public static string GetServerTimeStamp()
            {
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            }

            public static string GetYesNo(bool flag)
            {
                return flag ? "Yes" : "No";
            }

            public static string GetTimeDescription(double seconds, TimespanStringComponent flag = TimespanStringComponent.Day | TimespanStringComponent.Hour | TimespanStringComponent.Minute | TimespanStringComponent.Second, bool includeZeroTimes = true, bool annotate = false)
            {
                StringBuilder builder = new StringBuilder();
                if ((flag & TimespanStringComponent.Day) != 0)
                {
                    int daysCount = (int)TimeUtil.SecondsToDays(seconds);
                    if (includeZeroTimes || daysCount > 0)
                    {
                        builder.Append(daysCount.ToString("00"));
                        if (annotate)
                            builder.Append("D ");
                    }
                }

                if ((flag & TimespanStringComponent.Hour) != 0)
                {
                    int hoursCount = (int)TimeUtil.SecondsToHours(seconds) % 24;
                    if (includeZeroTimes || hoursCount > 0)
                    {
                        if (!annotate && builder.Length != 0)
                            builder.Append(":");
                        builder.Append(hoursCount.ToString("00"));
                        if (annotate)
                            builder.Append("H ");
                    }
                }

                if ((flag & TimespanStringComponent.Minute) != 0)
                {
                    int minutesCount = (int)TimeUtil.SecondsToMinutes(seconds) % 60;
                    if (includeZeroTimes || minutesCount > 0)
                    {
                        if (!annotate && builder.Length != 0)
                            builder.Append(":");
                        builder.Append(minutesCount.ToString("00"));
                        if (annotate)
                            builder.Append("M ");
                    }
                }

                if ((flag & TimespanStringComponent.Second) != 0)
                {
                    int secondsCount = (int)seconds % 60;
                    if (includeZeroTimes || secondsCount > 0)
                    {
                        if (!annotate && builder.Length != 0)
                            builder.Append(":");
                        builder.Append(secondsCount.ToString("00"));
                        if (annotate)
                            builder.Append("S");
                    }
                }
                return builder.ToString().Trim();
            }
        }

        public static class Discord
        {
            public static string GetActivityString()
            {
                int onlinePlayers = EcoUtils.NumOnlinePlayers;
                string activityString;
                if (onlinePlayers > 0)
                {
                    string playerDesc = onlinePlayers == 1 ? "player" : "players";
                    activityString = $"{onlinePlayers} {playerDesc} play Eco";
                }
                else
                {
                    int randomNumber = new Random().Next(2);
                    if (randomNumber == 0)
                    {
                        AnimalSpecies animal = (AnimalSpecies)Simulation.EcoSim.AllSpecies.Where(species => species.GetType().DerivesFrom(typeof(AnimalSpecies))).Random();
                        string animalName = animal.DisplayName.ToLower();
                        string movementDesc;
                        if (animal.Swimming)
                            movementDesc = "swim";
                        else if (animal.Flying)
                            movementDesc = "fly";
                        else
                            movementDesc = "run";

                        activityString = $"{animalName} {movementDesc} around";
                    }
                    else
                    {
                        string plantName = Simulation.EcoSim.AllSpecies.Where(species => species.GetType().DerivesFrom(typeof(PlantSpecies)) || species.GetType().DerivesFrom(typeof(TreeSpecies))).Random().DisplayName.ToLower();
                        activityString = $"{plantName} grow";
                    }
                }
                return activityString;
            }

            public static DiscordLinkEmbed GetServerInfo(ServerInfoComponentFlag flag)
            {
                var plugin = DiscordLink.Obj;

                DLConfigData config = DLConfig.Data;
                ServerInfo serverInfo = NetworkManager.GetServerInfo();

                DiscordLinkEmbed embed = new DiscordLinkEmbed();

                if (flag.HasFlag(ServerInfoComponentFlag.Name))
                {
                    embed.WithTitle($"**{MessageUtils.FirstNonEmptyString(config.ServerName, MessageUtils.StripTags(serverInfo.Description), "[Server Title Missing]")} Server Status**\n{DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString()}");
                }
                else
                {
                    DateTime time = DateTime.Now;
                    int utcOffset = TimeZoneInfo.Local.GetUtcOffset(time).Hours;
                    embed.WithTitle($"**Server Status**\n[{DateTime.Now.ToString("yyyy-MM-dd : HH:mm", CultureInfo.InvariantCulture)} UTC {(utcOffset != 0 ? (utcOffset >= 0 ? "+" : "-") + utcOffset : "")}]");
                }

                if (flag.HasFlag(ServerInfoComponentFlag.Description))
                {
                    embed.WithDescription(MessageUtils.FirstNonEmptyString(config.ServerDescription, MessageUtils.StripTags(serverInfo.Description), "No server description is available."));
                }

                if (flag.HasFlag(ServerInfoComponentFlag.Logo) && !string.IsNullOrWhiteSpace(config.ServerLogo))
                {
                    embed.WithThumbnail(config.ServerLogo);
                }

                if (flag.HasFlag(ServerInfoComponentFlag.ConnectionInfo))
                {
                    string fieldText = "-- Connection info not configured --";
                    if (!string.IsNullOrEmpty(config.ConnectionInfo))
                        fieldText = config.ConnectionInfo;
                    else if (!string.IsNullOrEmpty(serverInfo.Address))
                        fieldText = serverInfo.Address;

                    embed.AddField("Connection Info", fieldText);
                }

                if (flag.HasFlag(ServerInfoComponentFlag.WebServerAddress))
                {
                    string fieldText = "-- Webpage Address not configured --";
                    if (!string.IsNullOrEmpty(config.WebServerAddress))
                        fieldText = $"{MessageUtils.GetWebserverBaseURL()}/index.html";

                    embed.AddField("Webpage Address", fieldText);
                }

                if (flag.HasFlag(ServerInfoComponentFlag.PlayerCount) || flag.HasFlag(ServerInfoComponentFlag.LawCount) || flag.HasFlag(ServerInfoComponentFlag.ActiveElectionCount))
                {
                    int fieldsAdded = 0;
                    if (flag.HasFlag(ServerInfoComponentFlag.PlayerCount))
                    {
                        embed.AddField("Player Count", $"{EcoUtils.NumOnlinePlayers} Online / {serverInfo.TotalPlayers} Total", inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.LawCount))
                    {
                        embed.AddField("Law Count", $"{EcoUtils.ActiveLaws.Count()}", inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.ActiveElectionCount))
                    {
                        embed.AddField("Active Elections Count", $"{EcoUtils.ActiveElections.Count()}", inline: true);
                        ++fieldsAdded;
                    }

                    for (int i = fieldsAdded; i < DLConstants.DISCORD_EMBED_FIELDS_PER_ROW_LIMIT; ++i)
                    {
                        embed.AddAlignmentField();
                    }
                }

                if (flag.HasFlag(ServerInfoComponentFlag.IngameTime) || flag.HasFlag(ServerInfoComponentFlag.MeteorTimeRemaining) || flag.HasFlag(ServerInfoComponentFlag.ServerTime))
                {
                    int fieldsAdded = 0;
                    if (flag.HasFlag(ServerInfoComponentFlag.IngameTime))
                    {
                        TimeSpan timeSinceStartSpan = new TimeSpan(0, 0, (int)serverInfo.TimeSinceStart);
                        embed.AddField("Ingame Time", $"Day {timeSinceStartSpan.Days + 1} {timeSinceStartSpan.Hours.ToString("00")}:{timeSinceStartSpan.Minutes.ToString("00")}", inline: true); // +1 days to get start at day 1 just like ingame
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.MeteorTimeRemaining))
                    {
                        TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                        bool meteorHasHit = timeRemainingSpan.Seconds < 0;
                        timeRemainingSpan = meteorHasHit ? new TimeSpan(0, 0, 0) : timeRemainingSpan;
                        embed.AddField("Time Remaining", Shared.GetTimeDescription(timeRemainingSpan.TotalSeconds, Shared.TimespanStringComponent.Day | Shared.TimespanStringComponent.Hour | Shared.TimespanStringComponent.Minute, includeZeroTimes: false, annotate: true), inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.ServerTime))
                    {
                        TimeSpan timeSinceStartSpan = new TimeSpan(0, 0, (int)serverInfo.TimeSinceStart);
                        embed.AddField("Server Time", Shared.GetServerTimeStamp(), inline: true);
                        ++fieldsAdded;
                    }

                    for (int i = fieldsAdded; i < DLConstants.DISCORD_EMBED_FIELDS_PER_ROW_LIMIT; ++i)
                    {
                        embed.AddAlignmentField();
                    }
                }

                if (BalancePlugin.Obj.Config.IsLimitingHours && flag.HasFlag(ServerInfoComponentFlag.ExhaustionResetTime) || flag.HasFlag(ServerInfoComponentFlag.ExhaustionResetTimeLeft) || flag.HasFlag(ServerInfoComponentFlag.ExhaustedPlayerCount))
                {
                    int fieldsAdded = 0;
                    if (flag.HasFlag(ServerInfoComponentFlag.ExhaustionResetTime))
                    {
                        embed.AddField("Exhaustion Reset Time", DateTime.Now.AddSeconds(EcoUtils.SecondsLeftOnDay).ToString("yyyy-MM-dd HH:mm"), inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.ExhaustionResetTimeLeft))
                    {
                        embed.AddField("Exhaustion Reset Countdown", Shared.GetTimeDescription(EcoUtils.SecondsLeftOnDay, Shared.TimespanStringComponent.Hour | Shared.TimespanStringComponent.Minute, includeZeroTimes: true, annotate: true), inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.ExhaustedPlayerCount))
                    {
                        embed.AddField("Exhausted Players Count", EcoUtils.NumExhaustedPlayers.ToString(), inline: true);
                        ++fieldsAdded;
                    }

                    for (int i = fieldsAdded; i < DLConstants.DISCORD_EMBED_FIELDS_PER_ROW_LIMIT; ++i)
                    {
                        embed.AddAlignmentField();
                    }
                }

                if (flag.HasFlag(ServerInfoComponentFlag.PlayerList))
                {
                    embed.AddField($"Online Players ({EcoUtils.NumOnlinePlayers})", Shared.GetOnlinePlayerList(), inline: true);
                    if (flag.HasFlag(ServerInfoComponentFlag.PlayerListLoginTime))
                    {
                        string sessionTimeList = Shared.GetPlayerSessionTimeList();
                        if (!string.IsNullOrWhiteSpace(sessionTimeList))
                            embed.AddField("Session Time", sessionTimeList, inline: true);
                        else
                            embed.AddAlignmentField();
                    }
                    else
                    {
                        embed.AddAlignmentField();
                    }

                    if(flag.HasFlag(ServerInfoComponentFlag.PlayerListExhaustionTime) && BalancePlugin.Obj.Config.IsLimitingHours)
                    {
                        string exhaustTimeList = Shared.GetPlayerExhaustionTimeList();
                        if (!string.IsNullOrWhiteSpace(exhaustTimeList))
                            embed.AddField("Exhaustion Countdown", exhaustTimeList, inline: true);
                        else
                            embed.AddAlignmentField();
                    }
                    else
                    {
                        embed.AddAlignmentField();
                    }
                }

                if (flag.HasFlag(ServerInfoComponentFlag.ActiveElectionList))
                {
                    Shared.GetActiveElectionsList(out string electionList, out string votesList, out string timeRemainingList);
                    if (!string.IsNullOrEmpty(electionList))
                    {
                        embed.AddField("Active Elections", electionList, inline: true);
                        embed.AddField("Votes", votesList, inline: true);
                        embed.AddField("Time Remaining", timeRemainingList, inline: true);
                    }
                    else
                    {
                        embed.AddField("Active Elections", "-- No active elections --", inline: true);
                        embed.AddAlignmentField();
                        embed.AddAlignmentField();
                    }
                }

                if (flag.HasFlag(ServerInfoComponentFlag.LawList))
                {
                    Shared.GetActiveElectionsList(out string lawList, out string creatorList);
                    if (!string.IsNullOrEmpty(lawList))
                    {
                        embed.AddField("Active Laws", lawList, inline: true);
                        embed.AddField("Creator", creatorList, inline: true);
                        embed.AddAlignmentField();
                    }
                    else
                    {
                        embed.AddField("Active Laws", "-- No active laws --", inline: true);
                        embed.AddAlignmentField();
                        embed.AddAlignmentField();
                    }
                }

                return embed;
            }

            public static async Task<DiscordLinkEmbed> GetPlayerReport(User user, PlayerReportComponentFlag flag)
            {
                LinkedUser linkedUser = UserLinkManager.LinkedUserByEcoUser(user);
                DiscordMember discordMember = null;
                bool userLinkExists = linkedUser != null;
                if (userLinkExists)
                    discordMember = linkedUser.DiscordMember;

                DiscordLinkEmbed report = new DiscordLinkEmbed();
                report.WithTitle($"Report for {MessageUtils.StripTags(user.Name)}");

                // Online Status
                if (flag.HasFlag(PlayerReportComponentFlag.OnlineStatus))
                {
                    report.AddField("Online", Shared.GetYesNo(user.IsOnline), inline: true);
                    if (user.IsOnline)
                        report.AddField("Session Time", Shared.GetTimeDescription(user.GetSecondsSinceLogin(), Shared.TimespanStringComponent.Hour | Shared.TimespanStringComponent.Minute), inline: true);
                    else
                        report.AddField("Last Online", $"{Shared.GetTimeDescription(user.GetSecondsSinceLogout(), includeZeroTimes: false, annotate: true)} ago", inline: true);
                    report.AddAlignmentField();
                }

                // Play time
                if (flag.HasFlag(PlayerReportComponentFlag.PlayTime))
                {
                    report.AddField("Playtime Total", Shared.GetTimeDescription(user.OnlineTimeLog.SecondsOnline(0.0)), inline: true);
                    report.AddField("Playtime last 24 hours", Shared.GetTimeDescription(user.OnlineTimeLog.SecondsOnline(DLConstants.SECONDS_PER_DAY), Shared.TimespanStringComponent.Hour | Shared.TimespanStringComponent.Minute | Shared.TimespanStringComponent.Second), inline: true);
                    report.AddField("Playtime Last 7 days", Shared.GetTimeDescription(user.OnlineTimeLog.SecondsOnline(DLConstants.SECONDS_PER_WEEK)), inline: true);
                }

                // Exhaustion
                if (flag.HasFlag(PlayerReportComponentFlag.Exhaustion) && BalancePlugin.Obj.Config.IsLimitingHours)
                {
                    report.AddField("Exhaustion Countdown", user.ExhaustionMonitor.IsExhausted ? "Exhausted" : Shared.GetTimeDescription(user.GetSecondsLeftUntilExhaustion(), Shared.TimespanStringComponent.Hour | Shared.TimespanStringComponent.Minute | Shared.TimespanStringComponent.Second), inline: true);
                    report.AddAlignmentField();
                    report.AddAlignmentField();
                }

                // Permissions
                if (flag.HasFlag(PlayerReportComponentFlag.Permissions))
                {
                    report.AddField("Eco Admin", Shared.GetYesNo(user.IsAdmin), inline: true);
                    if (userLinkExists)
                        report.AddField("Discord Admin", Shared.GetYesNo(DiscordLink.Obj.Client.MemberIsAdmin(discordMember)), inline: true);
                    report.AddField("Eco Dev Permission", Shared.GetYesNo(user.IsDev), inline: true);
                    if (!userLinkExists)
                        report.AddAlignmentField();
                }

                // Access lists
                if (flag.HasFlag(PlayerReportComponentFlag.AccessLists))
                {
                    report.AddField("Whitelisted", Shared.GetYesNo(user.IsWhitelisted()), inline: true);
                    report.AddField("Banned", Shared.GetYesNo(user.IsBanned()), inline: true);
                    report.AddField("Muted", Shared.GetYesNo(user.IsMuted()), inline: true);
                }

                // Discord Account Info
                if (flag.HasFlag(PlayerReportComponentFlag.DiscordInfo))
                {
                    if (userLinkExists)
                    {
                        report.AddField("Linked Discord Account", discordMember.DisplayName, inline: true);
                        report.AddField($"Top Discord Role", discordMember.GetHighestHierarchyRoleName(), inline: true);
                        report.AddField($"Joined {discordMember.Guild.Name} at: ", discordMember.JoinedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), inline: true);
                    }
                    else
                    {
                        report.AddField("Linked Discord Account", "Not Linked", inline: true);
                        report.AddAlignmentField();
                        report.AddAlignmentField();
                    }
                }

                // Reputation
                if (flag.HasFlag(PlayerReportComponentFlag.Reputation))
                {
                    Reputation reputation = ReputationManager.Obj.Rep(user.Name, createIfMissing: false);
                    report.AddField("Reputation", ((int)reputation.CachedTotalReputation).ToString(), inline: true);
                    report.AddField("Can Give Today", ((int)reputation.GiveableReputation).ToString(), inline: true);
                    report.AddAlignmentField();
                }

                // XP Multiplier
                if (flag.HasFlag(PlayerReportComponentFlag.Experience))
                {
                    report.AddField("Total XP Multiplier", ((int)user.GetTotalXPMultiplier()).ToString(), inline: true);
                    report.AddField("Nutrition", ((int)user.GetNutritionXP()).ToString(), inline: true);
                    report.AddField("Housing", ((int)user.GetHousingXP()).ToString(), inline: true);
                }

                // Skills
                if (flag.HasFlag(PlayerReportComponentFlag.Skills))
                {
                    StringBuilder skillsDesc = new StringBuilder();
                    StringBuilder levelsDesc = new StringBuilder();
                    StringBuilder percentOfNextLevelDoneDesc = new StringBuilder();

                    IEnumerable<Skill> orderedSkills = user.Skillset.Skills.Where(s => s.Level > 0).OrderByDescending(s => s.Level);
                    foreach (Skill skill in orderedSkills)
                    {
                        bool maxLevelReached = skill.Level >= skill.MaxLevel;
                        skillsDesc.AppendLine(skill.DisplayName);
                        levelsDesc.AppendLine(skill.Level.ToString() + (maxLevelReached ? " (Max)" : string.Empty));
                        percentOfNextLevelDoneDesc.AppendLine(maxLevelReached ? "N/A" : $"{(int)skill.PercentTowardsNextLevel}%");
                    }

                    report.AddField("Skills", skillsDesc.ToString(), inline: true);
                    report.AddField("Level", levelsDesc.ToString(), inline: true);
                    report.AddField("Percent of Next Level", percentOfNextLevelDoneDesc.ToString(), inline: true);
                }

                // Demographics
                if (flag.HasFlag(PlayerReportComponentFlag.Demographics))
                {
                    StringBuilder demographicDesc = new StringBuilder();
                    IEnumerable<Demographic> userDemographics = EcoUtils.ActiveDemographics.Where(demographic => demographic.Contains(user)).OrderBy(demographic => demographic.Name);
                    foreach (Demographic demographic in userDemographics)
                    {
                        demographicDesc.AppendLine(demographic.Name + (demographic.Creator == user ? " (Creator)" : string.Empty));
                    }
                    report.AddField("Demographics", userDemographics.Count() > 0 ? demographicDesc.ToString() : "No Demographics", inline: true);
                }

                // Titles
                if (flag.HasFlag(PlayerReportComponentFlag.Titles))
                {
                    StringBuilder titlesDesc = new StringBuilder();
                    IEnumerable<Title> userTitles = EcoUtils.ActiveTitles.Where(title => title.UserSet.Contains(user)).OrderBy(title => title.Name);
                    foreach (Title title in userTitles)
                    {
                        titlesDesc.AppendLine(title.Name + (title.Creator == user ? " (Creator)" : string.Empty));
                    }
                    report.AddField("Titles", userTitles.Count() > 0 ? titlesDesc.ToString() : "No Titles", inline: true);
                    report.AddAlignmentField();
                }

                // Deeds
                if (flag.HasFlag(PlayerReportComponentFlag.Properties))
                {
                    StringBuilder propertiessDesc = new StringBuilder();
                    StringBuilder propertiessSizeOrVehicleDesc = new StringBuilder();
                    StringBuilder propertiessLocationDesc = new StringBuilder();

                    IEnumerable<Deed> userDeeds = EcoUtils.Deeds.Where(deed => deed.ContainsOwners(user)).OrderByDescending(deed => deed.GetTotalPlotSize());
                    foreach (Deed deed in userDeeds)
                    {
                        propertiessDesc.AppendLine(deed.Name.TrimEndString(" Deed"));
                        propertiessSizeOrVehicleDesc.AppendLine(deed.IsVehicle() ? deed.GetVehicle().Parent.CreatingItem.DisplayName : $"{deed.GetTotalPlotSize()}m²");
                        propertiessLocationDesc.AppendLine(deed.IsVehicle() ? deed.GetVehicle().Parent.Position3i.ToString() : deed.Position3i.ToString());
                    }

                    bool hasProperty = userDeeds.Count() > 0;
                    report.AddField("Property", hasProperty ? propertiessDesc.ToString() : "No Owned Property", inline: true);
                    report.AddField("Size/Vehicle", hasProperty ? propertiessSizeOrVehicleDesc.ToString() : "N/A", inline: true);
                    report.AddField("Location", hasProperty ? propertiessLocationDesc.ToString() : "N/A", inline: true);
                }

                return report;
            }

            public static DiscordLinkEmbed GetCurrencyReport(Currency currency, int maxTopHolders, bool useBackingInfo, bool useTradeCount)
            {
                var currencyTradesMap = DLStorage.WorldData.CurrencyToTradeCountMap;

                DiscordLinkEmbed embed = new DiscordLinkEmbed();
                embed.WithTitle(MessageUtils.StripTags(currency.Name));

                // Find and sort relevant accounts
                IEnumerable<BankAccount> accounts = BankAccountManager.Obj.Accounts.Where(acc => acc.GetCurrencyHoldingVal(currency) >= 1).OrderByDescending(acc => acc.GetCurrencyHoldingVal(currency));
                int tradesCount = currencyTradesMap.Keys.Contains(currency.Id) ? currencyTradesMap[currency.Id] : 0;

                var accountEnumerator = accounts.GetEnumerator();
                string topAccounts = string.Empty;
                string amounts = string.Empty;
                string topAccountHolders = string.Empty;
                for (int i = 0; i < maxTopHolders && accountEnumerator.MoveNext(); ++i)
                {
                    // Some bank accounts (e.g treasury) have no creator and one will belong to the bot
                    // Unbacked currencies has their creator owning infinity
                    float currencyAmount = accountEnumerator.Current.GetCurrencyHoldingVal(currency);
                    if (accountEnumerator.Current.Creator == null || accountEnumerator.Current.Creator == DiscordLink.Obj.EcoUser || currencyAmount == float.PositiveInfinity)
                    {
                        --i;
                        continue;
                    }
                    topAccounts += $"{MessageUtils.StripTags(accountEnumerator.Current.Name)}\n";
                    amounts += $"**{accountEnumerator.Current.GetCurrencyHoldingVal(currency):n0}**\n";
                    topAccountHolders += $"{accountEnumerator.Current.Creator.Name}\n";
                }

                if (tradesCount <= 0 && string.IsNullOrWhiteSpace(topAccounts))
                    return null;

                string backededItemName = currency.Backed ? $"{currency.BackingItem.DisplayName}" : "Personal";

                // Build message
                if (useTradeCount)
                    embed.AddField("Total trades", tradesCount.ToString("n0"), inline: true);
                
                embed.AddField("Amount in circulation", currency.Circulation.ToString("n0"), inline: true);
                embed.AddAlignmentField();
                if (!useTradeCount)
                    embed.AddAlignmentField();

                if (useBackingInfo && currency.Backed)
                {
                    embed.AddField("Backing", backededItemName, inline: true);
                    embed.AddField("Coins per item", currency.CoinsPerItem.ToString("n0"), inline: true);
                    embed.AddAlignmentField();
                }

                if (!string.IsNullOrWhiteSpace(topAccounts))
                {
                    embed.AddField("Top Holders", topAccountHolders, inline: true);
                    embed.AddField("Amount", amounts, inline: true);
                    embed.AddField("Account", topAccounts, inline: true);
                }
                else
                {
                    embed.AddField("Top Holders", "-- No player holding this currency --", inline: true);
                }

                return embed;
            }

            public static DiscordLinkEmbed GetElectionReport(Election election)
            {
                DiscordLinkEmbed report = new DiscordLinkEmbed();
                report.WithTitle(MessageUtils.StripTags(election.Name));

                // Link
                string webServerURL = MessageUtils.GetWebserverBaseURL();
                if (!string.IsNullOrWhiteSpace(webServerURL))
                    report.AddField("URL", $"{webServerURL}/elections.html?election={election.Id}");

                // Proposer name
                report.AddField("Proposer", election.Creator.Name, inline: true);

                // Process
                report.AddField("Process", MessageUtils.StripTags(election.Process.Name), inline: true);

                // Time Remaining
                report.AddField("Time Remaining", TimeFormatter.FormatSpan(election.TimeLeft), inline: true);

                // Votes
                string voteDesc = string.Empty;
                string choiceDesc = string.Empty;
                if (!election.Process.AnonymousVoting)
                {
                    foreach(ElectionChoice choice in election.Choices)
                    {
                        foreach(RunoffVote vote in election.Votes)
                        {
                            for( int i = 0; i < vote.RankedVotes.Count; ++i)
                            {
                                if(vote.RankedVotes[i] == choice.ID)
                                {

                                }
                            }
                        }
                    }

                    foreach (RunoffVote vote in election.Votes)
                    {
                        string topChoiceName = null;
                        int topChoiceID = vote.RankedVotes.FirstOrDefault();
                        foreach (ElectionChoice choice in election.Choices)
                        {
                            if (choice.ID == topChoiceID)
                            {
                                topChoiceName = choice.Name;
                                break;
                            }
                        }
                        voteDesc += $"{vote.Voter.Name}\n";
                        choiceDesc += $"{topChoiceName}\n";
                    }
                }
                else
                {
                    voteDesc = "-- Anonymous Voting --";
                }

                if (string.IsNullOrEmpty(voteDesc))
                    voteDesc = "-- No Votes Recorded --";

                report.AddField($"Votes ({election.TotalVotes})", voteDesc, inline: true);

                if (!string.IsNullOrEmpty(choiceDesc))
                    report.AddField("Choice", choiceDesc, inline: true);
                else
                    report.AddAlignmentField();

                // Options
                if (!election.BooleanElection && election.Choices.Count > 0)
                {
                    string optionsDesc = string.Empty;
                    foreach (ElectionChoice choice in election.Choices)
                    {
                        optionsDesc += $"{choice.Name}\n";
                    }
                    report.AddField("Options", optionsDesc, inline: true);
                }
                else
                {
                    report.AddAlignmentField();
                }

                return report;
            }

            public static DiscordLinkEmbed GetWorkPartyReport(WorkParty workParty)
            {
                DiscordLinkEmbed report = new DiscordLinkEmbed();
                report.WithTitle(MessageUtils.StripTags(workParty.Name));

                // Workers
                string workersDesc = string.Empty;
                foreach (Laborer laborer in workParty.Laborers)
                {
                    if (laborer.Citizen == null) continue;
                    string creator = (laborer.Citizen == workParty.Creator) ? "(Creator)" : string.Empty;
                    workersDesc += $"{laborer.Citizen.Name} {creator}\n";
                }

                if (string.IsNullOrWhiteSpace(workersDesc))
                {
                    workersDesc += "-- No Workers Registered --";
                }
                report.AddField("Workers", workersDesc);

                // Work
                foreach (Work work in workParty.Work)
                {
                    string workDesc = string.Empty;
                    string workType = string.Empty;
                    List<string> workEntries = new List<string>();
                    switch (work)
                    {
                        case LaborWork laborWork:
                            {
                                if (!string.IsNullOrEmpty(laborWork.ShortDescriptionRemaining))
                                {
                                    workType = $"Labor for {laborWork.Order.Recipe.RecipeName}";
                                    workEntries.Add(MessageUtils.StripTags(laborWork.ShortDescriptionRemaining));
                                }
                                break;
                            }

                        case WorkOrderWork orderWork:
                            {
                                workType = $"Materials for {orderWork.Order.Recipe.RecipeName}";
                                foreach (TagStack stack in orderWork.Order.MissingIngredients)
                                {
                                    string itemName = string.Empty;
                                    if (stack.Item != null)
                                        itemName = stack.Item.DisplayName;
                                    else if (stack.StackObject != null)
                                        itemName = stack.StackObject.DisplayName;
                                    workEntries.Add($"{itemName} ({stack.Quantity})");
                                }
                                break;
                            }

                        default:
                            break;
                    }

                    if (workEntries.Count > 0)
                    {
                        foreach (string material in workEntries)
                        {
                            workDesc += $"- {material}\n";
                        }

                        if (!string.IsNullOrWhiteSpace(workDesc))
                        {
                            string percentDone = (work.PercentDone * 100.0f).ToString("N1", CultureInfo.InvariantCulture).Replace(".0", "");
                            report.AddField($"\n {workType} (Weight: {work.Weight.ToString("F1")}) ({percentDone}% completed) \n", workDesc);
                        }
                    }
                }

                // Payment
                string paymentDesc = string.Empty;
                foreach (Payment payment in workParty.Payment)
                {
                    string desc = string.Empty;
                    switch (payment)
                    {
                        case CurrencyPayment currencyPayment:
                            {
                                float currencyAmountLeft = currencyPayment.Amount - currencyPayment.AmountPaid;
                                if (currencyAmountLeft > 0.0f)
                                {
                                    desc = $"Receive **{currencyAmountLeft.ToString("F1")} {currencyPayment.Currency.Name}**"
                                        + (currencyPayment.PayType == PayType.SplitByWorkPercent ? ", split based on work performed" : ", split evenly")
                                        + (currencyPayment.PayAsYouGo ? ", paid as work is performed." : ", paid when the project finishes.");
                                }
                                break;
                            }

                        case GrantTitlePayment titlePayment:
                            {
                                desc = $"Receive title `{MessageUtils.StripTags(titlePayment.Title.Name)}` if work contributed is at least *{titlePayment.MinContributedPercent.ToString("F1")}%*.";
                                break;
                            }

                        case KnowledgeSharePayment knowledgePayment:
                            {
                                if (knowledgePayment.Skills.Entries.Count > 0)
                                    desc = $"Receive knowledge of `{MessageUtils.StripTags(knowledgePayment.ShortDescription())}` if work contributed is at least *{knowledgePayment.MinContributedPercent.ToString("F1")}%*.";
                                break;
                            }

                        case ReputationPayment reputationPayment:
                            {
                                float reputationAmountLeft = reputationPayment.Amount - reputationPayment.AmountPaid;
                                desc = $"Receive **{reputationAmountLeft.ToString("F1")} reputation** from *{workParty.Creator.Name}*"
                                    + (reputationPayment.PayType == PayType.SplitByWorkPercent ? ", split based on work performed" : ", split evenly")
                                    + (reputationPayment.PayAsYouGo ? ", paid as work is performed." : ", paid when the project finishes.");
                                break;
                            }

                        default:
                            break;
                    }

                    if (!string.IsNullOrEmpty(desc))
                        paymentDesc += $"- {desc}\n";
                }

                if (!string.IsNullOrWhiteSpace(paymentDesc))
                    report.AddField("Payment", paymentDesc);

                return report;
            }

            public static DiscordLinkEmbed GetAccumulatedTradeReport(List<CurrencyTrade> tradeList)
            {
                if (tradeList.Count <= 0)
                    return null;

                CurrencyTrade firstTrade = tradeList[0];
                DiscordLinkEmbed embed = new DiscordLinkEmbed();
                string leftName = firstTrade.Citizen.Name;
                string rightName = (firstTrade.WorldObject as WorldObject).Name;
                embed.WithTitle($"{leftName} traded at {MessageUtils.StripTags(rightName)}");

                // Go through all acumulated trade events and create a summary
                string boughtItemsDesc = string.Empty;
                float boughtTotal = 0;
                string soldItemsDesc = string.Empty;
                float soldTotal = 0;
                foreach (CurrencyTrade trade in tradeList)
                {
                    if (trade.BoughtOrSold == BoughtOrSold.Buying)
                    {
                        boughtItemsDesc += $"{trade.NumberOfItems} X {trade.ItemUsed.DisplayName} * {MathF.Round(trade.CurrencyAmount / trade.NumberOfItems, 2)} = {MathF.Round(trade.CurrencyAmount, 2)}\n";
                        boughtTotal += trade.CurrencyAmount;
                    }
                    else if (trade.BoughtOrSold == BoughtOrSold.Selling)
                    {
                        soldItemsDesc += $"{trade.NumberOfItems} X {trade.ItemUsed.DisplayName} * {MathF.Round(trade.CurrencyAmount / trade.NumberOfItems, 2)} = {MathF.Round(trade.CurrencyAmount, 2)}\n";
                        soldTotal += trade.CurrencyAmount;
                    }
                }

                if (!boughtItemsDesc.IsEmpty())
                {
                    boughtItemsDesc += $"\nTotal = {boughtTotal.ToString("n2")}";
                    embed.AddField("Bought", boughtItemsDesc, inline: true);
                }

                if (!soldItemsDesc.IsEmpty())
                {
                    soldItemsDesc += $"\nTotal = {soldTotal.ToString("n2")}";
                    embed.AddField("Sold", soldItemsDesc, inline: true);
                }

                // Currency transfer description
                string resultDesc;
                float subTotal = MathF.Round( soldTotal, 2) - Mathf.Round(boughtTotal, 2);
                if (Math.Abs(subTotal) <= 0.00f)
                {
                    resultDesc = "No curreny was exchanged.";
                }
                else
                {
                    string paidOrGained = subTotal < 0.0f ? "paid" : "gained";
                    resultDesc = $"*{leftName}* {paidOrGained} {MathF.Round(Math.Abs(subTotal), 2)} *{MessageUtils.StripTags(firstTrade.Currency.Name)}*.";
                }

                embed.AddField("Result", resultDesc, allowAutoLineBreak: true);
                return embed;
            }

            public static DiscordLinkEmbed GetVerificationDM(User ecoUser)
            {
                DLConfigData config = DLConfig.Data;
                ServerInfo serverInfo = NetworkManager.GetServerInfo();
                string serverName = MessageUtils.StripTags(!string.IsNullOrWhiteSpace(config.ServerName) ? DLConfig.Data.ServerName : MessageUtils.StripTags(serverInfo.Description));

                DiscordLinkEmbed embed = new DiscordLinkEmbed();
                embed.WithTitle("Account Linking Verification");
                embed.AddField("Initiator", MessageUtils.StripTags(ecoUser.Name));
                embed.AddField("Description", $"Your Eco account has been linked to your Discord account on the server \"{serverName}\".");
                embed.AddField("Action Required", $"If you initiated this action, click the {DLConstants.ACCEPT_EMOJI} below to verify that these accounts should be linked.");
                embed.WithFooter($"If you did not initiate this action, click the {DLConstants.DENY_EMOJI} and notify a server admin.\nThe account link cannot be used until verified.");
                return embed;
            }

            public static string GetStandardEmbedFooter()
            {
                string serverName = MessageUtils.FirstNonEmptyString(DLConfig.Data.ServerName, MessageUtils.StripTags(NetworkManager.GetServerInfo().Description), "[Server Title Missing]");
                string timestamp = Shared.GetServerTimeStamp();
                return $"Message sent by DiscordLink @ {serverName} [{timestamp}]";
            }

            public static void FormatTrades(string matchedName, TradeTargetType tradeType, StoreOfferList groupedBuyOffers, StoreOfferList groupedSellOffers, out DiscordLinkEmbed embedContent)
            {
                // Format message
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle($"Trade offers for {matchedName}");

                if (groupedSellOffers.Count() > 0 || groupedBuyOffers.Count() > 0)
                {
                    Func<Tuple<StoreComponent, TradeOffer>, string> getLabel = tradeType switch
                    {
                        TradeTargetType.Tag => t => $"{t.Item2.Stack.Item.DisplayName} @ *{MessageUtils.StripTags(t.Item1.Parent.Name)}*",
                        TradeTargetType.Item => t => $"@ *{MessageUtils.StripTags(t.Item1.Parent.Name)}*",
                        TradeTargetType.User => t => t.Item2.Stack.Item.DisplayName,
                        TradeTargetType.Store => t => t.Item2.Stack.Item.DisplayName,
                        _ => t => string.Empty,
                    };
                    ICollection<StoreOffer> Offers = TradeOffersToFields(groupedBuyOffers, groupedSellOffers, getLabel);

                    for (int i = 0; i < Offers.Count; ++i)
                    {
                        StoreOffer currentOffer = Offers.ElementAt(i);
                        StoreOffer previousOffer = null;
                        if (i - 1 >= 0)
                            previousOffer = Offers.ElementAt(i - 1);

                        if (currentOffer.Buying)
                        {
                            if (previousOffer != null)
                            {
                                if (previousOffer.Buying)
                                {
                                    embed.AddAlignmentField();
                                    embed.AddAlignmentField();
                                }
                            }
                            embed.AddField(currentOffer.Title, currentOffer.Description, allowAutoLineBreak: true, inline: true);
                        }
                        else
                        {
                            if (previousOffer != null)
                            {
                                if (previousOffer.Buying)
                                {
                                    embed.AddAlignmentField();
                                    if (previousOffer.Currency != currentOffer.Currency)
                                    {
                                        embed.AddAlignmentField();
                                        embed.AddAlignmentField();
                                        embed.AddAlignmentField();
                                    }
                                }
                                else
                                {
                                    embed.AddAlignmentField();
                                    embed.AddAlignmentField();
                                }
                            }
                            else
                            {
                                embed.AddAlignmentField();
                                embed.AddAlignmentField();
                            }
                            embed.AddField(currentOffer.Title, currentOffer.Description, allowAutoLineBreak: true, inline: true);
                        }
                    }
                }
                else
                {
                    embed.WithTitle($"No trade offers found for {matchedName}");
                }
                embedContent = embed;
            }

            private static ICollection<StoreOffer> TradeOffersToFields<T>(T buyOfferGroups, T sellOfferGroups, Func<Tuple<StoreComponent, TradeOffer>, string> getLabel)
                where T : StoreOfferList
            {
                List<StoreOffer> buyOffers = new List<StoreOffer>();
                foreach (var group in buyOfferGroups)
                {
                    var offerDescriptions = TradeOffersToDescriptions(group,
                        t => t.Item2.Price.ToString(),
                        t => getLabel(t),
                        t => t.Item2.Stack.Quantity);

                    var fieldBodyBuilder = new StringBuilder();
                    foreach (string offer in offerDescriptions)
                    {
                        fieldBodyBuilder.Append($"{offer}\n");
                    }

                    buyOffers.Add(new StoreOffer($"**Buying for {group.Key}**", fieldBodyBuilder.ToString(), group.First().Item1.Currency, buying: true));
                }

                List<StoreOffer> sellOffers = new List<StoreOffer>();
                foreach (var group in sellOfferGroups)
                {
                    var offerDescriptions = TradeOffersToDescriptions(group,
                        t => t.Item2.Price.ToString(),
                        t => getLabel(t),
                        t => t.Item2.Stack.Quantity);

                    var fieldBodyBuilder = new StringBuilder();
                    foreach (string offer in offerDescriptions)
                    {
                        fieldBodyBuilder.Append($"{offer}\n");
                    }

                    sellOffers.Add(new StoreOffer($"**Selling for {group.Key}**", fieldBodyBuilder.ToString(), group.First().Item1.Currency, buying: false));
                }

                // Group offers by their currency and order them by currency popularity
                List<StoreOffer> allOffersSorted = buyOffers.Concat(sellOffers).OrderBy(o => o.Currency != null ? o.Currency.Id : -1).OrderByDescending(o =>
                {
                    if (o.Currency == null)
                        return -1; // Sort barters last

                    DLStorage.WorldData.CurrencyToTradeCountMap.TryGetValue(o.Currency.Id, out int result);
                    return result;
                }).ToList();
                return allOffersSorted;
            }

            private static IEnumerable<string> TradeOffersToDescriptions<T>(IEnumerable<T> offers, Func<T, string> getPrice, Func<T, string> getLabel, Func<T, int?> getQuantity)
            {
                return offers.Select(t =>
                {
                    var price = getPrice(t);
                    var quantity = getQuantity(t);
                    var quantityString = quantity.HasValue ? $"{quantity.Value} - " : "";
                    var quantityAndPrice = $"{quantityString}${price}";
                    if (quantity == 0)
                        quantityAndPrice = $"~~{quantityAndPrice}~~";
                    return $"{quantityAndPrice} {getLabel(t)}";
                });
            }
        }

        public static class Eco
        {
            public static void FormatTrades(User user, TradeTargetType tradeType, StoreOfferList groupedBuyOffers, StoreOfferList groupedSellOffers, out string message)
            {
                // Format message
                StringBuilder builder = new StringBuilder();
                if (groupedSellOffers.Count() > 0 || groupedBuyOffers.Count() > 0)
                {
                    switch (tradeType)
                    {
                        case TradeTargetType.Tag:
                        case TradeTargetType.Item:
                            groupedBuyOffers = groupedBuyOffers.OrderByDescending(o => o.First().Item1.Currency != null ? DLStorage.WorldData.CurrencyToTradeCountMap.GetValueOrDefault(o.First().Item1.Currency.Id, 0) : int.MinValue); // Currency == null => Barter store
                            groupedSellOffers = groupedSellOffers.OrderByDescending(o => o.First().Item1.Currency != null ? DLStorage.WorldData.CurrencyToTradeCountMap.GetValueOrDefault(o.First().Item1.Currency.Id, 0) : int.MinValue);
                            break;

                        case TradeTargetType.User:
                        case TradeTargetType.Store:
                            break;
                    }

                    foreach (StoreOfferGroup group in groupedBuyOffers)
                    {
                        builder.AppendLine(Text.Bold(Text.Color(Color.Green, $"<--- Buying for {group.First().Item1.CurrencyName} --->")));
                        builder.Append(TradeOffersToDescriptions(group, user, tradeType));
                        builder.AppendLine();
                    }

                    foreach (StoreOfferGroup group in groupedSellOffers)
                    {
                        builder.AppendLine(Text.Bold(Text.Color(Color.Red, $"<--- Selling for {MessageUtils.StripTags(group.First().Item1.CurrencyName)} --->")));
                        builder.Append(TradeOffersToDescriptions(group, user, tradeType));
                        builder.AppendLine();
                    }
                }
                else
                {
                    builder.AppendLine("--- No trade offers available ---");
                }
                message = builder.ToString();
            }

            private static string TradeOffersToDescriptions(StoreOfferGroup offers, User user, TradeTargetType tradeType)
            {
                Func<Tuple<StoreComponent, TradeOffer>, string> getLabel = tradeType switch
                {
                    TradeTargetType.Tag => t => $"{t.Item2.Stack.Item.MarkedUpName} @ {t.Item1.Parent.MarkedUpName}",
                    TradeTargetType.Item => t => $"@ {t.Item1.Parent.MarkedUpName}",
                    TradeTargetType.User => t => t.Item2.Stack.Item.MarkedUpName,
                    TradeTargetType.Store => t => t.Item2.Stack.Item.MarkedUpName,
                    _ => t => string.Empty,
                };

                StringBuilder NormalOffers = new StringBuilder();
                StringBuilder NoQuantityOffers = new StringBuilder();
                StringBuilder DisabledOffers = new StringBuilder();
                foreach (var storeAndoffer in offers)
                {
                    StoreComponent store = storeAndoffer.Item1;
                    TradeOffer offer = storeAndoffer.Item2;

                    float price = offer.Price;
                    int quantity = offer.Stack.Quantity;
                    Currency currency = store.Currency;
                    float availableCurrency = offer.Buying ? store.BankAccount.GetCurrencyHoldingVal(currency) : user.GetWealthInCurrency(currency);
                    int maxTradeCount = price > 0f && !float.IsInfinity(availableCurrency) ? (int)Mathf.Floor(availableCurrency / price) : Int32.MaxValue; // Calculate how many items can be traded using the available money
                    if (offer.Buying)
                    {
                        if (offer.ShouldLimit && offer.MaxNumWanted < maxTradeCount) // If there is a buy limit that is lower than what can be afforded, lower to that limit
                            maxTradeCount = offer.MaxNumWanted;
                    }
                    else if (quantity < maxTradeCount)
                    {
                        maxTradeCount = quantity; // If there less items for sale than we can pay for, lower to the amount available for sale
                    }

                    var quantityString = (offer.Stack.Quantity == maxTradeCount || maxTradeCount == Int32.MaxValue)
                        ? $"{quantity}"
                        : $"{quantity} ({maxTradeCount})";
                    var line = $"{quantityString} - ${price} {getLabel(storeAndoffer)}";

                    // Apply color and sort
                    if (!store.OnOff.Enabled)
                    {
                        line = Text.Color(Color.Red, line);
                        DisabledOffers.AppendLine(line);
                    }
                    else if (offer.Stack.Quantity == 0)
                    {
                        line = Text.Color(Color.Yellow, line);
                        NoQuantityOffers.AppendLine(line);
                    }
                    else
                    {
                        NormalOffers.AppendLine(line);
                    }
                }

                return $"{NormalOffers}{NoQuantityOffers}{DisabledOffers}";
            }
        }
    }
}
