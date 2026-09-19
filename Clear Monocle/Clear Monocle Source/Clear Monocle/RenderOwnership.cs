using System;

namespace ThaleMagnus.ClearMonocle;

internal static class RenderOwnership {
    [ThreadStatic]
    private static int SuppressionDepth;

    internal static bool IsSuppressed => SuppressionDepth > 0;

    internal static int EnterSuppressedScope() {
        int previousDepth = SuppressionDepth;
        SuppressionDepth = previousDepth + 1;
        return previousDepth;
    }

    internal static void RestoreSuppressionDepth(int previousDepth) {
        SuppressionDepth = Math.Max(0, previousDepth);
    }
}
