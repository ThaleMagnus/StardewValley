using HarmonyLib;
using StardewValley;
using StardewValley.Objects;
using System;

namespace ThaleTheGreat.CosmeticRingsRedux.Framework.Patches
{
    internal sealed class RingPatch
    {
        internal void Apply(Harmony harmony)
        {
            Patch(harmony, nameof(Ring.onEquip), new[] { typeof(Farmer) }, nameof(OnEquipPostfix));
            Patch(harmony, nameof(Ring.onUnequip), new[] { typeof(Farmer) }, nameof(OnUnequipPostfix));
            Patch(harmony, nameof(Ring.onNewLocation), new[] { typeof(Farmer), typeof(GameLocation) }, nameof(OnNewLocationPostfix));
            Patch(harmony, nameof(Ring.onLeaveLocation), new[] { typeof(Farmer), typeof(GameLocation) }, nameof(OnLeaveLocationPostfix));
        }

        private static void Patch(Harmony harmony, string methodName, Type[] argumentTypes, string postfixName)
        {
            var target = AccessTools.Method(typeof(Ring), methodName, argumentTypes)
                ?? throw new MissingMethodException(typeof(Ring).FullName, methodName);
            var postfix = AccessTools.Method(typeof(RingPatch), postfixName)
                ?? throw new MissingMethodException(typeof(RingPatch).FullName, postfixName);

            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        private static void OnEquipPostfix(Ring __instance, Farmer who)
        {
            if (RingManager.IsCosmeticRing(__instance) && who?.currentLocation != null)
                RingManager.HandleEquip(who, who.currentLocation, __instance);
        }

        private static void OnUnequipPostfix(Ring __instance, Farmer who)
        {
            if (RingManager.IsCosmeticRing(__instance))
                RingManager.HandleUnequip(who, who?.currentLocation, __instance);
        }

        private static void OnNewLocationPostfix(Ring __instance, Farmer who, GameLocation environment)
        {
            if (RingManager.IsCosmeticRing(__instance))
                RingManager.HandleNewLocation(who, environment, __instance);
        }

        private static void OnLeaveLocationPostfix(Ring __instance, Farmer who, GameLocation environment)
        {
            if (RingManager.IsCosmeticRing(__instance))
                RingManager.HandleLeaveLocation(who, environment, __instance);
        }
    }
}
