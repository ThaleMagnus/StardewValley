using ThaleTheGreat.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Objects;
using System.Collections.Generic;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class PetalRing : CustomRing
    {
        internal override Ring RingObject { get; }

        internal PetalRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            SpawnPetal(who, location);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            SpawnPetal(who, location);
        }

        internal override void HandleLeaveLocation(Farmer who, GameLocation location)
        {
        }

        internal override void Update(Farmer who, GameLocation location)
        {
            SpawnPetal(who, location);
        }

        private static void SpawnPetal(Farmer who, GameLocation location)
        {
            location.critters ??= new List<Critter>();
            location.critters.Add(new Petal(
                who.Tile,
                0,
                Game1.random.Next(15) / 500f,
                Game1.random.Next(-10, 0) / 50f,
                Game1.random.Next(10) / 50f
            ));
        }
    }
}
