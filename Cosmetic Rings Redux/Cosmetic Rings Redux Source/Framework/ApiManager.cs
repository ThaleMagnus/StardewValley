using ThaleMagnus.CosmeticRingsRedux.Framework.Interfaces;
using StardewModdingAPI;

namespace ThaleMagnus.CosmeticRingsRedux.Framework
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
