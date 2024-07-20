﻿using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Persistance;
using Eco.Plugins.DiscordLink.Events;
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
        private const string PERSISANT_STORAGE_FILE_NAME = "DLPersistentData.json";
        private const string WORLD_STORAGE_FILE_NAME = "DLWorldData.json";

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

        public void HandleEvent(DLEventType eventType, params object[] data)
        {
            switch (eventType)
            {
                case DLEventType.WorldReset:
                    Logger.Info("New world generated - Removing storage data for previous world");
                    ResetWorldData();
                    break;

                default:
                    break;
            }
        }

        private void HandleLinkedUserRemoved(object sender, LinkedUser user)
        {
            WorldData.TradeWatchers.Remove(ulong.Parse(user.DiscordID));
        }

        public class PersistentStorageData
        {
            public List<LinkedUser> LinkedUsers = new List<LinkedUser>();
            public List<ulong> RoleIDs = new List<ulong>();
            public List<EcoUser> OptedInUsers = new List<EcoUser>();
            public List<EcoUser> OptedOutUsers = new List<EcoUser>();
        }

        public class WorldStorageData
        {
            public Dictionary<ulong, List<TradeWatcherEntry>> TradeWatchers = new Dictionary<ulong, List<TradeWatcherEntry>>();

            public IEnumerable<KeyValuePair<ulong, List<TradeWatcherEntry>>> DisplayTradeWatchers => TradeWatchers.Where(userAndWatchers => userAndWatchers.Value.Any(watcher => watcher.Type == ModuleArchetype.Display));
            public IEnumerable<KeyValuePair<ulong, List<TradeWatcherEntry>>> FeedTradeWatchers => TradeWatchers.Where(userAndWatchers => userAndWatchers.Value.Any(watcher => watcher.Type == ModuleArchetype.Feed));

            public int TradeWatcherCountTotal => TradeWatchers.Values.Sum(watchers => watchers.Count);
            public int TradeWatcherDisplayCountTotal => TradeWatchers.Values.SelectMany(watchers => watchers).Where(watcher => watcher.Type == ModuleArchetype.Display).Count();
            public int TradeWatcherFeedCountTotal => TradeWatchers.Values.SelectMany(watchers => watchers).Where(watcher => watcher.Type == ModuleArchetype.Feed).Count();

            public async Task<bool> AddTradeWatcher(ulong discordUserId, TradeWatcherEntry watcherEntry)
            {
                if (!TradeWatchers.ContainsKey(discordUserId))
                    TradeWatchers.Add(discordUserId, new List<TradeWatcherEntry>());

                if (TradeWatchers[discordUserId].Contains(watcherEntry))
                    return false;

                TradeWatchers[discordUserId].Add(watcherEntry);
                await TradeWatcherAdded?.Invoke(this, EventArgs.Empty, watcherEntry);

                return true;
            }

            public async Task<bool> RemoveTradeWatcher(ulong discordUserId, TradeWatcherEntry watcherEntry)
            {
                if (!TradeWatchers.ContainsKey(discordUserId))
                    return false;

                List<TradeWatcherEntry> watcherList = TradeWatchers[discordUserId];
                int toRemoveIndex = watcherList.FindIndex(w => w.Equals(watcherEntry));
                if (toRemoveIndex == -1)
                    return false;

                TradeWatcherEntry entry = watcherList[toRemoveIndex];
                watcherList.RemoveAt(toRemoveIndex);
                await TradeWatcherRemoved?.Invoke(this, EventArgs.Empty, entry);

                // Remove the user entry if the last watcher was remvoed
                if (watcherList.Count <= 0)
                    TradeWatchers.Remove(discordUserId);

                return true;
            }

            public void RemoveAllTradeWatchersForUser(ulong discordUserId)
            {
                if (!TradeWatchers.ContainsKey(discordUserId))
                    return;

                TradeWatchers.Remove(discordUserId);
            }

            public int GetTradeWatcherCountForUser(ulong discordUserID)
            {
                if (!TradeWatchers.ContainsKey(discordUserID))
                    return 0;

                return TradeWatchers[discordUserID].Count;
            }

            public string ListTradeWatchers(ulong discordUserID)
            {
                if (!TradeWatchers.ContainsKey(discordUserID) || TradeWatchers[discordUserID].Count <= 0)
                    return "No trade watchers exist for this user";

                StringBuilder builder = new StringBuilder();
                builder.Append("Your trade watchers are:\n");
                foreach (TradeWatcherEntry tradeWatcher in TradeWatchers[discordUserID])
                {
                    builder.AppendLine($"- {tradeWatcher}");
                }
                return builder.ToString();
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
}
