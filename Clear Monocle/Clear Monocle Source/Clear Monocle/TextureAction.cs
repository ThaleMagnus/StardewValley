using ThaleTheGreat.ClearMonocle.Types;
using System.Runtime.InteropServices;

namespace ThaleTheGreat.ClearMonocle;

[StructLayout(LayoutKind.Auto)]
internal record struct TextureAction(string Name, int Size, ComparableWeakReference<XTexture2D> Reference, Bounds Bounds);
