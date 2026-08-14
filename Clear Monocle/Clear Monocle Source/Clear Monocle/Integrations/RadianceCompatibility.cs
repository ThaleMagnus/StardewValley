using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ThaleTheGreat.ClearMonocle.Integrations;

internal static class RadianceCompatibility {
    private const string RadianceUniqueId = "phuicmt.SDVRadiance";
    private const string RadianceModEntryTypeName = "SDVRadiance.ModEntry";

    private static readonly (string MethodName, Type EventArgsType)[] RenderHandlers = {
        ("OnRenderingWorld", typeof(RenderingWorldEventArgs)),
        ("OnRenderingStep", typeof(RenderingStepEventArgs)),
        ("OnRenderedWorld", typeof(RenderedWorldEventArgs))
    };

    private static bool Applied;

    internal static void Apply(Harmony harmony, IModRegistry modRegistry) {
        if (Applied || !modRegistry.IsLoaded(RadianceUniqueId)) {
            return;
        }

        try {
            Type? modEntryType = AccessTools.TypeByName(RadianceModEntryTypeName);
            if (modEntryType is null) {
                LogCompatibilityFailure("compatibility.radiance.type-missing");
                return;
            }

            var targets = new List<MethodInfo>(RenderHandlers.Length);
            foreach ((string methodName, Type eventArgsType) in RenderHandlers) {
                MethodInfo? target = AccessTools.Method(
                    modEntryType,
                    methodName,
                    new[] { typeof(object), eventArgsType }
                );
                if (target is null) {
                    LogCompatibilityFailure(
                        "compatibility.radiance.method-missing",
                        new { type = RadianceModEntryTypeName, method = methodName }
                    );
                    return;
                }

                targets.Add(target);
            }

            MethodInfo prefix = AccessTools.Method(typeof(RadianceCompatibility), nameof(BeforeRadianceRender))
                ?? throw new MissingMethodException(typeof(RadianceCompatibility).FullName, nameof(BeforeRadianceRender));
            MethodInfo finalizer = AccessTools.Method(typeof(RadianceCompatibility), nameof(AfterRadianceRender))
                ?? throw new MissingMethodException(typeof(RadianceCompatibility).FullName, nameof(AfterRadianceRender));

            var prefixPatch = new HarmonyMethod(prefix) { priority = Priority.First };
            var finalizerPatch = new HarmonyMethod(finalizer) { priority = Priority.Last };

            foreach (MethodInfo target in targets) {
                harmony.Patch(
                    original: target,
                    prefix: prefixPatch,
                    finalizer: finalizerPatch
                );
            }

            Applied = true;
        }
        catch (Exception ex) {
            LogCompatibilityFailure("compatibility.radiance.patch-failed", exception: ex);
        }
    }

    private static void LogCompatibilityFailure(string key, object? tokens = null, Exception? exception = null) {
        string message = tokens is null
            ? ModEntry.Self.Helper.Translation.Get(key).ToString()
            : ModEntry.Self.Helper.Translation.Get(key, tokens).ToString();

        if (exception is null) {
            Debug.Warning(message);
        }
        else {
            Debug.Warning(message, exception);
        }
    }

    private static void BeforeRadianceRender(out int __state) {
        __state = RenderOwnership.EnterSuppressedScope();
    }

    private static Exception? AfterRadianceRender(Exception? __exception, int __state) {
        RenderOwnership.RestoreSuppressionDepth(__state);
        return __exception;
    }
}
