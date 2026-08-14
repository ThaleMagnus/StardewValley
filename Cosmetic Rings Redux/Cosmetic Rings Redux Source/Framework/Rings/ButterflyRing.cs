using ThaleTheGreat.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Objects;
using System.Collections.Generic;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class ButterflyRing : CustomRing
    {
        private ButterflyFollower butterfly;

        internal override Ring RingObject { get; }

        internal ButterflyRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            EnsureCritters(location);
            butterfly ??= new ButterflyFollower(who.Tile);
            AddIfMissing(location, butterfly);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
            Remove(location);
            butterfly = null;
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            EnsureCritters(location);
            butterfly ??= new ButterflyFollower(who.Tile);
            butterfly.resetForNewLocation(who.Tile);
            AddIfMissing(location, butterfly);
        }

        internal override void HandleLeaveLocation(Farmer who, GameLocation location)
        {
            Remove(location);
        }

        internal override void Update(Farmer who, GameLocation location)
        {
        }

        private void Remove(GameLocation location)
        {
            if (butterfly != null)
                location?.critters?.Remove(butterfly);
        }

        private static void EnsureCritters(GameLocation location)
        {
            location.critters ??= new List<Critter>();
        }

        private static void AddIfMissing(GameLocation location, Critter follower)
        {
            if (!location.critters.Contains(follower))
                location.critters.Add(follower);
        }
    }
}
