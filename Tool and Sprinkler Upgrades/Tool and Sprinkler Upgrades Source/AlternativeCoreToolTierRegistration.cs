namespace ThaleTheGreat.ToolAndSprinklerUpgrades;

internal sealed record AlternativeCoreToolTierRegistration(
    string OwnerModId,
    string TierId,
    int UpgradeLevel,
    string TextureAsset,
    string BarItemId,
    string AxeId,
    Func<string> AxeDisplayNameProvider,
    string PickaxeId,
    Func<string> PickaxeDisplayNameProvider,
    string HoeId,
    Func<string> HoeDisplayNameProvider,
    string WateringCanId,
    Func<string> WateringCanDisplayNameProvider
)
{
    public string AxeDisplayName => AxeDisplayNameProvider();
    public string PickaxeDisplayName => PickaxeDisplayNameProvider();
    public string HoeDisplayName => HoeDisplayNameProvider();
    public string WateringCanDisplayName => WateringCanDisplayNameProvider();
}
