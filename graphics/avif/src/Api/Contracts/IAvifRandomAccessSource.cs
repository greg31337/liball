using System;

namespace avif;

/// <summary>Supplies bounded exact byte ranges from a caller-owned random-access source.</summary>
/// <remarks>
/// A successful read fills the complete destination and reports its length. A temporarily
/// unavailable read reports <c>NeedMoreData</c> and the exact positive missing subrange without
/// partially committing bytes. The library never closes the source.
/// </remarks>
public interface IAvifRandomAccessSource
{
    /// <summary>Returns the complete nonnegative source length.</summary>
    /// <returns>The total source byte count.</returns>
    long GetLength();

    /// <summary>Fulfills one exact absolute byte-range request.</summary>
    /// <param name="offset">The nonnegative absolute source offset.</param>
    /// <param name="destination">The destination that must be filled atomically on success.</param>
    /// <returns>The exact-read outcome and any unavailable subrange.</returns>
    AvifRandomAccessReadResult? ReadRange(
        long offset,
        Span<byte> destination);
}
