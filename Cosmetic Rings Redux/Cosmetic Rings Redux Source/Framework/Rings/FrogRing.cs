using ThaleMagnus.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.Objects;

namespace ThaleMagnus.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class FrogRing : CustomRing
    {
        private FrogFollower frog;

        internal override Ring RingObject { get; }

        internal FrogRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            frog ??= new FrogFollower(who.Tile);
            AddIfMissing(location, frog);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
            Remove(location);
            frog = null;
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            frog ??= new FrogFollower(who.Tile);
            frog.resetForNewLocation(who.Tile);
            AddIfMissing(location, frog);
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
            if (frog != null)
                location?.characters?.Remove(frog);
        }

        private static void AddIfMissing(GameLocation location, NPC follower)
        {
            if (location?.characters != null && !location.characters.Contains(follower))
                location.characters.Add(follower);
        }
    }
}
