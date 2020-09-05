using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink
{
    public sealed class DLStorage
    {
        public static readonly DLStorage Instance = new DLStorage();
        public Dictionary<string, string> Snippets = new Dictionary<string, string>();

        // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
        static DLStorage()
        {
        }

        private DLStorage()
        {
        }
    }
}
