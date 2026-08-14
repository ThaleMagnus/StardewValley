namespace ThaleTheGreat.UniversalItemBags;

public enum UniversalBagSize
{
    Small = 0,
    Medium = 1,
    Large = 2,
    Giant = 3,
    Massive = 4
}

public sealed class ModConfig
{
    public bool EnableAutomaticDiscovery { get; set; } = true;

    public UniversalBagSize MinimumBagSize { get; set; } = UniversalBagSize.Small;

    public bool DebugLogging { get; set; } = false;

    public void CopyFrom(ModConfig other)
    {
        EnableAutomaticDiscovery = other.EnableAutomaticDiscovery;
        MinimumBagSize = other.MinimumBagSize;
        DebugLogging = other.DebugLogging;
    }
}
