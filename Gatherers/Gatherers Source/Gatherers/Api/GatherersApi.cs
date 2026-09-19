using StardewValley.Objects;
using ThaleMagnus.Gatherers.Services;

namespace ThaleMagnus.Gatherers.Api;

public sealed class GatherersApi : IGatherersApi
{
    public bool IsGathererStorage(Chest chest)
    {
        return StorageMarker.IsGathererStorage(chest);
    }
}
