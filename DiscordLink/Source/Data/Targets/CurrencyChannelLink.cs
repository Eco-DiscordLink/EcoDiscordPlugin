using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class CurrencyChannelLink : ChannelLink
    {
        [Description("Conditions for showing minted currencies.")]
        public CurrencyTypeDisplayCondition UseMintedCurrency { get; set; } = CurrencyTypeDisplayCondition.MintedExists;

        [Description("Conditions for showing personal currencies.")]
        public CurrencyTypeDisplayCondition UsePersonalCurrency { get; set; } = CurrencyTypeDisplayCondition.NoMintedExists;

        [Description("Max minted currencies to show.")]
        public int MaxMintedCount { get; set; } = DLConfig.DefaultValues.MaxMintedCurrencies;

        [Description("Max personal currencies to show.")]
        public int MaxPersonalCount { get; set; } = DLConfig.DefaultValues.MaxPersonalCurrencies;

        [Description("Max currency holders to show.")]
        public int MaxTopCurrencyHolderCount { get; set; } = DLConfig.DefaultValues.MaxTopCurrencyHolderCount;

        [Description("Display the amount of trades for each currency.")]
        public bool UseTradeCount { get; set; } = false;

        [Description("Display the backing information (if such exists) for each currency.")]
        public bool UseBackingInfo { get; set; } = false;
    }
}
