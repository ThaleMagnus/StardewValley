using ThaleTheGreat.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.Objects;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class JunimoRing : CustomRing
    {
        private JunimoFollower junimo;

        internal override Ring RingObject { get; }

        internal JunimoRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            junimo ??= new JunimoFollower(who.Tile);
            AddIfMissing(location, junimo);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
            Remove(location);
            junimo = null;
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            junimo ??= new JunimoFollower(who.Tile);
            junimo.resetForNewLocation(who.Tile);
            AddIfMissing(location, junimo);
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
            if (junimo != null)
                location?.characters?.Remove(junimo);
        }

        private static void AddIfMissing(GameLocation location, NPC follower)
        {
            if (location?.characters != null && !location.characters.Contains(follower))
                location.characters.Add(follower);
        }
    }
}
