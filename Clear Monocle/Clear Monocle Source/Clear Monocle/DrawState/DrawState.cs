using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ThaleTheGreat.ClearMonocle.Configuration;
using ThaleTheGreat.ClearMonocle.Extensions;
using ThaleTheGreat.ClearMonocle.Extensions.Reflection;
using ThaleTheGreat.ClearMonocle.Tasking;
using ThaleTheGreat.ClearMonocle.Types;
using ThaleTheGreat.ClearMonocle.Types.Interlocking;
using StardewValley;
using System;
using System.Runtime.CompilerServices;

namespace ThaleTheGreat.ClearMonocle;

internal static partial class DrawState {
    private static class UpdateState {
        internal static readonly InterlockedULong LastUpdated = 0UL;

        // ReSharper disable once MemberHidesStaticFromOuterClass
        internal static volatile bool IsUpdatedThisFrame = false;
    }

    internal static bool IsUpdatedThisFrame {
        get => UpdateState.IsUpdatedThisFrame;
        set {
            if (value) {
                UpdateState.LastUpdated.Set(CurrentFrame);
            }
            UpdateState.IsUpdatedThisFrame = value;
        }
    }

    internal static InterlockedULong CurrentFrame = 0UL;

    private static class Defaults {
        internal static readonly SamplerState SamplerState = SamplerState.LinearClamp;
        internal static readonly BlendState BlendState = BlendState.AlphaBlend;
        internal static readonly RasterizerState RasterizerState = RasterizerState.CullCounterClockwise;
        internal const SpriteSortMode SortMode = SpriteSortMode.Deferred;
    }

    private static SamplerState MakeSamplerState(SamplerState reference, TextureAddressMode addressMode) {
        if (reference.AddressU == addressMode && reference.AddressV == addressMode) {
            return reference;
        }

        var state = reference.Clone();
        state.AddressU = state.AddressV = addressMode;
        return state;
    }

    internal static class SamplerStateExt {
        internal static readonly Lazy<SamplerState> AnisotropicBorder = new(() => MakeSamplerState(SamplerState.AnisotropicClamp, TextureAddressMode.Border));
        internal static readonly Lazy<SamplerState> LinearBorder = new(() => MakeSamplerState(SamplerState.LinearClamp, TextureAddressMode.Border));
        internal static readonly Lazy<SamplerState> PointBorder = new(() => MakeSamplerState(SamplerState.PointClamp, TextureAddressMode.Border));
        internal static readonly Lazy<SamplerState> AnisotropicMirror = new(() => MakeSamplerState(SamplerState.AnisotropicClamp, TextureAddressMode.Mirror));
        internal static readonly Lazy<SamplerState> LinearMirror = new(() => MakeSamplerState(SamplerState.LinearClamp, TextureAddressMode.Mirror));
        internal static readonly Lazy<SamplerState> PointMirror = new(() => MakeSamplerState(SamplerState.PointClamp, TextureAddressMode.Mirror));
    }

    private sealed class BatchRenderState {
        internal SamplerState SamplerState = Defaults.SamplerState;
        internal BlendState BlendState = Defaults.BlendState;
        internal RasterizerState RasterizerState = Defaults.RasterizerState;
        internal SpriteSortMode SortMode = Defaults.SortMode;
        internal bool ResamplingAllowed = false;
    }

    private static readonly ConditionalWeakTable<XSpriteBatch, BatchRenderState> BatchStates = new();
    private static readonly BatchRenderState FallbackBatchState = new();

    [ThreadStatic]
    private static XSpriteBatch? ActiveBatch;

    private static BatchRenderState GetBatchState(XSpriteBatch batch) =>
        BatchStates.GetValue(batch, static _ => new BatchRenderState());

    private static BatchRenderState CurrentBatchState =>
        ActiveBatch is { } batch ? GetBatchState(batch) : FallbackBatchState;

    internal static SamplerState CurrentSamplerState => CurrentBatchState.SamplerState;
    internal static BlendState CurrentBlendState => CurrentBatchState.BlendState;
    internal static RasterizerState CurrentRasterizerState => CurrentBatchState.RasterizerState;
    internal static SpriteSortMode CurrentSortMode => CurrentBatchState.SortMode;

    [MethodImpl(Runtime.MethodImpl.Inline)]
    internal static void Activate(XSpriteBatch batch) => ActiveBatch = batch;

    [MethodImpl(Runtime.MethodImpl.Inline)]
    internal static bool IsResamplingAllowed(XSpriteBatch batch) =>
        !RenderOwnership.IsSuppressed && GetBatchState(batch).ResamplingAllowed;

    internal static readonly Condition TriggerCollection = new(initialState: false);

    private static TimeSpan ExpectedFrameTime = GameConstants.FrameTime.TimeSpan;
    internal static bool ForceSynchronous = false;

    private static int LastFrameTimesIndex = 0;
    private static TimeSpan[] LastFrameTimesCPU = new TimeSpan[64];
    private static TimeSpan[] LastFrameTimesTotal = new TimeSpan[64];
    private static readonly System.Diagnostics.Stopwatch FrameStopwatch = System.Diagnostics.Stopwatch.StartNew();
    private static readonly System.Diagnostics.Stopwatch RealFrameStopwatch = System.Diagnostics.Stopwatch.StartNew();

    private const int BaselineFrameTimeRunningCount = 20;
    private static TimeSpan BaselineFrameTime = TimeSpan.Zero;

    internal static class Statistics {
        internal static uint DrawCalls = 0u;

        internal static void Reset() {
            DrawCalls = 0u;
        }
    }

