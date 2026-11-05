using System;

namespace random;

/// <summary>
/// Supplies uniform random bits and the mathematical mode used to transform them.
/// </summary>
/// <remarks>
/// Distribution and sampling APIs depend on this small contract rather than generator
/// lifecycle, state persistence, or a particular engine. Implementations are mutable and
/// are expected to return unbiased values across each method's complete output domain.
/// </remarks>
public interface IRandomSource
{
    /// <summary>Returns the mathematical-kernel mode used by transformations.</summary>
    /// <returns>The portable or system math mode.</returns>
    RandomMathMode MathMode();

    /// <summary>Returns 32 uniformly distributed random bits.</summary>
    /// <returns>A value across the complete unsigned 32-bit domain.</returns>
    uint NextUInt32();

    /// <summary>Returns 64 uniformly distributed random bits.</summary>
    /// <returns>A value across the complete unsigned 64-bit domain.</returns>
    ulong NextUInt64();

    /// <summary>Returns one uniformly distributed random bit.</summary>
    /// <returns><c>true</c> and <c>false</c> with equal probability.</returns>
    bool NextBoolean();

    /// <summary>Fills a destination with uniformly distributed random bytes.</summary>
    /// <param name="destination">The destination bytes.</param>
    void FillBytes(Span<byte> destination);
}
