using StardewModdingAPI;
using System;

namespace ThaleTheGreat.UniversalItemBags.Integration;

internal static class GmcmIntegration
{
    private const string GmcmModId = "spacechase0.GenericModConfigMenu";

    private static readonly string[] BagSizes =
    {
        nameof(UniversalBagSize.Small),
        nameof(UniversalBagSize.Medium),
        nameof(UniversalBagSize.Large),
        nameof(UniversalBagSize.Giant),
        nameof(UniversalBagSize.Massive)
    };

    public static void Register(
        IModHelper helper,
        IMonitor monitor,
        IManifest manifest,
        ModConfig config,
        Action refresh
    )
    {
        IGenericModConfigMenuApi? gmcm = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(GmcmModId);
        if (gmcm is null)
            return;

        try
        {
            gmcm.Register(
                manifest,
                reset: () => config.CopyFrom(new ModConfig()),
                save: () =>
                {
                    helper.WriteConfig(config);
                    refresh();
                }
            );

            gmcm.AddBoolOption(
                manifest,
                getValue: () => config.EnableAutomaticDiscovery,
                setValue: value => config.EnableAutomaticDiscovery = value,
                name: () => "Enable Automatic Discovery",
                tooltip: () => "Automatically add verified mod-owned objects and big craftables to compatible standard Item Bags categories.",
                fieldId: nameof(ModConfig.EnableAutomaticDiscovery)
            );

            gmcm.AddTextOption(
                manifest,
                getValue: () => config.MinimumBagSize.ToString(),
                setValue: value =>
                {
                    if (Enum.TryParse(value, ignoreCase: true, out UniversalBagSize size))
                        config.MinimumBagSize = size;
                },
                name: () => "Minimum Bag Size",
                tooltip: () => "The smallest standard bag size that receives automatically discovered items.",
                allowedValues: BagSizes,
                fieldId: nameof(ModConfig.MinimumBagSize)
            );

            gmcm.AddBoolOption(
                manifest,
                getValue: () => config.DebugLogging,
                setValue: value => config.DebugLogging = value,
                name: () => "Debug Logging",
                tooltip: () => "Log discovery totals and skipped-item details for troubleshooting.",
                fieldId: nameof(ModConfig.DebugLogging)
            );
        }
        catch (Exception ex)
        {
            monitor.Log($"Could not register Generic Mod Config Menu integration.\n{ex}", LogLevel.Error);
        }
    }
}
