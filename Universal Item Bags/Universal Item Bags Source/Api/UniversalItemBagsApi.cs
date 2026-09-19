using System;
using System.Collections.Generic;
using ThaleMagnus.UniversalItemBags.Discovery;
using ThaleMagnus.UniversalItemBags;

namespace ThaleMagnus.UniversalItemBags.Api;

public sealed class UniversalItemBagsApi : IUniversalItemBagsApi
{
    private readonly UniversalDiscoveryService discovery;

    internal UniversalItemBagsApi(UniversalDiscoveryService discovery)
    {
        this.discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
    }

    public void Refresh()
    {
        discovery.ScheduleRefresh(1);
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetAssignments()
    {
        return discovery.GetAssignments();
    }

    public IReadOnlyList<string> GetUnclassifiedItems()
    {
        return discovery.GetUnclassifiedItems();
    }

    public bool RegisterItem(
        string ownerModId,
        string qualifiedItemId,
        IEnumerable<string> bagTypeIds,
        UniversalBagSize minimumBagSize = UniversalBagSize.Small,
        bool? hasQualities = null
    )
    {
        return discovery.RegisterItem(ownerModId, qualifiedItemId, bagTypeIds, minimumBagSize, hasQualities);
    }

    public bool UnregisterItems(string ownerModId)
    {
        return discovery.UnregisterItems(ownerModId);
    }
}
