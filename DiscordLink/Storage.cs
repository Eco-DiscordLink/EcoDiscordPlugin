using Eco.EM.Framework.FileManager;
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink
{
    public sealed class DLStorage
    {
        private const string STORAGE_FILE_NAME = "DiscordLinkData";

        public static readonly DLStorage Instance = new DLStorage();
        public static StorageData PersistantData { get; private set; } = new StorageData();

        public Dictionary<string, string> Snippets = new Dictionary<string, string>();        

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static DLStorage()
        {
            PersistantData = new StorageData();
        }

        private DLStorage()
        {
        }

        public void Write()
        {
            FileManager<StorageData>.WriteTypeHandledToFile(PersistantData, DLConstants.BasePath, STORAGE_FILE_NAME);
        }

        public void Read()
        {
            PersistantData = FileManager<StorageData>.ReadTypeHandledFromFile(DLConstants.BasePath, STORAGE_FILE_NAME);
        }

        public class StorageData
        {
            public List<LinkedUser> LinkedUsers = new List<LinkedUser>();
        }
    }
}
