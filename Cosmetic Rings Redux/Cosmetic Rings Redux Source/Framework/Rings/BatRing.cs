using ThaleTheGreat.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Objects;
using System.Collections.Generic;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class BatRing : CustomRing
    {
        private BatFollower bat;

        internal override Ring RingObject { get; }

        internal BatRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            EnsureCritters(location);
            bat ??= new BatFollower(who.Tile);
            AddIfMissing(location, bat);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
            Remove(location);
            bat = null;
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            EnsureCritters(location);
            bat ??= new BatFollower(who.Tile);
            bat.resetForNewLocation(who.Tile);
            AddIfMissing(location, bat);
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
            if (bat != null)
                location?.critters?.Remove(bat);
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
