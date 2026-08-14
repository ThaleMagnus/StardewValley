using ThaleTheGreat.CosmeticRingsRedux.Framework.Interfaces;
using StardewModdingAPI;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework
{
    public static class ApiManager
    {
        public static IWearMoreRingsApi GetWearMoreRingsApi(IModHelper helper)
        {
            return helper.ModRegistry.GetApi<IWearMoreRingsApi>("bcmpinc.WearMoreRings");
        }

        public static IGenericModConfigMenuAPI GetGenericModConfigMenuApi(IModHelper helper)
        {
            return helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");
        }
    }
}
