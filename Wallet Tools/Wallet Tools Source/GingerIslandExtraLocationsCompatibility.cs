using System;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Tools;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;

namespace ThaleMagnus.WalletTools;

internal static class GingerIslandExtraLocationsCompatibility
{
    private const string TargetModUniqueId = "mistyspring.GiEXredux.code";
    private const string TargetModName = "Ginger Island Extra Locations - redux";
    private const string TargetTypeName = "ExtraGIMapsRedux.Patches.ToolPatches";
    private const string TargetMethodName = "Pre_CreateToolInstance";

    internal static void Apply(Harmony harmony, IModRegistry modRegistry, IMonitor monitor)
    {
        if (!modRegistry.IsLoaded(TargetModUniqueId))
            return;

        Type? targetType = AccessTools.TypeByName(TargetTypeName);
        MethodInfo? targetMethod = targetType is null
            ? null
            : AccessTools.DeclaredMethod(targetType, TargetMethodName);

        if (!IsExpectedTarget(targetMethod))
        {
            monitor.Log(
                $"Could not apply the {TargetModName} compatibility fix because {TargetTypeName}.{TargetMethodName} was missing or its signature had changed.",
                LogLevel.Error
            );
            return;
        }

        MethodInfo? prefix = AccessTools.Method(
            typeof(GingerIslandExtraLocationsCompatibility),
            nameof(BeforeGingerIslandExtraLocationsToolPrefix)
        );

        if (prefix is null)
        {
            monitor.Log($"Could not locate the internal {TargetModName} compatibility prefix.", LogLevel.Error);
            return;
        }

        harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefix));
    }

    private static bool IsExpectedTarget(MethodInfo? method)
    {
        if (method is null || !method.IsStatic || method.ReturnType != typeof(bool))
            return false;

        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Length == 3
            && parameters[0].ParameterType == typeof(ParsedItemData)
            && parameters[1].ParameterType == typeof(ToolData)
            && parameters[2].ParameterType == typeof(Tool).MakeByRefType();
    }

    private static bool BeforeGingerIslandExtraLocationsToolPrefix(ToolData? __1, ref bool __result)
    {
        if (__1?.ClassName is not null)
            return true;

        __result = true;
        return false;
    }
}
