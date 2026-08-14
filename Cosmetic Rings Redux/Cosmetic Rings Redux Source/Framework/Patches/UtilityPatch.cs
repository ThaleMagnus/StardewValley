using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using System;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Patches
{
    internal sealed class UtilityPatch
    {
        internal void Apply(Harmony harmony)
        {
            var target = AccessTools.Method(
                typeof(Utility),
                nameof(Utility.isThereAFarmerOrCharacterWithinDistance),
                new[] { typeof(Vector2), typeof(int), typeof(GameLocation) }
            ) ?? throw new MissingMethodException(typeof(Utility).FullName, nameof(Utility.isThereAFarmerOrCharacterWithinDistance));

            var postfix = AccessTools.Method(typeof(UtilityPatch), nameof(Postfix))
                ?? throw new MissingMethodException(typeof(UtilityPatch).FullName, nameof(Postfix));

            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        private static void Postfix(ref Character __result, GameLocation environment)
        {
            if (environment is Town && ModEntry.IsCustomFollower(__result))
                __result = null;
        }
    }
}
