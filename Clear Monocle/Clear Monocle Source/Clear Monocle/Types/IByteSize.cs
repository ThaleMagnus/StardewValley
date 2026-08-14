using System.Diagnostics.Contracts;

namespace ThaleTheGreat.ClearMonocle.Types;

internal interface IByteSize {
    [Pure]
    long SizeBytes { get; }
}
