using ThaleMagnus.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.Objects;

namespace ThaleMagnus.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class BunnyRing : CustomRing
    {
        private BunnyFollower bunny;

        internal override Ring RingObject { get; }

        internal BunnyRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            bunny ??= new BunnyFollower(who.Tile);
            AddIfMissing(location, bunny);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
            Remove(location);
            bunny = null;
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            bunny ??= new BunnyFollower(who.Tile);
            bunny.resetForNewLocation(who.Tile);
            AddIfMissing(location, bunny);
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
            if (bunny != null)
                location?.characters?.Remove(bunny);
        }

        private static void AddIfMissing(GameLocation location, NPC follower)
        {
            if (location?.characters != null && !location.characters.Contains(follower))
                location.characters.Add(follower);
        }
    }
}
