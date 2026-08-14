using ThaleTheGreat.ClearMonocle.Types;
using ThaleTheGreat.ClearMonocle.Types.Spans;
using System;
using System.Runtime.CompilerServices;

namespace ThaleTheGreat.ClearMonocle.Resample;

internal static class TextureEncode {
    [MethodImpl(Runtime.MethodImpl.Inline)]
    internal static bool Encode(
        ReadOnlySpan<Color8> data,
        ref TextureFormat format,
        Vector2I dimensions,
        bool hasAlpha,
        bool isPunchthroughAlpha,
        bool isMasky,
        bool hasR,
        bool hasG,
        bool hasB,
        out PinnedSpan<byte> result
    ) =>
        Encoder.StbBlockEncoder.Encode(data, ref format, dimensions, hasAlpha, isPunchthroughAlpha, isMasky, hasR, hasG, hasB, out result);
}
