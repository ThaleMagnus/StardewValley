using Microsoft.Xna.Framework.Graphics;
using ThaleTheGreat.ClearMonocle.GL;

namespace ThaleTheGreat.ClearMonocle.Types;

/// <summary>
/// A Texture2D that represents internal Clear Monocle data, and thus shouldn't continue down any resampling pipelines
/// </summary>
internal class InternalTexture2D : XTexture2D {
    private const string DefaultName = "Texture (Internal)";

    private Texture2DExt.Texture2DOpenGlMeta? InnerGlMeta;

    internal Texture2DExt.Texture2DOpenGlMeta GlMeta {
        get {
            if (InnerGlMeta is not { } meta) {
                InnerGlMeta = meta = Texture2DExt.Texture2DOpenGlMeta.Get(this);
            }

            return meta;
        }
    }

    internal InternalTexture2D(GraphicsDevice graphicsDevice, int width, int height) : base(graphicsDevice, width, height) {
        Name = DefaultName;
    }

    internal InternalTexture2D(GraphicsDevice graphicsDevice, int width, int height, bool mipmap, SurfaceFormat format) : base(graphicsDevice, width, height, mipmap, format) {
        Name = DefaultName;
    }

    internal InternalTexture2D(GraphicsDevice graphicsDevice, int width, int height, bool mipmap, SurfaceFormat format, int arraySize) : base(graphicsDevice, width, height, mipmap, format, arraySize) {
        Name = DefaultName;
    }

}
