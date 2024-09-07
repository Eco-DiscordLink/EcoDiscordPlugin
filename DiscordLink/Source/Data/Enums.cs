using DSharpPlus.SlashCommands;

namespace Eco.Plugins.DiscordLink
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

    public enum ChatSyncDirection
    {
        DiscordToEco,   // Send Discord messages to Eco
        EcoToDiscord,   // Send Eco messages to Discord
        Duplex,         // Send Discord messages to Eco and Eco messages to Discord
    }

    public enum ChatSyncMode
    {
        OptIn,
        OptOut,
    }
}