    internal static void UpdateDeviceManager(GraphicsDeviceManager manager) {
        var rate = manager.GetField("game")?.GetProperty<TimeSpan>("TargetElapsedTime");
        ExpectedFrameTime = rate.GetValueOrDefault(ExpectedFrameTime);
    }

    private static GraphicsDevice? PreviousDevice = null;
    internal static GraphicsDevice Device {
        get {
            UpdateDevice();
            return PreviousDevice!;
        }
    }
    internal static void UpdateDevice() {
        var currentDevice = Game1.graphics.GraphicsDevice;
        if (currentDevice != PreviousDevice) {
            PreviousDevice = currentDevice;
        }
    }

    internal static bool PushedUpdateWithin(int frames) => (long)(CurrentFrame - UpdateState.LastUpdated) <= frames;

    [MethodImpl(Runtime.MethodImpl.Inline)]
    internal static TimeSpan RemainingFrameTime(float multiplier = 1.0f, TimeSpan? offset = null) {
        var actualRemainingTime = ActualRemainingFrameTime();
        return (actualRemainingTime - (BaselineFrameTime + (offset ?? TimeSpan.Zero))).Multiply(multiplier);
    }

    [MethodImpl(Runtime.MethodImpl.Inline)]
    private static TimeSpan ActualRemainingFrameTime() => ExpectedFrameTime - FrameStopwatch.Elapsed;

    internal static void OnBeginDraw() {
        RealFrameStopwatch.Restart();
    }

    internal static void OnPresent() {
        using var watchdogScoped = WatchDog.WatchDog.ScopedWorkingState;

        TimeSpan? lastFrameTimeCPU = null;
        TimeSpan? lastFrameTimeTotal = null;

        if (Config.Debug.DisplayFrameTime) {
            {
                TimeSpan sumTime = TimeSpan.Zero;
                foreach (var frameTime in LastFrameTimesCPU) {
                    sumTime += frameTime;
                }

                sumTime /= LastFrameTimesCPU.Length;
                lastFrameTimeCPU = sumTime;
            }
            {
                TimeSpan sumTime = TimeSpan.Zero;
                foreach (var frameTime in LastFrameTimesTotal) {
                    sumTime += frameTime;
                }

                sumTime /= LastFrameTimesTotal.Length;
                lastFrameTimeTotal = sumTime;
            }
        }

        Debug.Mode.Draw(lastFrameTimeCPU, lastFrameTimeTotal);

        ++CurrentFrame;

        if (TriggerCollection.GetAndClear()) {
            ManagedSpriteInstance.PurgeTextures((Config.Garbage.RequiredFreeMemorySoft * Config.Garbage.RequiredFreeMemoryHysteresis).NearestLong());
            Garbage.Collect(compact: false, blocking: false, background: true);
        }

        if (Config.AsyncScaling.CanFetchAndLoadSameFrame || !IsUpdatedThisFrame) {
            var remaining = ActualRemainingFrameTime();
            SynchronizedTaskScheduler.Instance.Dispatch(remaining);
        }

        if (!IsUpdatedThisFrame) {
            var duration = FrameStopwatch.Elapsed;
            // Throw out garbage values.
            if (duration <= ExpectedFrameTime + ExpectedFrameTime) {
                var mean = BaselineFrameTime;
                mean -= mean / BaselineFrameTimeRunningCount;
                mean += duration / BaselineFrameTimeRunningCount;
                BaselineFrameTime = mean;

                // TODO : fix me, this doesn't work particularly well so I've disabled it.
                BaselineFrameTime = TimeSpan.Zero;
            }
        }
        else {
            IsUpdatedThisFrame = false;
        }

        if (Config.Debug.DisplayFrameTime) {
            int index = LastFrameTimesIndex++;
            LastFrameTimesIndex %= LastFrameTimesCPU.Length;
            LastFrameTimesCPU[index] = RealFrameStopwatch.Elapsed;
            LastFrameTimesTotal[index] = FrameStopwatch.Elapsed;
        }

        if (!Config.IsEnabled) {
            return;
        }

        Garbage.EphemeralCollection.Collect(CurrentFrame);
    }

    [MethodImpl(Runtime.MethodImpl.Inline)]
    internal static void OnPresentPost() {
        FrameStopwatch.Restart();

        using var watchdogScoped = WatchDog.WatchDog.ScopedWorkingState;

        Core.OnDrawImpl.ResetLastDrawCache();

        Statistics.Reset();
    }

    private static bool FirstDraw = true;

    internal static void OnBegin(
        XSpriteBatch @this,
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState,
        Effect? effect,
        in Matrix transformMatrix
    ) {
        using var watchdogScoped = WatchDog.WatchDog.ScopedWorkingState;

        if (FirstDraw) {
            ModEntry.Self.OnFirstDraw();
            FirstDraw = false;
        }

        var state = GetBatchState(@this);
        state.SortMode = sortMode;
        state.SamplerState = samplerState ?? Defaults.SamplerState;
        state.BlendState = blendState ?? Defaults.BlendState;
        state.RasterizerState = rasterizerState ?? Defaults.RasterizerState;
        Activate(@this);
        CheckStates();

        var renderTargets = @this.GraphicsDevice.GetRenderTargets();
        var currentRenderTarget = renderTargets.Length != 0 ? renderTargets[0].RenderTarget as RenderTarget2D : null;

        bool isKnownGameTarget =
            currentRenderTarget is null ||
            ReferenceEquals(currentRenderTarget, Game1.game1.screen) ||
            ReferenceEquals(currentRenderTarget, Game1.game1.uiScreen);

        state.ResamplingAllowed = effect is null && isKnownGameTarget;
        ForceSynchronous = false;
    }
}
