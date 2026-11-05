using System;
namespace token;

/// <summary>
/// Provides fixed-width conversion helpers for unsigned big-endian integer byte sequences.
/// </summary>
internal static class BigEndianInteger
{
    /// <summary>
    /// Normalizes an unsigned big-endian integer to an exact byte width.
    /// </summary>
    /// <param name="value">The unsigned big-endian integer bytes.</param>
    /// <param name="width">The required output width in bytes.</param>
    /// <returns>A result containing a zero-padded fixed-width value, or failure details when the value does not fit.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>width</c> is negative.</exception>
    internal static TokenBytesResult ToFixedWidth(byte[] value, int width)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return ToFixedWidth(value.AsSpan(), width);
    }

    /// <summary>
    /// Normalizes an unsigned big-endian integer span to an exact byte width.
    /// </summary>
    /// <param name="value">The unsigned big-endian integer bytes.</param>
    /// <param name="width">The required output width in bytes.</param>
    /// <returns>A result containing a zero-padded fixed-width value, or failure details when the value does not fit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>width</c> is negative.</exception>
    internal static TokenBytesResult ToFixedWidth(ReadOnlySpan<byte> value, int width)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        TokenBytesResult result = new TokenBytesResult();
        int firstSignificant = FindFirstSignificantByte(value);
        int significantLength = value.Length - firstSignificant;

        if (significantLength > width)
        {
            result.Error = new TokenError(
                "integer_too_large",
                "The unsigned integer does not fit in the requested width.",
                firstSignificant);
            return result;
        }

        byte[] normalized = new byte[width];
        value.Slice(firstSignificant).CopyTo(normalized.AsSpan(width - significantLength));
        result.Bytes = normalized;
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Writes an unsigned big-endian integer into an exact-width destination span.
    /// </summary>
    /// <param name="value">The unsigned big-endian integer bytes.</param>
    /// <param name="destination">The destination whose length defines the required width.</param>
    /// <returns>A successful result, or failure details when the value does not fit.</returns>
    internal static TokenResult WriteFixedWidth(ReadOnlySpan<byte> value, Span<byte> destination)
    {
        TokenResult result = new TokenResult();
        int firstSignificant = FindFirstSignificantByte(value);
        int significantLength = value.Length - firstSignificant;

        if (significantLength > destination.Length)
        {
            result.Error = new TokenError(
                "integer_too_large",
                "The unsigned integer does not fit in the destination width.",
                firstSignificant);
            return result;
        }

        destination.Clear();
        value.Slice(firstSignificant).CopyTo(destination.Slice(destination.Length - significantLength));
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Removes redundant leading zero bytes from an unsigned big-endian integer.
    /// </summary>
    /// <param name="value">The unsigned big-endian integer bytes.</param>
    /// <returns>A successful result containing the minimal unsigned representation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    internal static TokenBytesResult ToMinimal(byte[] value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return ToMinimal(value.AsSpan());
    }

    /// <summary>
    /// Removes redundant leading zero bytes from an unsigned big-endian integer span.
    /// </summary>
    /// <param name="value">The unsigned big-endian integer bytes.</param>
    /// <returns>A successful result containing the minimal unsigned representation.</returns>
    internal static TokenBytesResult ToMinimal(ReadOnlySpan<byte> value)
    {
        TokenBytesResult result = new TokenBytesResult();
        int firstSignificant = FindFirstSignificantByte(value);

        if (firstSignificant == value.Length && value.Length > 0)
        {
            firstSignificant = value.Length - 1;
        }

        result.Bytes = value.Slice(firstSignificant).ToArray();
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Encodes an unsigned 64-bit integer using an exact big-endian byte width.
    /// </summary>
    /// <param name="value">The unsigned integer value.</param>
    /// <param name="width">The required output width from zero through eight bytes.</param>
    /// <returns>A result containing the fixed-width encoding, or failure details when the value does not fit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>width</c> is negative or greater than eight.</exception>
    internal static TokenBytesResult FromUInt64(ulong value, int width)
    {
        if (width < 0 || width > 8)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        TokenBytesResult result = new TokenBytesResult();
        if (width < 8 && value >= (1UL << (width * 8)))
        {
            result.Error = new TokenError(
                "integer_too_large",
                "The unsigned integer does not fit in the requested width.",
                -1);
            return result;
        }

        byte[] encoded = new byte[width];
        int index = width - 1;
        while (index >= 0)
        {
            encoded[index] = (byte)(value & 255);
            value >>= 8;
            index -= 1;
        }

        result.Bytes = encoded;
        result.Success = true;
        return result;
    }

    static int FindFirstSignificantByte(ReadOnlySpan<byte> value)
    {
        int index = 0;
        while (index < value.Length && value[index] == 0)
        {
            index += 1;
        }

        return index;
    }
}
