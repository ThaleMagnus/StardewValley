using System;
using System.Collections.Generic;
using System.Linq;

namespace ThaleMagnus.UniversalItemBags.Discovery;

internal sealed class ItemCandidate : IEquatable<ItemCandidate>
{
    public ItemCandidate(
        string itemId,
        bool isBigCraftable,
        int category,
        string objectType,
        IEnumerable<string> contextTags
    )
    {
        ItemId = itemId;
        IsBigCraftable = isBigCraftable;
        Category = category;
        ObjectType = objectType ?? string.Empty;
        ContextTags = new HashSet<string>(contextTags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public string ItemId { get; }

    public bool IsBigCraftable { get; }

    public int Category { get; }

    public string ObjectType { get; }

    public IReadOnlySet<string> ContextTags { get; }

    public string QualifiedItemId => $"{(IsBigCraftable ? "(BC)" : "(O)")}{ItemId}";

    public bool Equals(ItemCandidate? other)
    {
        return other is not null
            && IsBigCraftable == other.IsBigCraftable
            && string.Equals(ItemId, other.ItemId, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is ItemCandidate other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(ItemId), IsBigCraftable);
    }
}

internal sealed class ItemClassification
{
    public static ItemClassification Empty { get; } = new(Array.Empty<string>(), false);

    public ItemClassification(IEnumerable<string> bagTypeIds, bool hasQualities)
    {
        BagTypeIds = bagTypeIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        HasQualities = hasQualities;
    }

    public IReadOnlyList<string> BagTypeIds { get; }

    public bool HasQualities { get; }
}

internal sealed class ExplicitRegistration
{
    public ExplicitRegistration(
        string ownerModId,
        string qualifiedItemId,
        string itemId,
        bool isBigCraftable,
        IEnumerable<string> bagTypeIds,
        UniversalBagSize minimumBagSize,
        bool? hasQualities
    )
    {
        OwnerModId = ownerModId;
        QualifiedItemId = qualifiedItemId;
        ItemId = itemId;
        IsBigCraftable = isBigCraftable;
        BagTypeIds = bagTypeIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        MinimumBagSize = minimumBagSize;
        HasQualities = hasQualities;
    }

    public string OwnerModId { get; }

    public string QualifiedItemId { get; }

    public string ItemId { get; }

    public bool IsBigCraftable { get; }

    public IReadOnlyList<string> BagTypeIds { get; }

    public UniversalBagSize MinimumBagSize { get; }

    public bool? HasQualities { get; }
}
