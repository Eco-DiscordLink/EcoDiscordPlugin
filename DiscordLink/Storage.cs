using Eco.EM;
using Eco.EM.Framework.FileManager;
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink
{
    public sealed class DLStorage
    {
        private readonly string StorageFileName = "DiscordLinkData";

        public static readonly DLStorage Instance = new DLStorage();
        public Dictionary<string, string> Snippets = new Dictionary<string, string>();

        private StorageData Data = new StorageData();

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static DLStorage()
        {
        }

        private DLStorage()
        {
        }

        public void Write()
        {
            FileManager<StorageData>.WriteToFile(Data, DiscordLink.BasePath, StorageFileName);
        }

        public void Read()
        {
            FileManager<StorageData>.ReadFromFile(DiscordLink.BasePath, StorageFileName);
        }
    }

    public class StorageData
    {

    }
}
