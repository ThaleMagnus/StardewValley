using ThaleTheGreat.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Objects;
using System.Collections.Generic;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class RaindropRing : CustomRing
    {
        private RainCloud rainCloud;

        internal override Ring RingObject { get; }

        internal RaindropRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            Spawn(who, location);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
            Remove(location);
            rainCloud = null;
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            Spawn(who, location);
        }

        internal override void HandleLeaveLocation(Farmer who, GameLocation location)
        {
            Remove(location);
            rainCloud = null;
        }

        internal override void Update(Farmer who, GameLocation location)
        {
        }

        private void Spawn(Farmer who, GameLocation location)
        {
            location.critters ??= new List<Critter>();
            rainCloud = new RainCloud(
                who.Tile,
                0,
                Game1.random.Next(15) / 500f,
                Game1.random.Next(-10, 0) / 50f,
                Game1.random.Next(10) / 50f
            );
            location.critters.Add(rainCloud);
        }

        private void Remove(GameLocation location)
        {
            if (rainCloud != null)
                location?.critters?.Remove(rainCloud);
        }
    }
}
