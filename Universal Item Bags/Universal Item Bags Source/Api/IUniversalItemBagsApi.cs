using System.Collections.Generic;
using ThaleTheGreat.UniversalItemBags;

namespace ThaleTheGreat.UniversalItemBags.Api;

public interface IUniversalItemBagsApi
{
    void Refresh();

    IReadOnlyDictionary<string, IReadOnlyList<string>> GetAssignments();

    IReadOnlyList<string> GetUnclassifiedItems();

    bool RegisterItem(
        string ownerModId,
        string qualifiedItemId,
        IEnumerable<string> bagTypeIds,
        UniversalBagSize minimumBagSize = UniversalBagSize.Small,
        bool? hasQualities = null
    );

    bool UnregisterItems(string ownerModId);
}
