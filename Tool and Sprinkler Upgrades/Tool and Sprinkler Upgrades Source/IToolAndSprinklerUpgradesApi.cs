namespace ThaleMagnus.ToolAndSprinklerUpgrades;

public interface IToolAndSprinklerUpgradesApi
{
    bool IsPrismaticHighest { get; }

    bool RegisterAlternativeLevelFiveCoreTools(
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
    );

    bool RegisterAlternativeLevelSixCoreTools(
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
    );

    bool RegisterAlternativeLevelFiveCoreToolsLocalized(
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
    );

    bool RegisterAlternativeLevelSixCoreToolsLocalized(
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
    );

    bool SetAlternativeTierBarTransmutationEnabled(
        string ownerModId,
        string tierId,
        bool enabled
    );
}
