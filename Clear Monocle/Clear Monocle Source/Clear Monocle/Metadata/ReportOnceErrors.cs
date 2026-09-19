using System;

namespace ThaleMagnus.ClearMonocle.Metadata;

[Flags]
internal enum ReportOnceErrors : uint {
    OverlappingSource = 1U << 0,
    InvertedSource = 1U << 1,
    DegenerateSource = 1U << 2,
}
