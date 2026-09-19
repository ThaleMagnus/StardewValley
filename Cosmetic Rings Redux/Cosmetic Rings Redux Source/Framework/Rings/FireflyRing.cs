using ThaleMagnus.CosmeticRingsRedux.Framework.Critters;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Objects;
using System.Collections.Generic;

namespace ThaleMagnus.CosmeticRingsRedux.Framework.Rings
{
    internal sealed class FireflyRing : CustomRing
    {
        private readonly List<FireflyFollower> fireflies = new List<FireflyFollower>();

        internal override Ring RingObject { get; }

        internal FireflyRing(Ring pairedRing)
        {
            RingObject = pairedRing;
        }

        internal override void HandleEquip(Farmer who, GameLocation location)
        {
            EnsureCritters(location);
            EnsureFollowers(who);
            AddFollowers(location, who);
        }

        internal override void HandleUnequip(Farmer who, GameLocation location)
        {
            RemoveFollowers(location);
            fireflies.Clear();
        }

        internal override void HandleNewLocation(Farmer who, GameLocation location)
        {
            EnsureCritters(location);
            EnsureFollowers(who);
            AddFollowers(location, who);
        }

        internal override void HandleLeaveLocation(Farmer who, GameLocation location)
        {
            RemoveFollowers(location);
        }

        internal override void Update(Farmer who, GameLocation location)
        {
        }

        private void EnsureFollowers(Farmer who)
        {
            if (fireflies.Count > 0)
                return;

            int count = Game1.random.Next(1, 4);
            for (int index = 0; index < count; index++)
                fireflies.Add(new FireflyFollower(who.Tile));
        }

        private void AddFollowers(GameLocation location, Farmer who)
        {
            foreach (FireflyFollower firefly in fireflies)
            {
                firefly.ResetForNewLocation(who.Tile, location);
                if (!location.critters.Contains(firefly))
                    location.critters.Add(firefly);
            }
        }

        private void RemoveFollowers(GameLocation location)
        {
            foreach (FireflyFollower firefly in fireflies)
            {
                firefly.DetachLight(location);
                location?.critters?.Remove(firefly);
            }
        }

        private static void EnsureCritters(GameLocation location)
        {
            location.critters ??= new List<Critter>();
        }
    }
}
