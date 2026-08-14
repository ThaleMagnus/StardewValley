using StardewValley;
using StardewValley.Objects;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Rings
{
    internal abstract class CustomRing
    {
        internal abstract Ring RingObject { get; }

        internal GameLocation CurrentLocation { get; set; }

        internal abstract void HandleEquip(Farmer who, GameLocation location);

        internal abstract void HandleUnequip(Farmer who, GameLocation location);

        internal abstract void HandleNewLocation(Farmer who, GameLocation location);

        internal abstract void HandleLeaveLocation(Farmer who, GameLocation location);

        internal abstract void Update(Farmer who, GameLocation location);
    }
}
