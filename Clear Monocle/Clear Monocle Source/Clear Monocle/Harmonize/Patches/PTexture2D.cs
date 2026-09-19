using CommunityToolkit.HighPerformance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ThaleMagnus.ClearMonocle.Configuration;
using ThaleMagnus.ClearMonocle.Extensions;
using ThaleMagnus.ClearMonocle.Extensions.Reflection;
using ThaleMagnus.ClearMonocle.Metadata;
using ThaleMagnus.ClearMonocle.Types;
using ThaleMagnus.ClearMonocle.Types.Reflection;
using StardewValley;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using static ThaleMagnus.ClearMonocle.Harmonize.Harmonize;

namespace ThaleMagnus.ClearMonocle.Harmonize.Patches;

[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
internal static class PTexture2D {
    #region Cache Handlers

    // https://benbowen.blog/post/fun_with_makeref/

    [MethodImpl(Runtime.MethodImpl.Inline)]
    private static bool Cacheable(XTexture2D texture) => texture.LevelCount <= 1;

    private static unsafe void SetDataPurge<T>(
        XTexture2D texture,
        XRectangle? rect,
        ReadOnlySpan<T> data,
        int startIndex,
        int elementCount,
        bool animated
    ) where T : unmanaged {
        if (!ManagedSpriteInstance.Validate(texture, clean: true)) {
            return;
        }

        if (texture.Format.IsBlock()) {
            ManagedSpriteInstance.FullPurge(texture, animated: animated);
            return;
        }

        var byteData = Cacheable(texture) ? data : default;

        var span = byteData.IsEmpty ? default : byteData.Slice(startIndex, elementCount).AsBytes();

        ManagedSpriteInstance.Purge(
            reference: texture,
            bounds: rect,
            data: new(span),
            animated: animated
        );
    }

    #endregion

    #region SetData

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Span<T> ThrowArgumentOutOfRangeLessThanException<T>(string name, int value, int constraint) =>
        throw new ArgumentOutOfRangeException(name, $"{value} < {constraint}");

    internal static unsafe Span<T> GetCachedData<T>(
        XTexture2D __instance,
        int level,
        int arraySlice,
        Bounds rect,
        Span<T> data,
        int startIndex = 0,
        int? elementCount = null
    ) where T : unmanaged {
        // While we do technically cache block data, the internal routines for updating them don't work well
        // and thus we assume that purges are required. We do not presently have the ability to handle this
        // sanely.
        if (__instance.Format.IsBlock()) {
            return default;
        }

        if (level != 0 || arraySlice != 0) {
            return default;
        }

        try {
            if (__instance.TryMeta(out var sourceMeta) && sourceMeta.HasCachedData && sourceMeta.CachedData is { } cachedSourceData) {
                int numElements;
                int byteSize;
                if (elementCount.HasValue) {
                    numElements = elementCount.Value;
                    byteSize = numElements * sizeof(T);
                }
                else {
                    byteSize = __instance.Format.SizeBytes(rect.Area);
                    numElements = byteSize / sizeof(T);
                }

                if (numElements == 0) {
                    return default;
                }

                if (cachedSourceData.Length < byteSize) {
                    return ThrowArgumentOutOfRangeLessThanException<T>(nameof(numElements), cachedSourceData.Length, byteSize);
                }

                if (data.IsEmpty && startIndex == 0) {
                    data = SpanExt.Make<T>(numElements);
                }

                if (data.Length < numElements + startIndex) {
                    return ThrowArgumentOutOfRangeLessThanException<T>(nameof(data), data.Length, numElements + startIndex);
                }

                if (rect == __instance.Bounds) {
                    ReadOnlySpan<byte> sourceBytes = cachedSourceData;
                    var source = sourceBytes.Cast<T>();
                    source.CopyTo(data, 0, startIndex, numElements);

                    return data;
                }
                if (__instance.Bounds.Contains(rect)) {
                    if (typeof(T) == typeof(XColor)) {
                        var cachedData = cachedSourceData.AsReadOnlySpan().Cast<byte, XColor>();
                        var destData = data.Cast<T, XColor>();
                        int sourceStride = __instance.Width;
                        int destStride = rect.Width;
                        int sourceOffset = (rect.Top * sourceStride) + rect.Left;
                        int destOffset = startIndex;
                        for (int y = 0; y < rect.Height; ++y) {
                            cachedData.Slice(sourceOffset, destStride).CopyTo(destData.Slice(destOffset, destStride));
                            sourceOffset += sourceStride;
                            destOffset += destStride;
                        }

                        return data;
                    }

                    if (typeof(T) == typeof(byte)) {
                        var cachedData = cachedSourceData.AsReadOnlySpan();
                        var destData = data.Cast<T, byte>();
                        int bytesPerTexel = __instance.Format.SizeBytes(1);
                        int sourceStride = __instance.Format.SizeBytes(__instance.Width);
                        int destStride = __instance.Format.SizeBytes(rect.Width);
                        int sourceOffset = (rect.Top * sourceStride) + (rect.Left * bytesPerTexel);
                        int destOffset = startIndex;
                        for (int y = 0; y < rect.Height; ++y) {
                            cachedData.Slice(sourceOffset, destStride).CopyTo(destData.Slice(destOffset, destStride));
                            sourceOffset += sourceStride;
                            destOffset += destStride;
                        }

                        return data;
                    }
                }
            }
        }
        catch (Exception ex) {
            Debug.Error("OnGetData optimization threw an exception", ex);
        }

        return default;
    }

    #endregion

    private static Assembly? CpaAssembly;
    private static Assembly? GetCpaAssembly() => CpaAssembly ??= AssemblyExt.GetAssembly("ContentPatcherAnimations");
    private static readonly ConditionalWeakTable<XTexture2D, ConcurrentDictionary<Bounds, bool>> ContentPatcherAnimationCache = new();

    private static readonly VariableAccessor<StackTrace, StackFrame[]?>? GetStackFramesDelegate =
        typeof(StackTrace).GetInstanceVariable("_stackFrames")?.GetAccessor<StackTrace, StackFrame[]?>();
    private static readonly VariableAccessor<StackTrace, int>? GetNumFramesDelegate =
        typeof(StackTrace).GetInstanceVariable("_numOfFrames")?.GetAccessor<StackTrace, int>();

    [MemberNotNullWhen(true, "GetStackFramesDelegate", "GetNumFramesDelegate")]
    private static bool HasStackTraceDelegates { get; } =
        (GetStackFramesDelegate?.HasGetter ?? false) &&
        (GetNumFramesDelegate?.HasGetter ?? false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<StackFrame> GetStackFrames(int skipFrames) {
        var trace = new StackTrace(skipFrames: skipFrames, fNeedFileInfo: false);

        if (HasStackTraceDelegates) {
            ReadOnlySpan<StackFrame> stackFrames = GetStackFramesDelegate.Get(trace) ?? default;
            if (!stackFrames.IsEmpty) {
                int numFrames = Math.Min(GetNumFramesDelegate.Get(trace), stackFrames.Length);
                return stackFrames.Slice(0, numFrames);
            }
        }

        return trace.GetFrames();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsFromContentPatcherAnimations(XTexture2D instance, Bounds bounds) {
        var cpaAssembly = GetCpaAssembly();
        if (cpaAssembly is null) {
            return false;
        }

        if (instance.TryMeta(out var meta) && meta.IsAnyAnimated(instance, bounds)) {
            return true;
        }

        if (!Config.Extras.ContentPatcherAnimationsStackTraceDetection) {
            return false;
        }

        var cache = ContentPatcherAnimationCache.GetOrCreateValue(instance);
        if (cache.TryGetValue(bounds, out bool cached)) {
            return cached;
        }

        bool result = false;
        var stackFrames = GetStackFrames(skipFrames: 2);

        foreach (var frame in stackFrames) {
            if (ReferenceEquals(frame.GetMethod()?.DeclaringType?.Assembly, cpaAssembly)) {
                result = true;
                break;
            }
        }

        cache.TryAdd(bounds, result);
        return result;
    }

    [Harmonize(
        type: typeof(XTexture2D),
        "SetData",
        fixation: Fixation.Postfix,
        generic: Generic.Struct,
        argumentTypes: new[] { typeof(Array) }
    )]
    public static void OnSetDataPost<T>(
        XTexture2D __instance,
        T[] data
    ) where T : unmanaged {
        OnSetDataPostInternal(
            __instance: __instance,
            level: 0,
            arraySlice: 0,
            rect: __instance.Bounds(),
            data: data.AsReadOnlySpan(),
            startIndex: 0,
            elementCount: data.Length
        );
    }

    [Harmonize(
        type: typeof(XTexture2D),
        "SetData",
        fixation: Fixation.Postfix,
        generic: Generic.Struct,
        argumentTypes: new[] { typeof(Array), typeof(int), typeof(int) }
    )]
    public static void OnSetDataPost<T>(
        XTexture2D __instance,
        T[] data,
        int startIndex,
        int elementCount
    ) where T : unmanaged {
        OnSetDataPostInternal(
            __instance: __instance,
            level: 0,
            arraySlice: 0,
            rect: __instance.Bounds(),
            data: data.AsReadOnlySpan(),
            startIndex: startIndex,
            elementCount: elementCount
        );
    }

    [Harmonize(
        type: typeof(XTexture2D),
        "SetData",
        fixation: Fixation.Postfix,
        generic: Generic.Struct,
        argumentTypes: new[] { typeof(int), typeof(XRectangle?), typeof(Array), typeof(int), typeof(int) }
    )]
    public static void OnSetDataPost<T>(
        XTexture2D __instance,
        int level,
        XRectangle? rect,
        T[] data,
        int startIndex,
        int elementCount
    ) where T : unmanaged {
        OnSetDataPostInternal(
            __instance: __instance,
            level: level,
            arraySlice: 0,
            rect: rect ?? __instance.Bounds(),
            data: data.AsReadOnlySpan(),
            startIndex: startIndex,
            elementCount: elementCount
        );
    }

    [Harmonize(
        type: typeof(XTexture2D),
        "SetData",
        fixation: Fixation.Postfix,
        generic: Generic.Struct,
        argumentTypes: new[] { typeof(int), typeof(int), typeof(XRectangle?), typeof(Array), typeof(int), typeof(int) }
    )]
    public static void OnSetDataPost<T>(
        XTexture2D __instance,
        int level,
        int arraySlice,
        XRectangle? rect,
        T[] data,
        int startIndex,
        int elementCount
    ) where T : unmanaged {
        OnSetDataPostInternal(
            __instance: __instance,
            level: level,
            arraySlice: arraySlice,
            rect: rect ?? __instance.Bounds(),
            data: data.AsReadOnlySpan(),
            startIndex: startIndex,
            elementCount: elementCount
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void OnSetDataPostInternal<T>(
        XTexture2D __instance,
        int level,
        int arraySlice,
        Bounds rect,
        ReadOnlySpan<T> data,
        int startIndex = 0,
        int? elementCount = null
    ) where T : unmanaged {
        if (
            RenderOwnership.IsSuppressed ||
            __instance is (ManagedTexture2D or InternalTexture2D or RenderTarget2D)
        ) {
            return;
        }

        __instance.Meta().IncrementRevision();

        SetDataPurge(
            __instance,
            rect,
            data,
            startIndex,
            elementCount ?? (__instance.Format.SizeBytes(rect.Area) / sizeof(T)),
            animated: IsFromContentPatcherAnimations(__instance, rect)
        );
    }
}
