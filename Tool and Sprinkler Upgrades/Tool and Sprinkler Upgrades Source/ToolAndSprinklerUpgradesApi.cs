namespace ThaleMagnus.ToolAndSprinklerUpgrades;

public sealed class ToolAndSprinklerUpgradesApi : IToolAndSprinklerUpgradesApi
{
    public bool IsPrismaticHighest => ModEntry.Config.RadioactiveBeforePrismatic;

    public bool RegisterAlternativeLevelFiveCoreTools(
        string ownerModId,
        string tierId,
        string textureAsset,
        string barItemId,
        string axeId,
        string axeDisplayName,
        string pickaxeId,
        string pickaxeDisplayName,
        string hoeId,
        string hoeDisplayName,
        string wateringCanId,
        string wateringCanDisplayName
    )
    {
        return ModEntry.Instance.RegisterAlternativeLevelFiveCoreTools(
            ownerModId,
            tierId,
            textureAsset,
            barItemId,
            axeId,
            axeDisplayName,
            pickaxeId,
            pickaxeDisplayName,
            hoeId,
            hoeDisplayName,
            wateringCanId,
            wateringCanDisplayName
        );
    }

    public bool RegisterAlternativeLevelSixCoreTools(
        string ownerModId,
        string tierId,
        string textureAsset,
        string barItemId,
        string axeId,
        string axeDisplayName,
        string pickaxeId,
        string pickaxeDisplayName,
        string hoeId,
        string hoeDisplayName,
        string wateringCanId,
        string wateringCanDisplayName
    )
    {
        return ModEntry.Instance.RegisterAlternativeLevelSixCoreTools(
            ownerModId,
            tierId,
            textureAsset,
            barItemId,
            axeId,
            axeDisplayName,
            pickaxeId,
            pickaxeDisplayName,
            hoeId,
            hoeDisplayName,
            wateringCanId,
            wateringCanDisplayName
        );
    }


    public bool RegisterAlternativeLevelFiveCoreToolsLocalized(
        string ownerModId,
        string tierId,
        string textureAsset,
        string barItemId,
        string axeId,
        Func<string> axeDisplayName,
        string pickaxeId,
        Func<string> pickaxeDisplayName,
        string hoeId,
        Func<string> hoeDisplayName,
        string wateringCanId,
        Func<string> wateringCanDisplayName
    )
    {
        return ModEntry.Instance.RegisterAlternativeLevelFiveCoreToolsLocalized(
            ownerModId,
            tierId,
            textureAsset,
            barItemId,
            axeId,
            axeDisplayName,
            pickaxeId,
            pickaxeDisplayName,
            hoeId,
            hoeDisplayName,
            wateringCanId,
            wateringCanDisplayName
        );
    }

    public bool RegisterAlternativeLevelSixCoreToolsLocalized(
        string ownerModId,
        string tierId,
        string textureAsset,
        string barItemId,
        string axeId,
        Func<string> axeDisplayName,
        string pickaxeId,
        Func<string> pickaxeDisplayName,
        string hoeId,
        Func<string> hoeDisplayName,
        string wateringCanId,
        Func<string> wateringCanDisplayName
    )
    {
        return ModEntry.Instance.RegisterAlternativeLevelSixCoreToolsLocalized(
            ownerModId,
            tierId,
            textureAsset,
            barItemId,
            axeId,
            axeDisplayName,
            pickaxeId,
            pickaxeDisplayName,
            hoeId,
            hoeDisplayName,
            wateringCanId,
            wateringCanDisplayName
        );
    }

    public bool SetAlternativeTierBarTransmutationEnabled(
        string ownerModId,
        string tierId,
        bool enabled
    )
    {
        return ModEntry.Instance.SetAlternativeTierBarTransmutationEnabled(ownerModId, tierId, enabled);
    }
}
