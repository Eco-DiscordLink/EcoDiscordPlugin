using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DiscordLink.Extensions;
using DSharpPlus.Entities;
using Eco.Gameplay.Civics.Elections;
using Eco.Gameplay.Civics.Laws;
using Eco.Gameplay.Components;
using Eco.Gameplay.Players;
using Eco.Plugins.DiscordLink.Modules;
using Eco.Plugins.Networking;
using Eco.Shared.Networking;
using Eco.Shared.Utils;

using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;
using Eco.Shared;
using DSharpPlus;

namespace Eco.Plugins.DiscordLink.Utilities
{
    static class MessageBuilder
    {
        public enum ServerInfoComponentFlag
        {
            Name                = 1 << 0,
            Description         = 1 << 1,
            Logo                = 1 << 2,
            ConnectionInfo      = 1 << 3,
            PlayerCount         = 1 << 4,
            PlayerList          = 1 << 5,
            PlayerListLoginTime = 1 << 6,
            CurrentTime         = 1 << 7,
            TimeRemaining       = 1 << 8,
            MeteorHasHit        = 1 << 9,
            ActiveElectionCount = 1 << 10,
            ActiveElectionList  = 1 << 11,
            LawCount            = 1 << 12,
            LawList             = 1 << 13,
            All                 = ~0
        }

        private class StoreOffer
        {
            public StoreOffer(string title, string description, bool buying)
            {
                this.Title = title;
                this.Description = description;
                this.Buying = buying;
            }

            public string Title { get; private set; }
            public string Description { get; private set; }
            public bool Buying { get; private set; }
        }


        public static class Shared
        {
            public static string GetAboutMessage()
            {
                return $"This server is running the DiscordLink plugin version {DiscordLink.Obj.PluginVersion}." +
                    "\nIt connects the game server to a Discord bot in order to perform seamless communication between Eco and Discord." +
                    "\nThis enables you to chat with players who are currently not online in Eco, but are available on Discord." +
                    "\nDiscordLink can also be used to display information about the Eco server in Discord, such as who is online and what items are available on the market." +
                    "\n\nFor more information, visit \"www.github.com/Eco-DiscordLink/EcoDiscordPlugin\".";
            }

            public static string GetDisplayString(bool verbose)
            {
                DiscordLink plugin = DiscordLink.Obj;
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"DiscordLink {plugin.PluginVersion}");
                if (verbose)
                {
                    builder.AppendLine($"Server Name: {MessageUtil.FirstNonEmptyString(DLConfig.Data.ServerName, MessageUtil.StripTags(NetworkManager.GetServerInfo().Description), "[Server Title Missing]")}");
                    builder.AppendLine($"Server Version: {EcoVersion.VersionNumber}");
                    builder.AppendLine($"D# Version: {plugin.DiscordClient.VersionString}");
                }
                builder.AppendLine($"Status: {plugin.GetStatus()}");
                TimeSpan elapssedTime = DateTime.Now.Subtract(plugin.InitTime);
                builder.AppendLine($"Running Time: {(int)elapssedTime.TotalDays}:{elapssedTime.Hours}:{elapssedTime.Minutes}");

                if (!plugin.DiscordConnected)
                    return builder.ToString();

                if (verbose)
                {
                    builder.AppendLine($"Start Time: {plugin.InitTime:yyyy-MM-dd HH:mm}");
                    builder.AppendLine($"Connection Time: {plugin.LastConnectionTime:yyyy-MM-dd HH:mm}");
                }
                builder.AppendLine();
                builder.AppendLine("--- User Data ---");
                builder.AppendLine($"Linked users: {DLStorage.PersistentData.LinkedUsers.Count}");
                builder.AppendLine();
                builder.AppendLine("--- Modules ---");
                foreach (Module module in plugin.Modules)
                {
                    builder.Append(module.GetDisplayText(string.Empty, verbose));
                }

