using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public class LayerChannelLink : ChannelLink
    {
        [TypeConverter(typeof(LayerNameAutocompleteConverter))]
        public string LayerName { get; set; } = string.Empty;
        public bool UseTerrainComparison { get; set; } = false;

        public override bool IsValid() => base.IsValid() && LayerName != string.Empty;
    }
}
