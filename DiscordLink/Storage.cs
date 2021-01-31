using Eco.EM.Framework.FileManager;
using Eco.Gameplay.GameActions;
using Eco.Plugins.DiscordLink.Events;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink
{
    public sealed class DLStorage
    {
        private const string PERSISANT_STORAGE_FILE_NAME = "DLPersistantData";
        private const string WORLD_STORAGE_FILE_NAME = "DLWorldData";

        public static readonly DLStorage Instance = new DLStorage();
        public static PersistantStorageData PersistantData { get; private set; } = new PersistantStorageData();
        public static WorldStorageData WorldData { get; private set; } = new WorldStorageData();

        public Dictionary<string, string> Snippets = new Dictionary<string, string>();        

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static DLStorage()
        {
            PersistantData = new PersistantStorageData();
        }

        private DLStorage()
        {
        }

        public void Write()
        {
            FileManager<PersistantStorageData>.WriteTypeHandledToFile(PersistantData, DLConstants.BasePath, PERSISANT_STORAGE_FILE_NAME);
            FileManager<WorldStorageData>.WriteTypeHandledToFile(WorldData, DLConstants.BasePath, WORLD_STORAGE_FILE_NAME);
        }

        public void Read()
        {
            PersistantData = FileManager<PersistantStorageData>.ReadTypeHandledFromFile(DLConstants.BasePath, PERSISANT_STORAGE_FILE_NAME);
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

        public class PersistantStorageData
        {
            public List<LinkedUser> LinkedUsers = new List<LinkedUser>();
        }

        public class WorldStorageData
        {
            public Dictionary<int, int> CurrencyToTradeCountMap = new Dictionary<int, int>();
        }
    }
}
