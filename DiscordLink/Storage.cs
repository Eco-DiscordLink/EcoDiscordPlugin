using Eco.EM.Framework.FileManager;
using Eco.Gameplay.GameActions;
using Eco.Plugins.DiscordLink.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public sealed class DLStorage
    {
        private const string PERSISANT_STORAGE_FILE_NAME = "DLPersistantData";
        private const string WORLD_STORAGE_FILE_NAME = "DLWorldData";

        public static readonly DLStorage Instance = new DLStorage();
        public static PersistantStorageData PersistentData { get; private set; } = new PersistantStorageData();
        public static WorldStorageData WorldData { get; private set; } = new WorldStorageData();

        public Dictionary<string, string> Snippets = new Dictionary<string, string>();

        public delegate Task OnTrackedTradeChangedDelegate(object sender, EventArgs e, string tradeItem);
        public static event OnTrackedTradeChangedDelegate TrackedTradeAdded;
        public static event OnTrackedTradeChangedDelegate TrackedTradeRemoved;

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static DLStorage()
        {
            PersistentData = new PersistantStorageData();
        }

        private DLStorage()
        {
        }

        public void Initialize()
        {
            Read();
            LinkedUserManager.OnLinkedUserRemoved += HandleLinkedUserRemoved;
        }

        public void Shutdown()
        {
            LinkedUserManager.OnLinkedUserRemoved -= HandleLinkedUserRemoved;
            Write();
        }

        public void Write()
        {
            FileManager<PersistantStorageData>.WriteTypeHandledToFile(PersistentData, DLConstants.BasePath, PERSISANT_STORAGE_FILE_NAME);
            FileManager<WorldStorageData>.WriteTypeHandledToFile(WorldData, DLConstants.BasePath, WORLD_STORAGE_FILE_NAME);
        }

        public void Read()
        {
            PersistentData = FileManager<PersistantStorageData>.ReadTypeHandledFromFile(DLConstants.BasePath, PERSISANT_STORAGE_FILE_NAME);
            WorldData = FileManager<WorldStorageData>.ReadTypeHandledFromFile(DLConstants.BasePath, WORLD_STORAGE_FILE_NAME);
        }

        public void Reset()
        {
            WorldData = new WorldStorageData();
            Write(); // Make sure we don't read old data in case of an ungraceful shutdown
        }

        public void HandleEvent(DLEventType eventType, object data)
        {
            switch(eventType)
            {
                // Keep track of the amount of trades per currency
                case DLEventType.AccumulatedTrade:
                    if (!(data is IEnumerable<List<CurrencyTrade>> accumulatedTrade)) return;
                    
                    foreach(var list in accumulatedTrade)
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
            WorldData.PlayerTrackedTrades.Remove(ulong.Parse(user.DiscordID));
        }

        public class PersistantStorageData
        {
            public List<LinkedUser> LinkedUsers = new List<LinkedUser>();
        }

        public class WorldStorageData
        {
            public Dictionary<int, int> CurrencyToTradeCountMap = new Dictionary<int, int>();
            public Dictionary<ulong, List<string>> PlayerTrackedTrades = new Dictionary<ulong, List<string>>();

            public async Task<bool> AddTrackedTradeItem(ulong discordUserId, string tradeItem)
            {
                if(!PlayerTrackedTrades.ContainsKey(discordUserId))
                    PlayerTrackedTrades.Add(discordUserId, new List<string>());

                if (PlayerTrackedTrades[discordUserId].Contains(tradeItem))
                    return false;

                PlayerTrackedTrades[discordUserId].Add(tradeItem);
                if(TrackedTradeAdded != null)
                    await TrackedTradeAdded.Invoke(this, EventArgs.Empty, tradeItem);

                return true;
            }

            public async Task<bool> RemoveTrackedTradeItem(ulong discordUserId, string tradeItem)
            {
                if (!PlayerTrackedTrades.ContainsKey(discordUserId))
                    return false;

                bool removed = PlayerTrackedTrades[discordUserId].Remove(tradeItem);
                if (removed && TrackedTradeRemoved != null)
                    await TrackedTradeRemoved?.Invoke(this, EventArgs.Empty, tradeItem);

                // Remove the user entry if the last tracked trade was remvoed
                if (PlayerTrackedTrades[discordUserId].Count <= 0)
                    PlayerTrackedTrades.Remove(discordUserId);

                return removed;
            }

            public void RemoveAllTrackedTradesForUser(ulong discordUserId)
            {
                if (!PlayerTrackedTrades.ContainsKey(discordUserId))
                    return;

                PlayerTrackedTrades.Remove(discordUserId);
            }

            public int GetTrackedTradesCountTotal()
            {
                int count = 0;
                foreach(List<string> trades in PlayerTrackedTrades.Values)
                {
                    count += trades.Count;
                }
                return count;
            }

            public int GetTrackedTradesCountForUser(ulong discordUserId)
            {
                if (!PlayerTrackedTrades.ContainsKey(discordUserId))
                    return 0;

                return PlayerTrackedTrades[discordUserId].Count;
            }

            public string ListTrackedTrades(ulong discordUserId)
            {
                if (!PlayerTrackedTrades.ContainsKey(discordUserId) || PlayerTrackedTrades[discordUserId].Count <= 0)
                    return "No tracked trades exist for this user";

                StringBuilder builder = new StringBuilder();
                builder.Append("Your tracked trades are:\n");
                foreach(string trackedTrade in PlayerTrackedTrades[discordUserId])
                {
                    builder.AppendLine($"- {trackedTrade}");
                }
                return builder.ToString();
            }
        }
    }
}
