using DSharpPlus.SlashCommands;

namespace Eco.Plugins.DiscordLink
{
    public static class Enums
    {
        public enum ApplicationInterfaceType
        {
            [ChoiceName("Eco")]
            Eco,
            [ChoiceName("Discord")]
            Discord
        }

        public enum DiscordReactionChange
        {
            Added,
            Removed
        }

        public enum CurrencyType
        {
            [ChoiceName("All")]
            All,
            [ChoiceName("Personal")]
            Personal,
            [ChoiceName("Minted")]
            Minted
        }

        public enum ChatSyncMode
        {
            OptIn,
            OptOut,
        }
    }
}
