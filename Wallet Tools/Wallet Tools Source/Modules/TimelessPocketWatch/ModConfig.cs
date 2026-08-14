using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace ThaleTheGreat.WalletToolsForTimelessPocketWatch;

internal sealed class ModConfig
{
    public bool Enabled { get; set; } = true;
    public bool AutoStoreFromInventory { get; set; } = true;
    public bool ShowWalletPower { get; set; } = true;
    public bool PlayToolSwapSound { get; set; } = true;
    public bool ShowStoredMessage { get; set; } = true;
    public bool ShowGearMessage { get; set; } = true;
    public bool ShowMissingMessage { get; set; } = true;

    public KeybindList UsePocketWatchHotkey { get; set; } = new(new Keybind(SButton.LeftControl, SButton.T));
}
