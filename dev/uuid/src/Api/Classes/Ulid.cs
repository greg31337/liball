using System;

namespace uuid;

/// <summary>Creates, parses, formats, and validates 128-bit Universally Unique Lexicographically Sortable Identifiers.</summary>
/// <remarks>A ULID stores a 48-bit Unix-millisecond timestamp and 80 random bits. Canonical text is 26 uppercase Crockford Base32 characters; lowercase input is accepted.</remarks>
public class Ulid : IUlid
{
    internal static readonly UlidGeneratorState MonotonicState = new UlidGeneratorState();

    /// <summary>Creates a ULID for the current UTC millisecond with new random data.</summary>
    /// <returns>A new ULID value.</returns>
    public UlidValue New()
    {
        return UlidGenerator.New();
    }

    /// <summary>Creates a ULID for a supplied instant with new random data.</summary>
    /// <param name="time">The instant whose Unix millisecond is stored.</param>
    /// <returns>A new ULID value.</returns>
    /// <exception cref="IdentifierException">The timestamp is negative or exceeds the 48-bit ULID range.</exception>
    public UlidValue NewAt(DateTimeOffset time)
    {
        ThrowIfInvalidTimestamp(UnixTimeTools.ToUnixMilliseconds(time));
        return UlidGenerator.NewAt(time);
    }

    /// <summary>Creates a ULID for a supplied Unix millisecond with new random data.</summary>
    /// <param name="unixMilliseconds">Milliseconds since the Unix epoch.</param>
    /// <returns>A new ULID value.</returns>
    /// <exception cref="IdentifierException">The timestamp is negative or exceeds 48 bits.</exception>
    public UlidValue NewAtUnixMilliseconds(long unixMilliseconds)
    {
        ThrowIfInvalidTimestamp(unixMilliseconds);
        return UlidGenerator.NewAtUnixMilliseconds(unixMilliseconds);
    }

    /// <summary>Creates a process-wide monotonic ULID using the current UTC millisecond.</summary>
    /// <returns>A value greater than the previous monotonic value.</returns>
    /// <remarks>Within one millisecond the 80-bit random field is incremented. A backward clock or random-field overflow throws; monotonic state is process-local and is not persisted.</remarks>
    /// <exception cref="IdentifierException">The clock moved backwards or the 80-bit random field overflowed.</exception>
    public UlidValue NewMonotonic()
    {
        return MonotonicState.Next();
    }

    /// <summary>Interprets 16 UUID-compatible network-order bytes as a ULID.</summary>
    /// <param name="bytes">Exactly 16 bytes to copy.</param>
    /// <returns>A ULID with the same raw bits.</returns>
    /// <exception cref="ArgumentNullException">The byte array is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The byte array does not contain exactly 16 bytes.</exception>
    public UlidValue FromUuidBytes(byte[]? bytes)
    {
        return UlidValue.FromUuidBytes(bytes);
    }

    /// <summary>Returns a copy of a ULID's 16 bytes for UUID-compatible storage.</summary>
    /// <param name="value">The ULID to copy.</param>
    /// <returns>A new 16-byte array.</returns>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public byte[] ToUuidBytes(UlidValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return value.ToUuidBytes();
    }

    /// <summary>Parses ULID text without throwing for invalid input.</summary>
    /// <param name="text">The 26-character Crockford Base32 text.</param>
    /// <returns>A result containing the decoded value and uppercase canonical text, or a failure reason.</returns>
    public UlidParseResult Parse(string? text)
    {
        if (text == null)
        {
            return UlidParseResult.Failure(IdentifierErrorCode.NullInput, "ULID text is null");
        }

        IdentifierValidationResult validation = CrockfordBase32.ValidateUlidText(text);
        if (!validation.Ok)
        {
            return UlidParseResult.Failure(validation.Code, validation.Error);
        }

        byte[] bytes = CrockfordBase32.DecodeUlidBytes(text);
        return UlidParseResult.Success(new UlidValue(bytes), validation.NormalizedText);
    }

    /// <summary>Parses ULID text without throwing for invalid input.</summary>
    /// <param name="text">The ULID text to parse.</param>
    /// <returns>The same result contract as <see cref="Parse"/>.</returns>
    public UlidParseResult TryParse(string? text)
    {
        return Parse(text);
    }

    /// <summary>Parses ULID text and returns the decoded value.</summary>
    /// <param name="text">The 26-character Crockford Base32 text.</param>
    /// <returns>The decoded ULID.</returns>
    /// <exception cref="IdentifierException">The text is null, malformed, or represents more than 128 bits.</exception>
    public UlidValue ParseValue(string? text)
    {
        UlidParseResult result = Parse(text);
        if (!result.Ok || result.Value == null)
        {
            throw new IdentifierException(result.Code, result.Error);
        }

        return result.Value;
    }

    /// <summary>Formats a ULID as canonical uppercase Crockford Base32 text.</summary>
    /// <param name="value">The ULID to format.</param>
    /// <returns>A 26-character string.</returns>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public string Format(UlidValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return value.ToCanonicalString();
    }

    /// <summary>Checks ULID length, Crockford alphabet, and 128-bit range.</summary>
    /// <param name="text">The text to validate.</param>
    /// <returns>A result containing uppercase canonical text or a failure reason.</returns>
    public IdentifierValidationResult Validate(string? text)
    {
        return CrockfordBase32.ValidateUlidText(text);
    }

    /// <summary>Compares two ULID values in byte order.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>A negative, zero, or positive value.</returns>
    public int CompareValues(UlidValue? left, UlidValue? right)
    {
        return UlidValue.Compare(left, right);
    }

    internal static void ThrowIfInvalidTimestamp(long unixMilliseconds)
    {
        if (unixMilliseconds < 0 || unixMilliseconds > BigEndianTools.MaxUInt48)
        {
            throw new IdentifierException(IdentifierErrorCode.TimestampOverflow, "ULID timestamp is outside the 48-bit range");
        }
    }
}