                if (verbose)
                {
                    builder.AppendLine("--- Status ---");
                    builder.AppendLine($"Start Time: {plugin.InitTime:yyyy-MM-dd HH:mm}");
                    builder.AppendLine($"Connection Time: {plugin.LastConnectionTime:yyyy-MM-dd HH:mm}");

                    builder.AppendLine();
                    builder.AppendLine("--- Config ---");
                    builder.AppendLine($"Name: {plugin.DiscordClient.CurrentUser.Username}");
                    builder.AppendLine($"Has GuildMembers Intent: {DiscordUtil.BotHasIntent(DiscordIntents.GuildMembers)}");

                    builder.AppendLine();
                    builder.AppendLine("--- Storage - Persistent ---");
                    builder.AppendLine("Linked User Data:");
                    foreach (LinkedUser linkedUser in DLStorage.PersistentData.LinkedUsers)
                    {
                        User ecoUser = UserManager.FindUserById(linkedUser.SteamID, linkedUser.SlgID);
                        string ecoUserName = (ecoUser != null) ? MessageUtil.StripTags(ecoUser.Name) : "[Uknown Eco User]";

                        DiscordUser discordUser = plugin.DiscordClient.GetUserAsync(ulong.Parse(linkedUser.DiscordID)).Result;
                        string discordUserName = (discordUser != null) ? discordUser.Username : "[Unknown Discord User]";

                        string verified = (linkedUser.Verified) ? "Verified" : "Unverified";
                        builder.AppendLine($"{ecoUserName} <--> {discordUserName} - {verified}");
                    }

                    builder.AppendLine();
                    builder.AppendLine("--- Storage - World ---");
                    builder.AppendLine("Tracked Trades:");
                    foreach (var trackedUserTrades in DLStorage.WorldData.PlayerTrackedTrades)
                    {
                        DiscordUser discordUser = plugin.DiscordClient.GetUserAsync(trackedUserTrades.Key).Result;
                        if (discordUser == null) continue;

                        builder.AppendLine($"[{discordUser.Username}]");
                        foreach (string trade in trackedUserTrades.Value)
                        {
                            builder.AppendLine($"- {trade}");
                        }
                    }

                    builder.AppendLine();
                    builder.AppendLine("Cached Guilds:");
                    foreach (DiscordGuild guild in plugin.DiscordClient.Guilds.Values)
                    {
                        builder.AppendLine($"- {guild.Name} ({guild.Id})");
                        builder.AppendLine("   Cached Channels");
                        foreach (DiscordChannel channel in guild.Channels.Values)
                        {
                            builder.AppendLine($"  - {channel.Name} ({channel.Id})");
                            builder.AppendLine($"      Permissions:");
                            builder.AppendLine($"          Read Messages:          {DiscordUtil.ChannelHasPermission(channel, Permissions.ReadMessageHistory)}");
                            builder.AppendLine($"          Send Messages:          {DiscordUtil.ChannelHasPermission(channel, Permissions.SendMessages)}");
                            builder.AppendLine($"          Manage Messages:        {DiscordUtil.ChannelHasPermission(channel, Permissions.ManageMessages)}");
                            builder.AppendLine($"          Embed Links:            {DiscordUtil.ChannelHasPermission(channel, Permissions.EmbedLinks)}");
                            builder.AppendLine($"          Mention Everyone/Here:  {DiscordUtil.ChannelHasPermission(channel, Permissions.MentionEveryone)}");
                        }
                    }
                }

                return builder.ToString();
            }

            public static string GetPlayerCount()
            {
                IEnumerable<User> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected);
                int numberTotal = NetworkManager.GetServerInfo().TotalPlayers;
                int numberOnline = onlineUsers.Count();
                return $"{numberOnline}/{numberTotal}";
            }

            public static string GetPlayerList()
            {
                string playerList = string.Empty;
                IEnumerable<User> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).OrderBy(user => user.Name);
                foreach (User player in onlineUsers)
                {
                    playerList += $"{player.Name}\n";
                }

                if (string.IsNullOrEmpty(playerList))
                    playerList = "-- No players online --";

