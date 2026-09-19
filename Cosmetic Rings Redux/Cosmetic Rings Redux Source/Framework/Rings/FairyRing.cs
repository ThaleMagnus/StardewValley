using ThaleMagnus.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Objects;
using System.Collections.Generic;

namespace ThaleMagnus.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class FairyRing : CustomRing
    {
        private Fairy fairy;

        internal override Ring RingObject { get; }

        internal FairyRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            EnsureCritters(location);
            fairy ??= new Fairy(who.Tile);
            fairy.ResetForNewLocation(who.Tile, location);
            AddIfMissing(location, fairy);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
            Remove(location);
            fairy = null;
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            EnsureCritters(location);
            fairy ??= new Fairy(who.Tile);
            fairy.ResetForNewLocation(who.Tile, location);
            AddIfMissing(location, fairy);
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
            if (fairy == null)
                return;

            fairy.DetachLight(location);
            location?.critters?.Remove(fairy);
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
