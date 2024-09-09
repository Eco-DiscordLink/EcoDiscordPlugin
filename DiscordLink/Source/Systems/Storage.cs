using DSharpPlus.Entities;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Persistance;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Modules;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public sealed class DLStorage
    {
        private const string PERSISANT_STORAGE_FILE_NAME = "PersistentData.json";
        private const string WORLD_STORAGE_FILE_NAME = "WorldData.json";

        public static readonly DLStorage Instance = new DLStorage();
        public static PersistentStorageData PersistentData { get; private set; } = new PersistentStorageData();
        public static WorldStorageData WorldData { get; private set; } = new WorldStorageData();

        public Dictionary<string, string> Snippets = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public delegate Task OnWatchedTradeChangedDelegate(object sender, EventArgs e, TradeWatcherEntry watcher);
        public static event OnWatchedTradeChangedDelegate TradeWatcherAdded;
        public static event OnWatchedTradeChangedDelegate TradeWatcherRemoved;

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static DLStorage()
        {
            PersistentData = new PersistentStorageData();
        }

        private DLStorage()
        {
        }

        public void Initialize()
        {
            Read();
            UserLinkManager.OnLinkedUserRemoved += HandleLinkedUserRemoved;
        }

        public void Shutdown()
        {
            UserLinkManager.OnLinkedUserRemoved -= HandleLinkedUserRemoved;
            Write();
        }

        public void ResetPersistentData()
        {
            PersistentData = new PersistentStorageData();
            Write(); // Make sure we don't read old data in case of an ungraceful shutdown
        }

        public void ResetWorldData()
        {
            WorldData = new WorldStorageData();
            Write(); // Make sure we don't read old data in case of an ungraceful shutdown
        }

        public void Write()
        {
            PersistentStorageData persistentData = PersistentData;
            if (Persistance.WriteJsonToFile<PersistentStorageData>(PersistentData, DLConstants.STORAGE_PATH_ABS, PERSISANT_STORAGE_FILE_NAME))
                PersistentData = persistentData;

            WorldStorageData worldData = WorldData;
            if (Persistance.WriteJsonToFile<WorldStorageData>(WorldData, DLConstants.STORAGE_PATH_ABS, WORLD_STORAGE_FILE_NAME))
                WorldData = worldData;
        }

        public void Read()
        {
            PersistentStorageData persistentData = PersistentData;
            if (Persistance.ReadJsonFromFile<PersistentStorageData>(DLConstants.STORAGE_PATH_ABS, PERSISANT_STORAGE_FILE_NAME, ref persistentData))
                PersistentData = persistentData;

            WorldStorageData worldData = WorldData;
            if (Persistance.ReadJsonFromFile<WorldStorageData>(DLConstants.STORAGE_PATH_ABS, WORLD_STORAGE_FILE_NAME, ref worldData))
                WorldData = worldData;
        }

        public void HandleEvent(DlEventType eventType, params object[] data)
        {
            switch (eventType)
            {
                case DlEventType.WorldReset:
                    Logger.Info("New world generated - Removing storage data for previous world");
                    ResetWorldData();
                    break;

                default:
                    break;
            }
        }

        private void HandleLinkedUserRemoved(object sender, LinkedUser user)
        {
            WorldData.TradeWatchers.Remove(ulong.Parse(user.DiscordId));
        }

        public class PersistentStorageData
        {
            public LayerDiscriminator LayerDiscriminator = new LayerDiscriminator();
            public List<LinkedUser> LinkedUsers = new List<LinkedUser>();
            public List<ulong> RoleIds = new List<ulong>();
            public List<EcoUser> OptedInUsers = new List<EcoUser>();
            public List<EcoUser> OptedOutUsers = new List<EcoUser>();

            public async Task<DiscordLinkEmbed> GetDataDescription()
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed();
                embed.WithTitle("Persistent data");
                embed.WithDescription("Persistent storage data is kept when a new world is created.");

                // Userlinks
                {
                    StringBuilder ecoBuilder = new StringBuilder();
                    StringBuilder discordBuilder = new StringBuilder();
                    StringBuilder verifiedBuilder = new StringBuilder();
                    foreach (LinkedUser linkedUser in PersistentData.LinkedUsers.Where(link => link.Verified))
                    {
                        ecoBuilder.AppendLine(linkedUser.EcoUser != null ? linkedUser.EcoUser.Name : linkedUser.StrangeId);
                        discordBuilder.AppendLine(linkedUser.DiscordMember != null ? linkedUser.DiscordMember.DisplayName : linkedUser.DiscordId.ToString());
                        verifiedBuilder.AppendLine(linkedUser.Verified ? "True" : "False");
                    }
                    embed.AddField("Eco Links", ecoBuilder.ToString(), inline: true);
                    embed.AddField("Discord Links", discordBuilder.ToString(), inline: true);
                    embed.AddField("Link Verified", verifiedBuilder.ToString(), inline: true);
                }

                // Opt in/out
                {
                    embed.AddField("Optout Users", string.Join("\n", PersistentData.OptedOutUsers.Where(user => user.GetUser != null).Select(user => user.GetUser.Name)), inline: true);
                    embed.AddField("Optin Users", string.Join("\n", PersistentData.OptedInUsers.Where(user => user.GetUser != null).Select(user => user.GetUser.Name)), inline: true);
                    embed.AddAlignmentField();
                }

                // Roles
                {
                    StringBuilder nameBuilder = new StringBuilder();
                    StringBuilder idBuilder = new StringBuilder();
                    StringBuilder permissionBuilder = new StringBuilder();
                    foreach (ulong id in PersistentData.RoleIds)
                    {
                        DiscordRole role = DiscordLink.Obj.Client.GetRoleById(id);
                        nameBuilder.AppendLine(role != null ? role.Name : "Uknown");
                        idBuilder.AppendLine(id.ToString());
                        permissionBuilder.AppendLine(role != null ? role.Permissions.ToString() : "Uknown");
                    }
                    embed.AddField("Role Names", nameBuilder.ToString(), inline: true);
                    embed.AddField("Role ID", idBuilder.ToString(), inline: true);
                    embed.AddField("Role Permissions", permissionBuilder.ToString(), inline: true);
                }

                // Layer discriminator
                {
                    embed.AddField("Layer Discriminator", LayerDiscriminator.Value.ToString(), inline: true);
                    if (LayerDiscriminator.LastIncrementTime > DateTime.MinValue)
                        embed.AddField("Last Updated", LayerDiscriminator.LastIncrementTime.ToDiscordTimeStamp('R'), inline: true);
                    else
                        embed.AddAlignmentField();
                    embed.AddAlignmentField();
                }

                return embed;
            }
        }

        public class WorldStorageData
        {
            public Dictionary<ulong, List<TradeWatcherEntry>> TradeWatchers = new Dictionary<ulong, List<TradeWatcherEntry>>();

            public IEnumerable<KeyValuePair<ulong, List<TradeWatcherEntry>>> DisplayTradeWatchers => TradeWatchers.Where(userAndWatchers => userAndWatchers.Value.Any(watcher => watcher.Type == ModuleArchetype.Display));
            public IEnumerable<KeyValuePair<ulong, List<TradeWatcherEntry>>> FeedTradeWatchers => TradeWatchers.Where(userAndWatchers => userAndWatchers.Value.Any(watcher => watcher.Type == ModuleArchetype.Feed));

            public int TradeWatcherCountTotal => TradeWatchers.Values.Sum(watchers => watchers.Count);
            public int TradeWatcherDisplayCountTotal => TradeWatchers.Values.SelectMany(watchers => watchers).Where(watcher => watcher.Type == ModuleArchetype.Display).Count();
            public int TradeWatcherFeedCountTotal => TradeWatchers.Values.SelectMany(watchers => watchers).Where(watcher => watcher.Type == ModuleArchetype.Feed).Count();

            public async Task<bool> AddTradeWatcher(ulong discordMemberId, TradeWatcherEntry watcherEntry)
            {
                if (!TradeWatchers.ContainsKey(discordMemberId))
                    TradeWatchers.Add(discordMemberId, new List<TradeWatcherEntry>());

                if (TradeWatchers[discordMemberId].Contains(watcherEntry))
                    return false;

                TradeWatchers[discordMemberId].Add(watcherEntry);
                await TradeWatcherAdded?.Invoke(this, EventArgs.Empty, watcherEntry);

                return true;
            }

            public async Task<bool> RemoveTradeWatcher(ulong discordMemberId, TradeWatcherEntry watcherEntry)
            {
                if (!TradeWatchers.ContainsKey(discordMemberId))
                    return false;

                List<TradeWatcherEntry> watcherList = TradeWatchers[discordMemberId];
                int toRemoveIndex = watcherList.FindIndex(w => w.Equals(watcherEntry));
                if (toRemoveIndex == -1)
                    return false;

                TradeWatcherEntry entry = watcherList[toRemoveIndex];
                watcherList.RemoveAt(toRemoveIndex);
                await TradeWatcherRemoved?.Invoke(this, EventArgs.Empty, entry);

                // Remove the user entry if the last watcher was remvoed
                if (watcherList.Count <= 0)
                    TradeWatchers.Remove(discordMemberId);

                return true;
            }

            public void RemoveAllTradeWatchersForMember(ulong discordMemberId)
            {
                if (!TradeWatchers.ContainsKey(discordMemberId))
                    return;

                TradeWatchers.Remove(discordMemberId);
            }

            public int GetTradeWatcherCountForMember(ulong discordMemberId)
            {
                if (!TradeWatchers.ContainsKey(discordMemberId))
                    return 0;

                return TradeWatchers[discordMemberId].Count;
            }

            public string ListTradeWatchers(ulong discordMemberId)
            {
                if (!TradeWatchers.ContainsKey(discordMemberId) || TradeWatchers[discordMemberId].Count <= 0)
                    return "No trade watchers exist for this user";

                StringBuilder builder = new StringBuilder();
                builder.Append("Your trade watchers are:\n");
                foreach (TradeWatcherEntry tradeWatcher in TradeWatchers[discordMemberId])
                {
                    builder.AppendLine($"- {tradeWatcher}");
                }
                return builder.ToString();
            }

            public async Task<DiscordLinkEmbed> GetDataDescription()
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed();
                embed.WithTitle("World data");
                embed.WithDescription("World storage data is reset when a new world is created.");

                // Trade watcher counts
                {
                    embed.AddField("Trade Watchers Total", WorldData.TradeWatcherCountTotal.ToString(), inline: true);
                    embed.AddField("Display Watchers", WorldData.TradeWatcherDisplayCountTotal.ToString(), inline: true);
                    embed.AddField("Feed Watchers", WorldData.TradeWatcherFeedCountTotal.ToString(), inline: true);
                }

                // Watchers
                {
                    StringBuilder userBuilder = new StringBuilder();
                    StringBuilder watchBuilder = new StringBuilder();
                    StringBuilder typeBuilder = new StringBuilder();
                    foreach (var memberIdAndWatch in TradeWatchers)
                    {
                        DiscordMember member = await DiscordLink.Obj.Client.GetMemberAsync(memberIdAndWatch.Key.ToString());
                        foreach (var watch in memberIdAndWatch.Value)
                        {
                            userBuilder.AppendLine(member != null ? member.DisplayName : memberIdAndWatch.Key.ToString());
                            watchBuilder.AppendLine(watch.Key);
                            typeBuilder.AppendLine(Enum.GetName(watch.Type));
                        }
                    }
                    embed.AddField("Owner", userBuilder.ToString(), inline: true);
                    embed.AddField("Watch", watchBuilder.ToString(), inline: true);
                    embed.AddField("Type", typeBuilder.ToString(), inline: true);
                }

                return embed;
            }
        }
    }

    public class TradeWatcherEntry
    {
        public TradeWatcherEntry(string key, ModuleArchetype type)
        {
            Key = key;
            Type = type;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            TradeWatcherEntry rhs = (TradeWatcherEntry)obj;
            return Key.EqualsCaseInsensitive(rhs.Key) && Type == rhs.Type;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode() ^ Type.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Key} ({Type})";
        }

        public string Key { get; private set; }
        public ModuleArchetype Type { get; private set; }
    }

    public class LayerDiscriminator
    {
        public long GetDiscriminator()
        {
            DateTime currentTime = DateTime.UtcNow;
            if ((currentTime - LastIncrementTime).TotalHours > 1) // Once an hour as that is the update rate of layers
            {
                Value++;
                LastIncrementTime = currentTime;
                DLStorage.Instance.Write();
            }

            return Value;
        }

        public long Value { get; set; } = 0;

        public DateTime LastIncrementTime { get; set; } = DateTime.MinValue;
    }
}
