using Eco.EM.Framework.FileManager;
using Eco.Gameplay.GameActions;
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
        private const string PERSISANT_STORAGE_FILE_NAME = "DLPersistentData";
        private const string WORLD_STORAGE_FILE_NAME = "DLWorldData";

        public static readonly DLStorage Instance = new DLStorage();
        public static PersistentStorageData PersistentData { get; private set; } = new PersistentStorageData();
        public static WorldStorageData WorldData { get; private set; } = new WorldStorageData();

        public Dictionary<string, string> Snippets = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public delegate Task OnWatchedTradeChangedDelegate(object sender, EventArgs e, PersonalTradeWatcherEntry watcher);
        public static event OnWatchedTradeChangedDelegate PersonalTradeWatcherAdded;
        public static event OnWatchedTradeChangedDelegate PersonalTradeWatcherRemoved;

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
            FileManager<PersistentStorageData>.WriteTypeHandledToFile(PersistentData, DLConstants.StoragePathAbs, PERSISANT_STORAGE_FILE_NAME);
            FileManager<WorldStorageData>.WriteTypeHandledToFile(WorldData, DLConstants.StoragePathAbs, WORLD_STORAGE_FILE_NAME);
        }

        public void Read()
        {
            PersistentData = FileManager<PersistentStorageData>.ReadTypeHandledFromFile(DLConstants.StoragePathAbs, PERSISANT_STORAGE_FILE_NAME);
            WorldData = FileManager<WorldStorageData>.ReadTypeHandledFromFile(DLConstants.StoragePathAbs, WORLD_STORAGE_FILE_NAME);
        }

        public void HandleEvent(DLEventType eventType, params object[] data)
        {
            switch (eventType)
            {
                // Keep track of the amount of trades per currency
                case DLEventType.AccumulatedTrade:
                    if (!(data[0] is IEnumerable<List<CurrencyTrade>> accumulatedTrade))
                        return;

                    foreach (var list in accumulatedTrade)
                    {
                        if (list.Count <= 0) continue;

                        // Make sure an entry exists for the currency
                        int currencyID = accumulatedTrade.First()[0].Currency.Id;
                        if (!WorldData.CurrencyToTradeCountMap.ContainsKey(currencyID))
                            WorldData.CurrencyToTradeCountMap.Add(currencyID, 0);

                        WorldData.CurrencyToTradeCountMap.TryGetValue(currencyID, out int tradeCount);
                        WorldData.CurrencyToTradeCountMap[currencyID] = tradeCount + 1;
                    }
                    break;

                default:
                    break;
            }
        }

        private void HandleLinkedUserRemoved(object sender, LinkedUser user)
        {
            WorldData.PersonalTradeWatchers.Remove(ulong.Parse(user.DiscordID));
        }

        public class PersistentStorageData
        {
            public List<LinkedUser> LinkedUsers = new List<LinkedUser>();
        }

        public class WorldStorageData
        {
            public Dictionary<int, int> CurrencyToTradeCountMap = new Dictionary<int, int>();
            public Dictionary<ulong, List<PersonalTradeWatcherEntry>> PersonalTradeWatchers = new Dictionary<ulong, List<PersonalTradeWatcherEntry>>();

            public async Task<bool> AddTradeWatcherDisplay(ulong discordUserId, PersonalTradeWatcherEntry watcherEntry)
            {
                if (!PersonalTradeWatchers.ContainsKey(discordUserId))
                    PersonalTradeWatchers.Add(discordUserId, new List<PersonalTradeWatcherEntry>());

                if (PersonalTradeWatchers[discordUserId].Contains(watcherEntry))
                    return false;

                PersonalTradeWatchers[discordUserId].Add(watcherEntry);
                    await PersonalTradeWatcherAdded?.Invoke(this, EventArgs.Empty, watcherEntry);

                return true;
            }

            public async Task<bool> RemoveTradeWatcherDisplay(ulong discordUserId, PersonalTradeWatcherEntry watcherEntry)
            {
                if (!PersonalTradeWatchers.ContainsKey(discordUserId))
                    return false;

                List<PersonalTradeWatcherEntry> watcherList = PersonalTradeWatchers[discordUserId];
                int toRemoveIndex = watcherList.FindIndex(w => w.Equals(watcherEntry));
                if (toRemoveIndex == -1)
                    return false;

                PersonalTradeWatcherEntry entry = watcherList[toRemoveIndex];
                watcherList.RemoveAt(toRemoveIndex);
                await PersonalTradeWatcherRemoved?.Invoke(this, EventArgs.Empty, entry);

                // Remove the user entry if the last watcher was remvoed
                if (watcherList.Count <= 0)
                    PersonalTradeWatchers.Remove(discordUserId);

                return true;
            }

            public void RemoveAllTradeWatchersForUser(ulong discordUserId)
            {
                if (!PersonalTradeWatchers.ContainsKey(discordUserId))
                    return;

                PersonalTradeWatchers.Remove(discordUserId);
            }

            public int GetTradeWatcherCountTotal()
            {
                return PersonalTradeWatchers.Values.Count();
            }

            public int GetTradeWatcherCountForUser(ulong discordUserID)
            {
                if (!PersonalTradeWatchers.ContainsKey(discordUserID))
                    return 0;

                return PersonalTradeWatchers[discordUserID].Count;
            }

            public string ListTradeWatchers(ulong discordUserID)
            {
                if (!PersonalTradeWatchers.ContainsKey(discordUserID) || PersonalTradeWatchers[discordUserID].Count <= 0)
                    return "No trade watchers exist for this user";

                StringBuilder builder = new StringBuilder();
                builder.Append("Your trade watchers are:\n");
                foreach (PersonalTradeWatcherEntry tradeWatcher in PersonalTradeWatchers[discordUserID])
                {
                    builder.AppendLine($"- {tradeWatcher}");
                }
                return builder.ToString();
            }
        }
    }

    public class PersonalTradeWatcherEntry
    {
        public PersonalTradeWatcherEntry(string key, ModuleType type)
        {
            Key = key;
            Type = type;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            PersonalTradeWatcherEntry rhs = (PersonalTradeWatcherEntry)obj;
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
        public ModuleType Type { get; private set; }
    }
}
