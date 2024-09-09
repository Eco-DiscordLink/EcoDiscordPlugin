using System.ComponentModel;
using System.Linq;

namespace Eco.Plugins.DiscordLink
{
    public class LayerNameAutocompleteConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(Moose.Utils.Lookups.Lookups.Layers.Select(layer => layer.Name).Order().ToList());
        }
    }
}
