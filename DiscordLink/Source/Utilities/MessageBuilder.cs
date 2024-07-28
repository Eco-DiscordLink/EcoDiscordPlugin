using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Eco.Core.Utils;
using Eco.Gameplay.Civics;
using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Laws;
using Eco.Gameplay.Civics.Titles;
using Eco.Gameplay.Components;
using Eco.Gameplay.Disasters;
using Eco.Gameplay.Economy;
using Eco.Gameplay.Economy.Reputation;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Settlements;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems;
using Eco.Gameplay.Systems.Exhaustion;
using Eco.Gameplay.Components.Store;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.Networking;
using Eco.Simulation.Types;
using Eco.Shared.Networking;
using Eco.Shared.Utils;
using Eco.Shared;
using Eco.Shared.Items;
using Eco.Moose.Extensions;
using Eco.Moose.Utils.Lookups;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Eco.Shared.Mathf; // Avoiding collisions with system mathf
using static Eco.Moose.Features.Trade;

using Constants = Eco.Moose.Data.Constants.Constants;
using Text = Eco.Shared.Utils.Text;

using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.Store.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class MessageBuilder
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
            ExhaustionResetTimeLeft     = 1 << 9,
            ExhaustedPlayerCount        = 1 << 10,
            IngameTime                  = 1 << 11,
            MeteorTimeRemaining         = 1 << 12,
            ServerTime                  = 1 << 13,
            ActiveElectionCount         = 1 << 14,
            ActiveElectionList          = 1 << 15,
            LawCount                    = 1 << 16,
            LawList                     = 1 << 17,
            ActiveSettlementCount       = 1 << 18,
            ActiveSettlementList        = 1 << 19,
            All                         = ~0
        }

        public enum PlayerReportComponentFlag
        {
            [ChoiceName("Online")]          OnlineStatus    = 1 << 0,
            [ChoiceName("Playtime")]        PlayTime        = 1 << 1,
            [ChoiceName("Exhaustion")]      Exhaustion      = 1 << 2,
            [ChoiceName("Permissions")]     Permissions     = 1 << 3,
            [ChoiceName("Access")]          AccessLists     = 1 << 4,
            [ChoiceName("Discord")]         DiscordInfo     = 1 << 5,
            [ChoiceName("Reputation")]      Reputation      = 1 << 6,
            [ChoiceName("Reputation")]      Experience      = 1 << 7,
            [ChoiceName("Skills")]          Skills          = 1 << 8,
            [ChoiceName("Demographics")]    Demographics    = 1 << 9,
            [ChoiceName("Titles")]          Titles          = 1 << 10,
            [ChoiceName("Properties")]      Properties      = 1 << 11,
            [ChoiceName("All")]             All             = ~0
        }

        public enum PermissionReportComponentFlag
        {
            Intents             = 1 << 0,
            ServerPermissions   = 1 << 1,
            ChannelPermissions  = 1 << 2,
            All                 = ~0
        }

        #pragma warning restore format

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

            public static string GetVersionMessage()
            {
                Version? modIOVersion = DiscordLink.Obj.ModIoVersion;
                string modIOVersionDesc = modIOVersion != null ? $"Latest version: {modIOVersion.ToString(3)}" : "Latest version: Unknown";

                Version installedVersion = DiscordLink.Obj.InstalledVersion;
                string installedVersionDesc = $"Installed version: {installedVersion.ToString(3)}";

                if (modIOVersion == null)
                    modIOVersionDesc = Text.Color(Color.Red, modIOVersionDesc);

                if (modIOVersion != null && modIOVersion > installedVersion)
                    installedVersionDesc = Text.Color(Color.Red, installedVersionDesc);

                return $"{modIOVersionDesc}\n{installedVersionDesc}";
            }

            public static string GetAboutMessage()
            {
                return $"This server is running the DiscordLink plugin version {DiscordLink.Obj.InstalledVersion.ToString(3)}." +
                    "\nIt connects the game server to a Discord bot in order to perform seamless communication between Eco and Discord." +
                    "\nThis enables you to chat with players who are currently not online in Eco, but are available on Discord." +
                    "\nDiscordLink can also be used to display information about the Eco server in Discord, such as who is online and what items are available on the market." +
                    "\n\nFor more information, visit \"www.github.com/Eco-DiscordLink/EcoDiscordPlugin\".";
            }

            public static string GetLinkAccountInfoMessage()
            {
                return "By linking Eco to your Discord account on this server, you can enable the following features:" +
                    $"\n* Trade Watchers - Get trade information about your store or market items directly to your DMs!" +
                    "\n* Discord election voting - Vote in elections directly via Discord." +
                    "\n* DiscordLinked Role and roles matching your demographics and specializations." +
                    "\n" +
                    "\n**Link instructions**" +
                    $"\n1. Use `/DL LinkAccount <UserName>` in Eco. The username is your Discord account name (not nickname). Example: `/DL LinkAccount Monzun`" +
                    "\n2. If your account could be found on the Discord server, the bot will send you a DM." +
                    "\n3. Click the approve button on the message to verify that you are the owner of both the Eco and Discord account." +
                    "\n4. Your account is now linked!" +
                    "\n" +
                    "\n**Unlinking**" +
                    $"\nIf you no longer wish to have your account linked, you can use the `/DL UnlinkAccount`. command." +
                    "\n" +
                    "\n**Additional Information**" +
                    "\n* Your account link is only valid for one combination of Eco and Discord servers. If you join a new server, you will need to link your account on that server as well." +
                    "\n* Your account link remains active over world resets. It is only removed if you use the `/DL UnlinkAccount` command or if the server host deletes the persistent storage data.";
            }

            public static async Task<string> GetDisplayStringAsync(bool verbose)
            {
                DiscordLink plugin = DiscordLink.Obj;
                DiscordClient client = plugin.Client;
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"DiscordLink {plugin.InstalledVersion.ToString(3)}");
                if (verbose)
                {
                    builder.AppendLine($"Server Name: {MessageUtils.FirstNonEmptyString(DLConfig.Data.ServerName, MessageUtils.StripTags(NetworkManager.GetServerInfo().Description), "[Server Title Missing]")}");
                    builder.AppendLine($"Server Version: {EcoVersion.VersionNumber}");
                    if (client.ConnectionStatus == DiscordClient.ConnectionState.Connected)
                        builder.AppendLine($"D# Version: {client.DSharpClient.VersionString}");
                }
                builder.AppendLine($"Plugin Status: {plugin.GetStatus()}");
                builder.AppendLine($"Discord Client Status: {client.Status}");
                if (client.LastConnectionError != DiscordClient.ConnectionError.None)
                    builder.AppendLine($"Discord Client Error: {client.LastConnectionError}");

                TimeSpan elapssedTime = DateTime.Now.Subtract(plugin.InitTime);
                if (verbose)
                    builder.AppendLine($"Start Time: {plugin.InitTime:yyyy-MM-dd HH:mm}");
                builder.AppendLine($"Running Time: {(int)elapssedTime.TotalDays}:{elapssedTime.Hours}:{elapssedTime.Minutes}");

                if (client.ConnectionStatus != DiscordClient.ConnectionState.Connected)
                    return builder.ToString();

                if (verbose)
                    builder.AppendLine($"Connection Time: {client.LastConnectionTime:yyyy-MM-dd HH:mm}");

                builder.AppendLine();
                builder.AppendLine("--- User Data ---");
                builder.AppendLine($"Linked users: {DLStorage.PersistentData.LinkedUsers.Count}");
                builder.AppendLine($"Opt-in users: {DLStorage.PersistentData.OptedInUsers.Count}");
                builder.AppendLine($"Opt-out users: {DLStorage.PersistentData.OptedOutUsers.Count}");
                builder.AppendLine();
                builder.AppendLine("--- Modules ---");

                string moduleDisplayText = plugin.Modules.Select(module => module.GetDisplayText(string.Empty, verbose)).DoubleNewlineList();
                builder.AppendLine(moduleDisplayText);

                if (verbose)
                {
                    builder.AppendLine();
                    builder.AppendLine("--- Configuration ---");
                    builder.AppendLine($"Bot Name: {client.DSharpClient.CurrentUser.Username}");
                }
                return builder.ToString();
            }

            public static string GetConfigVerificationReport()
            {
                StringBuilder builder = new StringBuilder();
                if (DiscordLink.Obj.Client.ConnectionStatus != DiscordClient.ConnectionState.Connected)
                {
                    builder.AppendLine("[Discord Client not connected - Parts of the config was not possible to verify]");
                }

                DLConfigData config = DLConfig.Data;

                // Guild
                if (config.DiscordServerId == 0)
                {
                    builder.AppendLine("- Discord server ID not configured.");
                }

                // Bot Token
                if (string.IsNullOrWhiteSpace(config.BotToken))
                {
                    builder.AppendLine("- Bot token not configured. See Github page for install instructions.");
                }

                // Invite message
                if (!string.IsNullOrWhiteSpace(config.InviteMessage) && !config.InviteMessage.ContainsCaseInsensitive(DLConstants.INVITE_COMMAND_TOKEN))
                {
                    builder.AppendLine($"- Invite message does not contain the invite link token {DLConstants.INVITE_COMMAND_TOKEN}.");
                }

                if (DiscordLink.Obj.Client.ConnectionStatus == DiscordClient.ConnectionState.Connected)
                {
                    // Discord guild and channel information isn't available the first time this function is called
                    if (DiscordLink.Obj.Client.Guild != null && DLConfig.GetChannelLinks(verifiedLinksOnly: false).Count > 0)
                    {
                        foreach (ChannelLink link in DLConfig.GetChannelLinks(verifiedLinksOnly: false))
                        {
                            if (!link.IsValid())
                                builder.AppendLine($"- Channel Link verification failed for \"{link}\".");
                        }
                    }
                }

                if (builder.Length == 0)
                {
                    builder.AppendLine("Verification Successful!");
                }

                return builder.ToString().TrimEnd();
            }

            public static string GetPermissionsReport(PermissionReportComponentFlag flag)
            {
                if (flag == 0)
                    return "Permission Check Failed";

                DiscordClient client = DiscordLink.Obj.Client;
                StringBuilder builder = new StringBuilder();
                if (flag.HasFlag(PermissionReportComponentFlag.Intents))
                {
                    foreach (DiscordIntents intent in DLConstants.REQUESTED_INTENTS)
                    {
                        if (!client.BotHasIntent(intent))
                        {
                            builder.AppendLine($"- Missing Intent \"{Enum.GetName(intent)}\".");
                        }
                    }
                }

                if (flag.HasFlag(PermissionReportComponentFlag.ServerPermissions))
                {
                    foreach (Permissions permission in DLConstants.REQUESTED_GUILD_PERMISSIONS)
                    {
                        if (!client.BotHasPermission(permission))
                        {
                            builder.AppendLine($"- Missing Server Permission \"{Enum.GetName(permission)}\".");
                        }
                    }
                }

                if (flag.HasFlag(PermissionReportComponentFlag.ChannelPermissions))
                {
                    foreach (ChannelLink link in DLConfig.GetChannelLinks().GroupBy(link => link.Channel.Id).Select(group => group.First())) // Only perform the check once per link
                    {
                        foreach (Permissions permission in DLConstants.REQUESTED_CHANNEL_PERMISSIONS)
                        {
                            if (!client.ChannelHasPermission(link.Channel, permission))
                            {
                                builder.AppendLine($"- Missing Channel Permission \"{permission}\" in channel \"{link.Channel.Name}\".");
                                if (client.BotHasPermission(permission))
                                {
                                    builder.AppendLine($"  ^ This is only related to the Channel's settings inside your Discord Server and not the DiscordLink configuration.");
                                }
                            }
                        }
                    }
                }

                if (builder.Length == 0)
                {
                    builder.AppendLine("Verification Successful!");
                }

                return builder.ToString().TrimEnd();
            }

            public static string GetPermissionsReportForChannel(DiscordChannel channel)
            {
                StringBuilder builder = new StringBuilder();
                foreach (Permissions permission in DLConstants.REQUESTED_CHANNEL_PERMISSIONS)
                {
                    if (!DiscordLink.Obj.Client.ChannelHasPermission(channel, permission))
                    {
                        builder.AppendLine($"- Missing Channel Permission \"{permission}\".");
                    }
                }

                if (builder.Length == 0)
                {
                    builder.AppendLine("Permission Check Passed!");
                }

                return builder.ToString().TrimEnd();
            }

            public static string GetChannelLinkList()
            {
                StringBuilder builder = new StringBuilder();
                foreach (ChannelLink link in DLConfig.GetChannelLinks(verifiedLinksOnly: false))
                {
                    builder.Append(link.ToString());
                    if (!link.IsValid())
                        builder.Append(" (Unverified)");
                    builder.AppendLine();
                }

                if (builder.Length == 0)
                {
                    builder.AppendLine("No channel links found in configuration");
                }
                return builder.ToString().TrimEnd();
            }

            public static string GetPlayerCount()
            {
                return $"{Lookups.NumOnlinePlayers}/{Lookups.NumTotalPlayers}";
            }

            public static string GetOnlinePlayerList()
            {
                string playerList = string.Join("\n", Lookups.OnlineUsersAlphabetical.Select(user => MessageUtils.StripTags(user.Name)));
                if (string.IsNullOrEmpty(playerList))
                    playerList = "-- No players online --";

                return playerList;
            }

            public static void GetActiveElectionsList(out string electionList, out string settlementList, out string VoteAndtimeRemainingList)
            {
                electionList = string.Empty;
                settlementList = string.Empty;
                VoteAndtimeRemainingList = string.Empty;
                foreach (Election election in Lookups.ActiveElections.OrderByDescending(election => election.Settlement.CachedData.CultureRecursiveTotal))
                {
                    electionList += $"{MessageUtils.StripTags(election.Name)}\n";
                    settlementList += election.Settlement != null ? $"{MessageUtils.StripTags(election.Settlement.Name)}\n" : "None";
                    VoteAndtimeRemainingList += $"{election.TotalVotes} | {TimeFormatter.FormatSpan(election.TimeLeft)}\n";
                }
            }

            public static void GetActiveLawsList(out string lawList, out string settlementList, out string creatorList)
            {
                lawList = string.Empty;
                settlementList = string.Empty;
                creatorList = string.Empty;
                foreach (Law law in Lookups.ActiveLaws.OrderByDescending(law => law.Settlement.CachedData.CultureRecursiveTotal))
                {
                    lawList += $"{MessageUtils.StripTags(law.Name)}\n";
                    settlementList += law.Settlement != null ? $"{MessageUtils.StripTags(law.Settlement.Name)}\n" : "None";
                    creatorList += law.Creator != null ? $"{MessageUtils.StripTags(law.Creator.Name)}\n" : "Unknown\n";
                }
            }

            public static void GetActiveSettlementsList(out string settlementList, out string activeCitizenCountAndInfluenceList, out string leaderList)
            {
                settlementList = string.Empty;
                activeCitizenCountAndInfluenceList = string.Empty;
                leaderList = string.Empty;
                foreach (Settlement settlement in Lookups.ActiveSettlements.OrderByDescending(settlement => settlement.CachedData.CultureRecursiveTotal))
                {
                    settlementList += $"{MessageUtils.StripTags(settlement.Name)}\n";
                    activeCitizenCountAndInfluenceList += $"{settlement.Citizens.Count(user => user.IsActive)} | {settlement.CachedData.CultureRecursiveTotal.ToString("0")}\n";
                    leaderList += settlement.Leader != null && settlement.Leader.Occupied ? $"{MessageUtils.StripTags(settlement.Leader.UserSet.First().Name)}\n" : "None\n";
                }
            }

            public static string GetPlayerSessionTimeList()
            {
                return string.Join("\n", Lookups.OnlineUsersAlphabetical.Select(user => GetTimeDescription(user.GetSecondsSinceLogin(), TimespanStringComponent.Day | TimespanStringComponent.Hour | TimespanStringComponent.Minute, TimespanStringComponent.Hour | TimespanStringComponent.Minute)));
            }

            public static string GetPlayerExhaustionTimeList()
            {
                return string.Join("\n", Lookups.OnlineUsersAlphabetical.Select(user => (user.ExhaustionMonitor?.IsExhausted ?? false) ? "Exhausted" : GetTimeDescription(user.GetSecondsLeftUntilExhaustion(), TimespanStringComponent.Day | TimespanStringComponent.Hour | TimespanStringComponent.Minute, TimespanStringComponent.Hour | TimespanStringComponent.Minute)));
            }

            public enum TimespanStringComponent
            {
                None = 0,
                Day = 1 << 0,
                Hour = 1 << 1,
                Minute = 1 << 2,
                Second = 1 << 3,
            }

            public static string GetGameTimeStamp()
            {
                double seconds = Simulation.Time.WorldTime.Seconds;
                return $"{((int)TimeUtil.SecondsToHours(seconds) % 24).ToString("00")}" +
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

            public static string GetTimeDescription(double seconds, TimespanStringComponent includeComponents = (TimespanStringComponent)~0, TimespanStringComponent includeZeroValues = TimespanStringComponent.None, bool annotate = false)
            {
                StringBuilder builder = new StringBuilder();
                if ((includeComponents & TimespanStringComponent.Day) != 0)
                {
                    int daysCount = (int)TimeUtil.SecondsToDays(seconds);
                    if (((includeZeroValues & TimespanStringComponent.Day) != 0) || daysCount > 0)
                    {
                        if (annotate)
                            builder.Append(daysCount.ToString() + "D ");
                        else
                            builder.Append(daysCount.ToString("00"));
                    }
                }

                if ((includeComponents & TimespanStringComponent.Hour) != 0)
                {
                    int hoursCount = (int)TimeUtil.SecondsToHours(seconds) % 24;
                    if (((includeZeroValues & TimespanStringComponent.Hour) != 0) || hoursCount > 0)
                    {
                        if (!annotate && builder.Length != 0)
                            builder.Append(":");
                        if (annotate)
                            builder.Append(hoursCount.ToString() + "H ");
                        else
                            builder.Append(hoursCount.ToString("00"));
                    }
                }

                if ((includeComponents & TimespanStringComponent.Minute) != 0)
                {
                    int minutesCount = (int)TimeUtil.SecondsToMinutes(seconds) % 60;
                    if (((includeZeroValues & TimespanStringComponent.Minute) != 0) || minutesCount > 0)
                    {
                        if (!annotate && builder.Length != 0)
                            builder.Append(":");
                        if (annotate)
                            builder.Append(minutesCount.ToString() + "M ");
                        else
                            builder.Append(minutesCount.ToString("00"));
                    }
                }

                if ((includeComponents & TimespanStringComponent.Second) != 0)
                {
                    int secondsCount = (int)seconds % 60;
                    if (((includeZeroValues & TimespanStringComponent.Second) != 0) || secondsCount > 0)
                    {
                        if (!annotate && builder.Length != 0)
                            builder.Append(":");
                        if (annotate)
                            builder.Append(secondsCount.ToString() + "S ");
                        else
                            builder.Append(secondsCount.ToString("00"));
                    }
                }
                return builder.ToString().Trim();
            }
        }

        public static class Discord
        {
            public static string GetActivityString()
            {
                int onlinePlayers = Lookups.NumOnlinePlayers;
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
                    embed.WithTitle($"**{MessageUtils.FirstNonEmptyString(config.ServerName, MessageUtils.StripTags(serverInfo.Description), "[Server Title Missing]")} Server Status**");
                }
                else
                {
                    embed.WithTitle($"**Server Status**");
                }

                if (flag.HasFlag(ServerInfoComponentFlag.Description))
                {
                    embed.WithDescription(MessageUtils.FirstNonEmptyString(config.ServerDescription, MessageUtils.StripTags(serverInfo.Description), "No server description is available."));
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
                    string WebServerUrl = NetworkManager.Config.WebServerUrl;
                    string fieldText = !string.IsNullOrEmpty(WebServerUrl) ? WebServerUrl : "Webserver URL not configured";
                    embed.AddField("Webpage Address", fieldText);
                }

                if (flag.HasFlag(ServerInfoComponentFlag.PlayerCount) || flag.HasFlag(ServerInfoComponentFlag.ActiveSettlementCount))
                {
                    int fieldsAdded = 0;
                    if (flag.HasFlag(ServerInfoComponentFlag.PlayerCount))
                    {
                        embed.AddField("Player Count", $"{Lookups.NumOnlinePlayers} Online / {serverInfo.TotalPlayers} Total", inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.ActiveSettlementCount))
                    {
                        embed.AddField("Active Settlement Count", $"{Lookups.ActiveSettlements.Count()}", inline: true);
                        ++fieldsAdded;
                    }

                    for (int i = fieldsAdded; i < DLConstants.DISCORD_EMBED_FIELDS_PER_ROW_LIMIT; ++i)
                    {
                        embed.AddAlignmentField();
                    }
                }

                if(flag.HasFlag(ServerInfoComponentFlag.LawCount) || flag.HasFlag(ServerInfoComponentFlag.ActiveElectionCount))
                {
                    int fieldsAdded = 0;
                    if (flag.HasFlag(ServerInfoComponentFlag.LawCount))
                    {
                        embed.AddField("Law Count", $"{Lookups.ActiveLaws.Count()}", inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.ActiveElectionCount))
                    {
                        embed.AddField("Active Elections Count", $"{Lookups.ActiveElections.Count()}", inline: true);
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

                    if (flag.HasFlag(ServerInfoComponentFlag.MeteorTimeRemaining) && serverInfo.HasMeteor)
                    {
                        string meteorContent = DisasterPlugin.MeteorDestroyed
                            ? "Destroyed!"
                            : DateTime.Now.AddSeconds(serverInfo.TimeLeft).ToDiscordTimeStamp('R');
                        embed.AddField("Meteor", meteorContent, inline: true);
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

                if (FeatureConfig.Obj.ExhaustionTimeEnabled && (flag.HasFlag(ServerInfoComponentFlag.ExhaustionResetTimeLeft) || flag.HasFlag(ServerInfoComponentFlag.ExhaustedPlayerCount)))
                {
                    int fieldsAdded = 0;
                    if (flag.HasFlag(ServerInfoComponentFlag.ExhaustionResetTimeLeft))
                    {
                        embed.AddField("Exhaustion Reset", DateTime.Now.AddSeconds(ExhaustionPlugin.Obj.Config.TimeUntilRefresh).ToDiscordTimeStamp('R'), inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.ExhaustedPlayerCount))
                    {
                        embed.AddField("Exhausted Players Count", Lookups.NumExhaustedPlayers.ToString(), inline: true);
                        ++fieldsAdded;
                    }

                    for (int i = fieldsAdded; i < DLConstants.DISCORD_EMBED_FIELDS_PER_ROW_LIMIT; ++i)
                    {
                        embed.AddAlignmentField();
                    }
                }

                if (flag.HasFlag(ServerInfoComponentFlag.PlayerList))
                {
                    embed.AddField($"Online Players ({Lookups.NumOnlinePlayers})", Shared.GetOnlinePlayerList(), inline: true);
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

                    if (flag.HasFlag(ServerInfoComponentFlag.PlayerListExhaustionTime) && FeatureConfig.Obj.ExhaustionTimeEnabled)
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

                if(flag.HasFlag(ServerInfoComponentFlag.ActiveSettlementList))
                {
                    Shared.GetActiveSettlementsList(out string settlementList, out string activeCitizenCountList, out string leaderList);
                    if (!string.IsNullOrEmpty(settlementList))
                    {
                        embed.AddField("Active Settlements", settlementList, inline: true);
                        embed.AddField("Active Citizen & Influence", activeCitizenCountList, inline: true);
                        embed.AddField("Leader", leaderList, inline: true);
                    }
                    else
                    {
                        embed.AddField("Active Settlements", "-- No active settlements --", inline: true);
                        embed.AddAlignmentField();
                        embed.AddAlignmentField();
                    }
                }

                if (flag.HasFlag(ServerInfoComponentFlag.ActiveElectionList))
                {
                    Shared.GetActiveElectionsList(out string electionList, out string settlementList, out string votesAndTimeRemainingList);
                    if (!string.IsNullOrEmpty(electionList))
                    {
                        embed.AddField("Active Elections", electionList, inline: true);
                        embed.AddField("Settlement", settlementList, inline: true);
                        embed.AddField("Votes & Timer", votesAndTimeRemainingList, inline: true);
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
                    Shared.GetActiveLawsList(out string lawList, out string settlementList, out string creatorList);
                    if (!string.IsNullOrEmpty(lawList))
                    {
                        embed.AddField("Active Laws", lawList, inline: true);
                        embed.AddField("Settlement", settlementList, inline: true);
                        embed.AddField("Creator", creatorList, inline: true);
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
                        report.AddField("Session Time", Shared.GetTimeDescription(user.GetSecondsSinceLogin(), Shared.TimespanStringComponent.Day | Shared.TimespanStringComponent.Hour | Shared.TimespanStringComponent.Minute, Shared.TimespanStringComponent.Hour | Shared.TimespanStringComponent.Minute), inline: true);
                    else
                        report.AddField("Last Online", $"{Shared.GetTimeDescription(user.GetSecondsSinceLogout(), annotate: true)} ago", inline: true);
                    report.AddAlignmentField();
                }

                // Play time
                if (flag.HasFlag(PlayerReportComponentFlag.PlayTime))
                {
                    report.AddField("Playtime Total", Shared.GetTimeDescription(user.OnlineTimeLog.ActiveSeconds(0.0), annotate: true), inline: true);
                    report.AddField("Playtime last 24 hours", Shared.GetTimeDescription(user.OnlineTimeLog.ActiveSeconds(Constants.SECONDS_PER_DAY), Shared.TimespanStringComponent.Hour | Shared.TimespanStringComponent.Minute | Shared.TimespanStringComponent.Second, annotate: true), inline: true);
                    report.AddField("Playtime Last 7 days", Shared.GetTimeDescription(user.OnlineTimeLog.ActiveSeconds(Constants.SECONDS_PER_WEEK), annotate: true), inline: true);
                }

                // Exhaustion
                if (flag.HasFlag(PlayerReportComponentFlag.Exhaustion) && FeatureConfig.Obj.ExhaustionTimeEnabled)
                {
                    report.AddField("Exhaustion Countdown", (user.ExhaustionMonitor?.IsExhausted ?? false) ? "Exhausted" : Shared.GetTimeDescription(user.GetSecondsLeftUntilExhaustion(), includeZeroValues: Shared.TimespanStringComponent.Hour | Shared.TimespanStringComponent.Minute | Shared.TimespanStringComponent.Second), inline: true);
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
                    float reputation = ReputationManager.Obj.GetRep(user);
                    report.AddField("Reputation", reputation.ToString("N0"), inline: true);
                    report.AddField("Can Give Today", reputation.ToString("N0"), inline: true);
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

                    IEnumerable<Skill> orderedSkills = user.Skillset.Skills.Where(skill => skill.Level > 0).OrderByDescending(skill => skill.Level);
                    foreach (Skill skill in orderedSkills)
                    {
                        bool maxLevelReached = skill.Level >= skill.MaxLevel;
                        skillsDesc.AppendLine(skill.DisplayName);
                        levelsDesc.AppendLine(skill.Level.ToString() + (maxLevelReached ? " (Max)" : string.Empty));
                        percentOfNextLevelDoneDesc.AppendLine(maxLevelReached ? "N/A" : $"{(int)(skill.PercentTowardsNextLevel * 100)}%");
                    }

                    report.AddField("Skills", skillsDesc.ToString(), inline: true);
                    report.AddField("Level", levelsDesc.ToString(), inline: true);
                    report.AddField("Percent of Next Level", percentOfNextLevelDoneDesc.ToString(), inline: true);
                }

                // Demographics
                if (flag.HasFlag(PlayerReportComponentFlag.Demographics))
                {
                    StringBuilder demographicDesc = new StringBuilder();
                    IEnumerable<Demographic> userDemographics = Lookups.ActiveDemographics.Where(demographic => demographic.ContainsUser(user)).OrderBy(demographic => demographic.Name);
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
                    IEnumerable<Title> userTitles = Lookups.ActiveElectedTitles.Where(title => title.UserSet.Contains(user)).Cast<Title>().Concat(Lookups.ActiveAppointedTitles.Where(title => title.UserSet.Contains(user)).Cast<Title>()).OrderBy(title => title.Name);
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

                    IEnumerable<Deed> userDeeds = Lookups.Deeds.Where(deed => deed.ContainsOwner(user)).OrderByDescending(deed => deed.GetTotalPlotSize());
                    foreach (Deed deed in userDeeds)
                    {
                        propertiessDesc.AppendLine(deed.Name.TrimEndString(" Deed"));
                        propertiessSizeOrVehicleDesc.AppendLine(deed.IsVehicle() ? deed.GetVehicle().Parent.CreatingItem.DisplayName : $"{deed.GetTotalPlotSize()}m²");
                        propertiessLocationDesc.AppendLine(deed.IsVehicle() ? deed.GetVehicle().Parent.Position3i.ToString() : deed.CachedCenterPos == null ? deed.CachedCenterPos.ToString() : "Unknown");
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
                var currencyTradesMap = Moose.Plugin.MooseStorage.WorldData.CurrencyToTradeCountMap;

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
                    if (accountEnumerator.Current.Creator == null || currencyAmount == float.PositiveInfinity)
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
                string webServerURL = NetworkManager.Config.WebServerUrl;
                string fieldText = !string.IsNullOrEmpty(webServerURL) ? $"{webServerURL}/election/{election.Id}" : "Webserver URL not configured";
                report.AddField("URL", fieldText);

                // Settlement juristiction
                report.AddField("Settlement", election.Settlement.Name, inline: true);

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
                    foreach (UserRunoffVote vote in election.UserVotes.Values)
                    {
                        string topChoiceName = null;
                        ElectionChoiceID topChoiceId = vote.RankedVotes.FirstOrDefault();
                        foreach (ElectionChoice choice in election.Choices)
                        {
                            if (choice.ID == topChoiceId)
                            {
                                topChoiceName = choice.Name;
                                break;
                            }
                        }
                        voteDesc += $"{vote.Voter.Name}\n";
                        choiceDesc += $"{topChoiceName}\n";
                    }

                    foreach (TwitchVote vote in election.RawTwitchVotes.Values)
                    {
                        string topChoiceName = null;
                        ElectionChoiceID topChoiceId = vote.ChoiceID;
                        foreach (ElectionChoice choice in election.Choices)
                        {
                            if (choice.ID == topChoiceId)
                            {
                                topChoiceName = choice.Name;
                                break;
                            }
                        }
                        voteDesc += $"Twitch Vote\n";
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
                        boughtItemsDesc += $"{trade.NumberOfItems} X {trade.ItemUsed.DisplayName} * {RoundToAcceptedDigits(trade.CurrencyAmount / trade.NumberOfItems)} = {RoundToAcceptedDigits(trade.CurrencyAmount)}\n";
                        boughtTotal += trade.CurrencyAmount;
                    }
                    else if (trade.BoughtOrSold == BoughtOrSold.Selling)
                    {
                        soldItemsDesc += $"{trade.NumberOfItems} X {trade.ItemUsed.DisplayName} * {RoundToAcceptedDigits(trade.CurrencyAmount / trade.NumberOfItems)} = {RoundToAcceptedDigits(trade.CurrencyAmount)}\n";
                        soldTotal += trade.CurrencyAmount;
                    }
                }

                if (!boughtItemsDesc.IsEmpty())
                {
                    boughtItemsDesc += $"\nTotal = {boughtTotal.ToString("n2")}";
                    embed.AddField("Bought", boughtItemsDesc, allowAutoLineBreak: true, inline: true);
                }

                if (!soldItemsDesc.IsEmpty())
                {
                    soldItemsDesc += $"\nTotal = {soldTotal.ToString("n2")}";
                    embed.AddField("Sold", soldItemsDesc, allowAutoLineBreak: true, inline: true);
                }

                // Currency transfer description
                string resultDesc;
                float subTotal = RoundToAcceptedDigits(soldTotal) - RoundToAcceptedDigits(boughtTotal);

                if (Math.Abs(subTotal) <= 0.00f)
                {
                    resultDesc = "No currency was exchanged.";
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

                    Moose.Plugin.MooseStorage.WorldData.CurrencyToTradeCountMap.TryGetValue(o.Currency.Id, out int result);
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
    }
}
