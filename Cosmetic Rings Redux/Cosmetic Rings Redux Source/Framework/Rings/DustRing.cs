using ThaleTheGreat.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.Objects;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class DustRing : CustomRing
    {
        private DustSprite dustSprite;

        internal override Ring RingObject { get; }

        internal DustRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            dustSprite ??= new DustSprite(who.Tile);
            AddIfMissing(location, dustSprite);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
            Remove(location);
            dustSprite = null;
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            dustSprite ??= new DustSprite(who.Tile);
            dustSprite.resetForNewLocation(who.Tile);
            AddIfMissing(location, dustSprite);
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
            if (dustSprite != null)
                location?.characters?.Remove(dustSprite);
        }

        private static void AddIfMissing(GameLocation location, NPC follower)
        {
            if (location?.characters != null && !location.characters.Contains(follower))
                location.characters.Add(follower);
        }
    }
}
