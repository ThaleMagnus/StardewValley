using StardewModdingAPI;

namespace ThaleMagnus.CosmeticRingsRedux.Framework
{
    internal static class ResourceManager
    {
        internal static string RaindropsTexturePath { get; private set; }

        internal static string FrogTexturePath { get; private set; }

        internal static string FrogAlternativeTexturePath { get; private set; }

        internal static void SetUpAssets(IModHelper helper)
        {
            RaindropsTexturePath = helper.ModContent.GetInternalAssetName("assets/Sprites/Raindrops.png").BaseName;
            FrogTexturePath = helper.ModContent.GetInternalAssetName("assets/Sprites/Frog_0.png").BaseName;
            FrogAlternativeTexturePath = helper.ModContent.GetInternalAssetName("assets/Sprites/Frog_1.png").BaseName;
        }
    }
}
