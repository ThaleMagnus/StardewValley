using System.Diagnostics.Contracts;

namespace ThaleMagnus.ClearMonocle.Types;

internal interface IByteSize {
    [Pure]
    long SizeBytes { get; }
}