                return playerList;
            }

            public static string GetPlayerSessionTimeList()
            {
                string playerSessionTimeList = string.Empty;
                IEnumerable<User> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).OrderBy(user => user.Name);
                foreach (User player in onlineUsers)
                {
                    playerSessionTimeList += $"{GetTimespan(Simulation.Time.WorldTime.Seconds - player.LoginTime, TimespanStringComponent.Hour | TimespanStringComponent.Minute)}\n";
                }
                return playerSessionTimeList;
            }

            public enum TimespanStringComponent
            {
                Day = 1 << 0,
                Hour = 1 << 1,
                Minute = 1 << 2,
                Second = 1 << 3,
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
        }

        public static class Discord
        {
            public static string EmbedToText(string textContent, DiscordLinkEmbed embedContent)
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

                    foreach (DiscordLinkEmbedField field in embedContent.Fields)
                    {
                        message += "**" + field.Title + "**\n" + field.Text + "\n\n";
                    }

                    if (!string.IsNullOrEmpty(embedContent.Footer))
                    {
                        message += embedContent.Footer;
                    }
                }
                return message.Trim();
            }

            public static DiscordLinkEmbed GetServerInfo(ServerInfoComponentFlag flag)
            {
                var plugin = DiscordLink.Obj;
                if (plugin == null) return null;

                DLConfigData config = DLConfig.Data;
                ServerInfo serverInfo = NetworkManager.GetServerInfo();

                DiscordLinkEmbed embed = new DiscordLinkEmbed();
                embed.WithFooter(GetStandardEmbedFooter());

                if (flag.HasFlag(ServerInfoComponentFlag.Name))
                {
                    embed.WithTitle($"**{MessageUtil.FirstNonEmptyString(config.ServerName, MessageUtil.StripTags(serverInfo.Description), "[Server Title Missing]")} " + "Server Status" + "**\n" + DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString());
                }
                else
                {
                    DateTime time = DateTime.Now;
                    int utcOffset = TimeZoneInfo.Local.GetUtcOffset(time).Hours;
                    embed.WithTitle("**" + "Server Status" + "**\n" + "[" + DateTime.Now.ToString("yyyy-MM-dd : HH:mm", CultureInfo.InvariantCulture) + " UTC " + (utcOffset != 0 ? (utcOffset >= 0 ? "+" : "-") + utcOffset : "") + "]");
                }

                if (flag.HasFlag(ServerInfoComponentFlag.Description))
                {
                    embed.WithDescription(MessageUtil.FirstNonEmptyString(config.ServerDescription, MessageUtil.StripTags(serverInfo.Description), "No server description is available."));
                }

                if (flag.HasFlag(ServerInfoComponentFlag.Logo) && !string.IsNullOrWhiteSpace(config.ServerLogo))
                {
                    embed.WithThumbnail(config.ServerLogo);
                }

                if (flag.HasFlag(ServerInfoComponentFlag.ConnectionInfo))
                {
                    string fieldText = "-- Connection info not configured --";
                    string address = string.Empty;
                    string port = string.Empty;
                    if (!string.IsNullOrEmpty(config.ServerAddress))
                    {
                        address = config.ServerAddress;
                    }
                    else if (!string.IsNullOrEmpty(serverInfo.Address))
                    {
                        address = serverInfo.Address;
                    }

                    if (!string.IsNullOrEmpty(address))
                    {
                        port = serverInfo.GamePort.ToString();
                        fieldText = $"{address}:{port}";
                    }

                    embed.AddField("Connection Info", fieldText);
                }

                if (flag.HasFlag(ServerInfoComponentFlag.PlayerCount) || flag.HasFlag(ServerInfoComponentFlag.LawCount) || flag.HasFlag(ServerInfoComponentFlag.ActiveElectionCount))
                {
                    int fieldsAdded = 0;
                    if (flag.HasFlag(ServerInfoComponentFlag.PlayerCount))
                    {
                        embed.AddField("Online Players Count", $"{UserManager.OnlineUsers.Where(user => user.Client.Connected).Count()}/{serverInfo.TotalPlayers}", inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.LawCount))
                    {
                        embed.AddField("Law Count", $"{EcoUtil.ActiveLaws.Count()}", inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.ActiveElectionCount))
                    {
                        embed.AddField("Active Elections Count", $"{EcoUtil.ActiveElections.Count()}", inline: true);
                        ++fieldsAdded;
                    }

                    for (int i = fieldsAdded; i < DLConstants.DISCORD_EMBED_FIELDS_PER_ROW_LIMIT; ++i)
                    {
                        embed.AddAlignmentField();
                    }
                }

                if (flag.HasFlag(ServerInfoComponentFlag.CurrentTime) || flag.HasFlag(ServerInfoComponentFlag.TimeRemaining) || flag.HasFlag(ServerInfoComponentFlag.MeteorHasHit))
                {
                    int fieldsAdded = 0;
                    if (flag.HasFlag(ServerInfoComponentFlag.CurrentTime))
                    {
                        TimeSpan timeSinceStartSpan = new TimeSpan(0, 0, (int)serverInfo.TimeSinceStart);
                        embed.AddField("Current Time", $"Day {timeSinceStartSpan.Days + 1} {timeSinceStartSpan.Hours.ToString("00")}:{timeSinceStartSpan.Minutes.ToString("00")}", inline: true); // +1 days to get start at day 1 just like ingame
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.TimeRemaining))
                    {
                        TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                        bool meteorHasHit = timeRemainingSpan.Seconds < 0;
                        timeRemainingSpan = meteorHasHit ? new TimeSpan(0, 0, 0) : timeRemainingSpan;
                        embed.AddField("Time Left Until Meteor", $"{timeRemainingSpan.Days} Days, {timeRemainingSpan.Hours} hours, {timeRemainingSpan.Minutes} minutes", inline: true);
                        ++fieldsAdded;
                    }

                    if (flag.HasFlag(ServerInfoComponentFlag.MeteorHasHit))
                    {
                        TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                        embed.AddField("Meteor Has Hit", timeRemainingSpan.Seconds < 0 ? "Yes" : "No", inline: true);
                        ++fieldsAdded;
                    }

                    for (int i = fieldsAdded; i < DLConstants.DISCORD_EMBED_FIELDS_PER_ROW_LIMIT; ++i)
                    {
                        embed.AddAlignmentField();
                    }
                }

                if (flag.HasFlag(ServerInfoComponentFlag.PlayerList))
                {
                    IEnumerable<string> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).Select(user => user.Name);
                    string playerCount = $"{UserManager.OnlineUsers.Where(user => user.Client.Connected).Count()}/{serverInfo.TotalPlayers}";
                    string playerList = onlineUsers.Count() > 0 ? string.Join("\n", onlineUsers) : "-- No players online --";
                    embed.AddField($"Online Players ({playerCount})", Shared.GetPlayerList(), inline: true);
                    if(flag.HasFlag(ServerInfoComponentFlag.PlayerListLoginTime))
                    {
                        string sessionTimeList = Shared.GetPlayerSessionTimeList();
                        if (!string.IsNullOrWhiteSpace(sessionTimeList))
                            embed.AddField("Session Time", sessionTimeList, inline: true);
                        else
                            embed.AddAlignmentField();
                        embed.AddAlignmentField();
                    }
                }

                if (flag.HasFlag(ServerInfoComponentFlag.ActiveElectionList))
                {
                    string electionList = string.Empty;
                    string votesList = string.Empty;
                    string timeRemainingList = string.Empty;
                    foreach (Election election in EcoUtil.ActiveElections)
                    {
                        electionList += $"{MessageUtil.StripTags(election.Name)}\n";
                        votesList += $"{election.TotalVotes} Votes";

                        TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                        timeRemainingList += $"{timeRemainingSpan.Days} Days, {timeRemainingSpan.Hours} hours, {timeRemainingSpan.Minutes} minutes";
                    }

                    if (string.IsNullOrEmpty(electionList))
                    {
                        embed.AddField("Active Elections", electionList, inline: true);
                        embed.AddField("Votes", votesList, inline: true);
                        embed.AddField("Time Remaining", timeRemainingList, inline: true);
                    }
                    else
                    {
                        embed.AddField("Active Elections", "--- No active elections ---", inline: true);
                        embed.AddAlignmentField();
                        embed.AddAlignmentField();
                    }
                }

                if (flag.HasFlag(ServerInfoComponentFlag.LawList))
                {
                    string lawList = string.Empty;
                    string creatorList = string.Empty;
                    foreach (Law law in EcoUtil.ActiveLaws)
                    {
                        lawList += $"{MessageUtil.StripTags(law.Name)}\n";
                        creatorList += $"{MessageUtil.StripTags(law.Creator.Name)}";
                    }
                
                    if (!string.IsNullOrEmpty(lawList))
                    {
                        embed.AddField("Active Laws", lawList, inline: true);
                        embed.AddField("Creator", creatorList, inline: true);
                        embed.AddAlignmentField();
                    }
                    else
                    {
                        embed.AddField("Active Laws", "--- No active laws ---", inline: true);
                        embed.AddAlignmentField();
                        embed.AddAlignmentField();
                    }
                }

                return embed;
            }

            public static DiscordLinkEmbed GetVerificationDM(User ecoUser)
            {
                DLConfigData config = DLConfig.Data;
                ServerInfo serverInfo = NetworkManager.GetServerInfo();
                string serverName = MessageUtil.StripTags(!string.IsNullOrWhiteSpace(config.ServerName) ? DLConfig.Data.ServerName : MessageUtil.StripTags(serverInfo.Description));

                DiscordLinkEmbed embed = new DiscordLinkEmbed();
                embed.WithTitle("Account Linking Verification");
                embed.AddField("Initiator", MessageUtil.StripTags(ecoUser.Name));
                embed.AddField("Description", $"Your Eco account has been linked to your Discord account on the server \"{serverName}\".");
                embed.AddField("Action Required", $"If you initiated this action, use the command `{config.DiscordCommandPrefix}verifylink` to verify that these accounts should be linked.");
                embed.WithFooter("If you did not initiate this action, notify a server admin.\nThe account link cannot be used until verified.");
                return embed;
            }

            public static string GetStandardEmbedFooter()
            {
                string serverName = MessageUtil.FirstNonEmptyString(DLConfig.Data.ServerName, MessageUtil.StripTags(NetworkManager.GetServerInfo().Description), "[Server Title Missing]");
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                return $"Message sent by DiscordLink @ {serverName} [{timestamp}]";
            }

            public static void FormatTrades(string matchedName, TradeTargetType tradeType, StoreOfferList groupedBuyOffers, StoreOfferList groupedSellOffers, out DiscordLinkEmbed embedContent)
            {
                // Format message
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle($"Trade offers for {matchedName}");

                if (groupedSellOffers.Count() > 0 || groupedBuyOffers.Count() > 0)
                {
                    Func<Tuple<StoreComponent, TradeOffer>, string> getLabel;
                    switch(tradeType)
                    {
                        case TradeTargetType.Tag:
                            getLabel = t => $"{t.Item2.Stack.Item.DisplayName} @ *{MessageUtil.StripTags(t.Item1.Parent.Name)}*";
                            break;

                        case TradeTargetType.Item:
                            getLabel = t => $"@ *{MessageUtil.StripTags(t.Item1.Parent.Name)}*";
                            break;

                        case TradeTargetType.User:
                            getLabel = t => t.Item2.Stack.Item.DisplayName;
                            break;

                        default:
                            getLabel = t => string.Empty;
                            break;
                    }
                    ICollection<StoreOffer> Offers = TradeOffersToFields(groupedBuyOffers, groupedSellOffers, getLabel);

                    for(int i = 0; i < Offers.Count; ++i)
                    {
                        StoreOffer currentOffer = Offers.ElementAt(i);
                        StoreOffer nextOffer = null;
                        if (i + 1 < Offers.Count)
                            nextOffer = Offers.ElementAt(i + 1);

                        embed.AddField(currentOffer.Title, currentOffer.Description, inline: true);

                        if (currentOffer.Buying && nextOffer != null)
                        {
                            if (nextOffer.Buying)
                            {
                                embed.AddAlignmentField();
                                embed.AddAlignmentField();
                            }
                            else
                            {
                                embed.AddAlignmentField();
                            }
                        }
                    }
                    embed.WithFooter(GetStandardEmbedFooter());
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

                    buyOffers.Add(new StoreOffer($"**Buying for {group.Key}**", fieldBodyBuilder.ToString(), buying: true));
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
                    buyOffers.Add(new StoreOffer($"**Selling for {group.Key}**", fieldBodyBuilder.ToString(), buying: false));
                }

                List<StoreOffer> allOffers = new List<StoreOffer>();
                int totalOffers = buyOffers.Count + sellOffers.Count;
                int index = 0;
                while (allOffers.Count < totalOffers)
                {
                    if (buyOffers.Count > index)
                        allOffers.Add(buyOffers[index]);

                    if (sellOffers.Count > index)
                        allOffers.Add(sellOffers[index]);

                    ++index;
                }

                return allOffers;
            }

            private static IEnumerable<string> TradeOffersToDescriptions<T>(IEnumerable<T> offers, Func<T, string> getPrice, Func<T, string> getLabel, Func<T, int?> getQuantity)
            {
                return offers.Select(t =>
                {
                    var price = getPrice(t);
                    var quantity = getQuantity(t);
                    var quantityString = quantity.HasValue ? $"{quantity.Value} - " : "";
                    var line = $"{quantityString}${price} {getLabel(t)}";
                    if (quantity == 0) line = $"~~{line}~~";
                    return line;
                });
            }
        }

        public static class Eco
        {
            public static void FormatTrades(TradeTargetType tradeType, StoreOfferList groupedBuyOffers, StoreOfferList groupedSellOffers, out string message)
            {
                Func<Tuple<StoreComponent, TradeOffer>, string> getLabel;
                
                switch(tradeType)
                {
                    case TradeTargetType.Tag:
                        getLabel = t => $"{t.Item2.Stack.Item.MarkedUpName} @ {t.Item1.Parent.MarkedUpName}";
                        break;

                    case TradeTargetType.Item:
                        getLabel = t => $"@ {t.Item1.Parent.MarkedUpName}";
                        break;

                    case TradeTargetType.User:
                        getLabel = t => t.Item2.Stack.Item.MarkedUpName;
                        break;

                    default:
                        getLabel = t => string.Empty;
                        break;
                }

                // Format message
                StringBuilder builder = new StringBuilder();

                if (groupedSellOffers.Count() > 0 || groupedBuyOffers.Count() > 0)
                {
                    foreach (var group in groupedBuyOffers)
                    {
                        var offerDescriptions = TradeOffersToDescriptions(group,
                            t => t.Item2.Price.ToString(),
                            t => getLabel(t),
                            t => t.Item2.Stack.Quantity);

                        builder.AppendLine(Text.Bold(Text.Color(Color.Green, $"<--- Buying for {group.First().Item1.CurrencyName} --->")));
                        foreach (string description in offerDescriptions)
                        {
                            builder.AppendLine(description);
                        }
                        builder.AppendLine();
                    }

                    foreach (var group in groupedSellOffers)
                    {
                        var offerDescriptions = TradeOffersToDescriptions(group,
                            t => t.Item2.Price.ToString(),
                            t => getLabel(t),
                            t => t.Item2.Stack.Quantity);

                        builder.AppendLine(Text.Bold(Text.Color(Color.Red, $"<--- Selling for {MessageUtil.StripTags(group.First().Item1.CurrencyName)} --->")));
                        foreach (string description in offerDescriptions)
                        {
                            builder.AppendLine(description);
                        }
                        builder.AppendLine();
                    }
                }
                else
                {
                    builder.AppendLine("--- No trade offers available ---");
                }
                message = builder.ToString();
            }

            private static IEnumerable<string> TradeOffersToDescriptions<T>(IEnumerable<T> offers, Func<T, string> getPrice, Func<T, string> getLabel, Func<T, int?> getQuantity)
            {
                return offers.Select(t =>
                {
                    var price = getPrice(t);
                    var quantity = getQuantity(t);
                    var quantityString = quantity.HasValue ? $"{quantity.Value} - " : "";
                    var line = $"{quantityString}${price} {getLabel(t)}";
                    if (quantity == 0) line = Text.Color(Color.Yellow, line);
                    return line;
                });
            }
        }
    }
}
