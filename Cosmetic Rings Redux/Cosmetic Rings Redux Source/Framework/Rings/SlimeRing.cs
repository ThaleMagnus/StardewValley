using ThaleMagnus.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.Objects;

namespace ThaleMagnus.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class SlimeRing : CustomRing
    {
        private SlimeFollower slime;

        internal override Ring RingObject { get; }

        internal SlimeRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            slime ??= new SlimeFollower(who.Tile);
            AddIfMissing(location, slime);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
            Remove(location);
            slime = null;
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            slime ??= new SlimeFollower(who.Tile);
            slime.resetForNewLocation(who.Tile);
            AddIfMissing(location, slime);
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
            if (slime != null)
                location?.characters?.Remove(slime);
        }

        private static void AddIfMissing(GameLocation location, NPC follower)
        {
            if (location?.characters != null && !location.characters.Contains(follower))
                location.characters.Add(follower);
        }
    }
}
