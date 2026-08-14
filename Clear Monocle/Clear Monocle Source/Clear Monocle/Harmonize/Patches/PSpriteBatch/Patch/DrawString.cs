using Microsoft.Xna.Framework.Graphics;
using ThaleTheGreat.ClearMonocle.Configuration;
using System.Text;

namespace ThaleTheGreat.ClearMonocle.Harmonize.Patches.PSpriteBatch.Patch;

internal class DrawString {
    private static bool ShouldBypass(XSpriteBatch batch) {
        DrawState.Activate(batch);

        return
            !Config.IsEnabled ||
            RenderOwnership.IsSuppressed ||
            !Config.Resample.IsEnabled ||
            !DrawState.IsResamplingAllowed(batch);
    }

    // public unsafe void DrawString (SpriteFont spriteFont, string text, XVector2 position, XColor color)
    // public unsafe void DrawString (SpriteFont spriteFont, string text, XVector2 position, XColor color, float rotation, XVector2 origin, XVector2 scale, SpriteEffects effects, float layerDepth)
    // public unsafe void DrawString (SpriteFont spriteFont, StringBuilder text, XVector2 position, XColor color)
    // public unsafe void DrawString (SpriteFont spriteFont, StringBuilder text, XVector2 position, XColor color, float rotation, XVector2 origin, XVector2 scale, SpriteEffects effects, float layerDepth)

    [Harmonize("DrawString", priority: Harmonize.PriorityLevel.First)]
    public static bool OnDrawString(XSpriteBatch __instance, SpriteFont spriteFont, string text, XVector2 position, XColor color) {
        if (ShouldBypass(__instance)) {
            return true;
        }

        __instance.DrawString(
            spriteFont: spriteFont,
            text: text,
            position: position,
            color: color,
            rotation: 0.0f,
            origin: XVector2.Zero,
            scale: XVector2.One,
            effects: SpriteEffects.None,
            layerDepth: 0.0f
        );
        return false;
    }

    [Harmonize("DrawString", priority: Harmonize.PriorityLevel.First)]
    public static bool OnDrawString(XSpriteBatch __instance, SpriteFont spriteFont, StringBuilder text, XVector2 position, XColor color) {
        if (ShouldBypass(__instance)) {
            return true;
        }

        __instance.DrawString(
            spriteFont: spriteFont,
            text: text,
            position: position,
            color: color,
            rotation: 0.0f,
            origin: XVector2.Zero,
            scale: XVector2.One,
            effects: SpriteEffects.None,
            layerDepth: 0.0f
        );
        return false;
    }

    [Harmonize("DrawString", priority: Harmonize.PriorityLevel.Last)]
    public static bool OnDrawString(
        XSpriteBatch __instance,
        SpriteFont spriteFont,
        string text,
        XVector2 position,
        XColor color,
        float rotation,
        XVector2 origin,
        XVector2 scale,
        SpriteEffects effects,
        float layerDepth
    ) {
        if (ShouldBypass(__instance)) {
            return true;
        }

        return Core.OnDrawStringImpl.DrawString(
            __instance,
            spriteFont,
            text,
            position,
            color,
            rotation,
            origin,
            scale,
            effects,
            layerDepth
        );
    }

    [Harmonize("DrawString", priority: Harmonize.PriorityLevel.Last)]
    public static bool OnDrawString(
        XSpriteBatch __instance,
        SpriteFont spriteFont,
        StringBuilder text,
        XVector2 position,
        XColor color,
        float rotation,
        XVector2 origin,
        XVector2 scale,
        SpriteEffects effects,
        float layerDepth
    ) {
        if (ShouldBypass(__instance)) {
            return true;
        }

        return Core.OnDrawStringImpl.DrawString(
            __instance,
            spriteFont,
            text,
            position,
            color,
            rotation,
            origin,
            scale,
            effects,
            layerDepth
        );
    }
}
