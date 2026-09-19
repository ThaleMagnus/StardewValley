using StardewModdingAPI.Utilities;

#nullable enable
namespace ThaleMagnus.DateChange;

public class ModConfig
{
  public KeybindList OpenMenuKey { get; set; } = KeybindList.Parse("LeftControl + C");

  public bool ApplyImmediately { get; set; } = false;

  public bool DebugLogging { get; set; } = false;
}
