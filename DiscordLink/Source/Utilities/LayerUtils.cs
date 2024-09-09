using Eco.Moose.Utils.Lookups;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class LayerUtils
    {
        public static string GetLayerName(MapRepresentationType representationType)
        {
            return representationType switch
            {
                MapRepresentationType.Preview => "WorldPreview",
                MapRepresentationType.Terrain => "TerrainLatest",
                _ => ""
            };
        }

        public static string GetLayerLink(string layerFileName)
        {
            return $"{Lookups.WebServerUrl}/Layers/{layerFileName}.gif?discriminator={DLStorage.PersistentData.LayerDiscriminator.GetDiscriminator()}";
        }
    }
}
