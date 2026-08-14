using ThaleTheGreat.CosmeticRingsRedux.Framework.Rings;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework
{
    internal enum RingType
    {
        Unknown,
        PetalRing,
        ButterflyRing,
        FairyRing,
        RaindropRing,
        BunnyRing,
        JunimoRing,
        SlimeRing,
        FireflyRing,
        FrogRing,
        DustRing,
        BatRing
    }

    internal static class RingManager
    {
        private const string RingNamePrefix = "ThaleTheGreat.CosmeticRingsRedux.Rings";
        private static readonly HashSet<string> RingNames = Enum
            .GetValues<RingType>()
            .Where(type => type != RingType.Unknown)
            .Select(type => $"{RingNamePrefix}.{type}")
            .ToHashSet(StringComparer.Ordinal);

        private static readonly List<CustomRing> WornCustomRings = new List<CustomRing>();

        internal static bool IsCosmeticRing(Ring ring)
        {
            return ring != null && (RingNames.Contains(ring.ItemId) || RingNames.Contains(ring.Name));
        }

        internal static void LoadWornRings(Farmer who, GameLocation location, IEnumerable<Ring> rings)
        {
            RemoveAllEffects();

            foreach (Ring ring in ExpandCombinedRings(rings).Where(IsCosmeticRing))
                HandleEquip(who, location, ring);
        }

        internal static void UpdateRingEffects(Farmer who, GameLocation location)
        {
            foreach (CustomRing ring in WornCustomRings.ToArray())
                ring.Update(who, location);
        }

        internal static void HandleEquip(Farmer who, GameLocation location, Ring ring)
        {
            if (!IsCosmeticRing(ring) || WornCustomRings.Any(entry => ReferenceEquals(entry.RingObject, ring)))
                return;

            CustomRing customRing = CreateCustomRing(ring);
            if (customRing == null)
                return;

            customRing.CurrentLocation = location;
            customRing.HandleEquip(who, location);
            WornCustomRings.Add(customRing);
        }

        internal static void HandleUnequip(Farmer who, GameLocation location, Ring ring)
        {
            CustomRing customRing = WornCustomRings.FirstOrDefault(entry => ReferenceEquals(entry.RingObject, ring));
            if (customRing == null)
                return;

            GameLocation effectLocation = customRing.CurrentLocation ?? location;
            if (effectLocation != null)
                customRing.HandleUnequip(who, effectLocation);

            customRing.CurrentLocation = null;
            WornCustomRings.Remove(customRing);
        }

        internal static void HandleNewLocation(Farmer who, GameLocation location, Ring ring)
        {
            CustomRing customRing = WornCustomRings.FirstOrDefault(entry => ReferenceEquals(entry.RingObject, ring));
            if (customRing == null)
                return;

            customRing.CurrentLocation = location;
            customRing.HandleNewLocation(who, location);
        }

        internal static void HandleLeaveLocation(Farmer who, GameLocation location, Ring ring)
        {
            CustomRing customRing = WornCustomRings.FirstOrDefault(entry => ReferenceEquals(entry.RingObject, ring));
            if (customRing == null)
                return;

            customRing.HandleLeaveLocation(who, location);
            customRing.CurrentLocation = null;
        }

        internal static void RemoveAllEffects()
        {
            Farmer who = Game1.player;
            foreach (CustomRing ring in WornCustomRings.ToArray())
            {
                if (who != null && ring.CurrentLocation != null)
                    ring.HandleUnequip(who, ring.CurrentLocation);
            }

            WornCustomRings.Clear();
        }

        internal static void Reset()
        {
            WornCustomRings.Clear();
        }

        private static IEnumerable<Ring> ExpandCombinedRings(IEnumerable<Ring> rings)
        {
            Stack<Ring> pending = new Stack<Ring>((rings ?? Enumerable.Empty<Ring>()).Where(ring => ring != null));
            while (pending.Count > 0)
            {
                Ring ring = pending.Pop();
                if (ring is CombinedRing combinedRing)
                {
                    foreach (Ring combined in combinedRing.combinedRings)
                    {
                        if (combined != null)
                            pending.Push(combined);
                    }
                }
                else
                {
                    yield return ring;
                }
            }
        }

        private static CustomRing CreateCustomRing(Ring ring)
        {
            switch (GetRingType(ring))
            {
                case RingType.PetalRing:
                    return new PetalRing(ring);
                case RingType.ButterflyRing:
                    return new ButterflyRing(ring);
                case RingType.FairyRing:
                    return new FairyRing(ring);
                case RingType.RaindropRing:
                    return new RaindropRing(ring);
                case RingType.BunnyRing:
                    return new BunnyRing(ring);
                case RingType.JunimoRing:
                    return new JunimoRing(ring);
                case RingType.SlimeRing:
                    return new SlimeRing(ring);
                case RingType.FireflyRing:
                    return new FireflyRing(ring);
                case RingType.FrogRing:
                    return new FrogRing(ring);
                case RingType.DustRing:
                    return new DustRing(ring);
                case RingType.BatRing:
                    return new BatRing(ring);
                default:
                    return null;
            }
        }

        private static RingType GetRingType(Ring ring)
        {
            string ringId = RingNames.Contains(ring.ItemId) ? ring.ItemId : ring.Name;
            if (string.IsNullOrEmpty(ringId) || !ringId.StartsWith(RingNamePrefix + ".", StringComparison.Ordinal))
                return RingType.Unknown;

            return Enum.TryParse(ringId.Substring(RingNamePrefix.Length + 1), out RingType type)
                ? type
                : RingType.Unknown;
        }
    }
}
