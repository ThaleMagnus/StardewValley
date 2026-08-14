using ThaleTheGreat.ClearMonocle.Resample.Scalers;

namespace ThaleTheGreat.ClearMonocle.Resample;

internal interface IScalerInfo {
    Scaler Scaler { get; }
    int MinScale { get; }
    int MaxScale { get; }
    XGraphics.TextureFilter? Filter { get; }
    bool PremultiplyAlpha { get; }
    bool GammaCorrect { get; }
    bool BlockCompress { get; }

    IScaler Interface { get; }
}
