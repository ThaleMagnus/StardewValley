using ThaleMagnus.ClearMonocle.Metadata;
using ThaleMagnus.ClearMonocle.Types;

namespace ThaleMagnus.ClearMonocle;

internal static class ResourceManager {
    internal static readonly ConcurrentConsumer<ulong> ReleasedTextureMetas =
        new("ReleasedTextureMetas", Texture2DMeta.Cleanup);
    internal static readonly ConcurrentConsumer<ManagedSpriteInstance.CleanupData> ReleasedSpriteInstances =
        new("ReleasedSpriteInstances", ManagedSpriteInstance.Cleanup);
}
