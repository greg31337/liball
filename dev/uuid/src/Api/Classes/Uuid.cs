using System;

namespace uuid;

/// <summary>Creates, parses, converts, validates, and inspects UUID values.</summary>
/// <remarks>UUID bytes use RFC network order rather than the mixed-endian byte layout returned by some <see cref="Guid"/> byte APIs. Text is normalized to lowercase canonical form.</remarks>
public class Uuid : IUuid
{
    internal static readonly UuidV7GeneratorState MonotonicV7State = new UuidV7GeneratorState();

    /// <summary>Parses canonical, compact, or <c>urn:uuid:</c> UUID text without throwing for invalid input.</summary>
    /// <param name="text">The UUID text to parse.</param>
    /// <returns>A result containing the raw UUID value and lowercase canonical text, or a failure reason.</returns>
    public UuidParseResult Parse(string? text)
    {
        return UuidParser.Parse(text);
    }

    /// <summary>Parses UUID text without throwing for invalid input.</summary>
    /// <param name="text">The UUID text to parse.</param>
    /// <returns>The same result contract as <see cref="Parse"/>.</returns>
    public UuidParseResult TryParse(string? text)
    {
        return UuidParser.Parse(text);
    }

    /// <summary>Parses UUID text and returns the raw value.</summary>
    /// <param name="text">Canonical, compact, or UUID URN text.</param>
    /// <returns>The decoded UUID.</returns>
    /// <exception cref="IdentifierException">The text is null or malformed.</exception>
    public UuidValue ParseValue(string? text)
    {
        return UuidParser.ParseValue(text);
    }

    /// <summary>Creates a random RFC 9562 version 4 UUID.</summary>
    /// <returns>A UUID with 122 cryptographically random bits plus version and variant bits.</returns>
    public UuidValue NewV4()
    {
        return UuidGenerator.NewV4();
    }

    /// <summary>Creates a version 7 UUID for the current UTC millisecond.</summary>
    /// <returns>A time-ordered UUID with a random tail.</returns>
    public UuidValue NewV7()
    {
        return UuidGenerator.NewV7();
    }

    /// <summary>Creates a version 7 UUID for a supplied instant.</summary>
    /// <param name="time">The instant whose Unix millisecond is stored.</param>
    /// <returns>A time-ordered UUID with a random tail.</returns>
    /// <exception cref="IdentifierException">The timestamp is negative or exceeds 48 bits.</exception>
    public UuidValue NewV7At(DateTimeOffset time)
    {
        ThrowIfInvalidV7Timestamp(UnixTimeTools.ToUnixMilliseconds(time));
        return UuidGenerator.NewV7At(time);
    }

    /// <summary>Creates a version 7 UUID for a supplied Unix millisecond.</summary>
    /// <param name="unixMilliseconds">Milliseconds since the Unix epoch.</param>
    /// <returns>A time-ordered UUID with a random tail.</returns>
    /// <exception cref="IdentifierException">The timestamp is negative or exceeds 48 bits.</exception>
    public UuidValue NewV7AtUnixMilliseconds(long unixMilliseconds)
    {
        ThrowIfInvalidV7Timestamp(unixMilliseconds);
        return UuidGenerator.NewV7AtUnixMilliseconds(unixMilliseconds);
    }

    /// <summary>Creates a process-wide monotonic version 7 UUID using the current UTC millisecond.</summary>
    /// <returns>A value greater than the previous monotonic UUIDv7.</returns>
    /// <remarks>Within one millisecond the random tail is incremented while version and variant bits remain fixed. Backward clocks and tail overflow throw; state is process-local.</remarks>
    /// <exception cref="IdentifierException">The clock moved backwards or the random tail overflowed.</exception>
    public UuidValue NewMonotonicV7()
    {
        return MonotonicV7State.Next();
    }

    /// <summary>Creates a deterministic version 3 UUID from a namespace and UTF-8 name.</summary>
    /// <param name="namespaceId">The namespace UUID, commonly from <see cref="UuidNamespaces"/>.</param>
    /// <param name="name">The case-sensitive name encoded as UTF-8.</param>
    /// <returns>The MD5-based name UUID.</returns>
    /// <remarks>Version 3 is retained for interoperability. Prefer version 5 when choosing a new name-based format.</remarks>
    /// <exception cref="ArgumentNullException">The namespace or name is <c>null</c>.</exception>
    public UuidValue FromNameV3(UuidValue? namespaceId, string? name)
    {
        return UuidGenerator.FromNameV3(namespaceId, name);
    }

    /// <summary>Creates a deterministic version 5 UUID from a namespace and UTF-8 name.</summary>
    /// <param name="namespaceId">The namespace UUID, commonly from <see cref="UuidNamespaces"/>.</param>
    /// <param name="name">The case-sensitive name encoded as UTF-8.</param>
    /// <returns>The SHA-1-based name UUID.</returns>
    /// <exception cref="ArgumentNullException">The namespace or name is <c>null</c>.</exception>
    public UuidValue FromNameV5(UuidValue? namespaceId, string? name)
    {
        return UuidGenerator.FromNameV5(namespaceId, name);
    }

    /// <summary>Reorders a version 1 UUID into the version 6 time-first layout.</summary>
    /// <param name="value">An RFC-variant version 1 UUID.</param>
    /// <returns>A version 6 UUID with the same 60-bit timestamp, clock sequence, and node.</returns>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The value is not an RFC-variant version 1 UUID.</exception>
    public UuidValue V1ToV6(UuidValue? value)
    {
        return UuidConverter.V1ToV6(value);
    }

    /// <summary>Reorders a version 6 UUID into the original version 1 layout.</summary>
    /// <param name="value">An RFC-variant version 6 UUID.</param>
    /// <returns>A version 1 UUID with the same 60-bit timestamp, clock sequence, and node.</returns>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">The value is not an RFC-variant version 6 UUID.</exception>
    public UuidValue V6ToV1(UuidValue? value)
    {
        return UuidConverter.V6ToV1(value);
    }

    /// <summary>Extracts the timestamp carried by a version 1, 6, or 7 UUID.</summary>
    /// <param name="value">The UUID to inspect.</param>
    /// <returns>A timestamp result or a failure reason for null, non-RFC, or non-time-based values.</returns>
    public IdentifierTimestampResult TimestampValue(UuidValue? value)
    {
        return UuidTimestamp.Extract(value);
    }

    /// <summary>Parses UUID text and extracts its version 1, 6, or 7 timestamp.</summary>
    /// <param name="text">The UUID text to inspect.</param>
    /// <returns>A timestamp result or a parse or compatibility failure reason.</returns>
    public IdentifierTimestampResult TimestampText(string? text)
    {
        return UuidTimestamp.Extract(text);
    }

    /// <summary>Checks UUID syntax and requires RFC variant bits, except for Nil and Max UUIDs.</summary>
    /// <param name="text">Canonical, compact, or UUID URN text.</param>
    /// <returns>A result containing lowercase canonical text or a failure reason.</returns>
    public IdentifierValidationResult Validate(string? text)
    {
        UuidParseResult result = UuidParser.Parse(text);
        if (!result.Ok || result.Value == null)
        {
            return IdentifierValidationResult.Failure(result.Code, result.Error);
        }

        if (!result.Value.HasRfc9562Variant() && !result.Value.IsNil() && !result.Value.IsMax())
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidUuidVariant, "UUID does not use RFC 9562 variant bits");
        }

        return IdentifierValidationResult.Success(IdentifierKind.Uuid, result.NormalizedText);
    }

    /// <summary>Checks UUID syntax and strictly requires RFC 9562 variant bits.</summary>
    /// <param name="text">The UUID text to validate.</param>
    /// <returns>A validation result; unlike <see cref="Validate"/>, Nil and Max fail because their variant bits are not RFC variant bits.</returns>
    public IdentifierValidationResult ValidateRfc9562Variant(string? text)
    {
        UuidParseResult result = UuidParser.Parse(text);
        if (!result.Ok || result.Value == null)
        {
            return IdentifierValidationResult.Failure(result.Code, result.Error);
        }

        if (!result.Value.HasRfc9562Variant())
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidUuidVariant, "UUID does not use RFC 9562 variant bits");
        }

        return IdentifierValidationResult.Success(IdentifierKind.Uuid, result.NormalizedText);
    }

    /// <summary>Checks UUID syntax, RFC variant bits, and an expected known version.</summary>
    /// <param name="text">The UUID text to validate.</param>
    /// <param name="version">The required version; <see cref="UuidVersion.Unknown"/> is rejected.</param>
    /// <returns>A result containing canonical text or a specific failure reason.</returns>
    public IdentifierValidationResult ValidateVersion(string? text, UuidVersion version)
    {
        if (version == UuidVersion.Unknown)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidUuidVersion, "expected UUID version must be known");
        }

        UuidParseResult result = UuidParser.Parse(text);
        if (!result.Ok || result.Value == null)
        {
            return IdentifierValidationResult.Failure(result.Code, result.Error);
        }

        if (!result.Value.HasRfc9562Variant())
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidUuidVariant, "UUID does not use RFC 9562 variant bits");
        }

        if (result.Value.Version() != version)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.InvalidUuidVersion, "UUID version does not match expected version");
        }

        return IdentifierValidationResult.Success(IdentifierKind.Uuid, result.NormalizedText);
    }

    /// <summary>Converts a <see cref="Guid"/> to UUID network-order bytes.</summary>
    /// <param name="guid">The GUID to convert.</param>
    /// <returns>A UUID with the same canonical representation.</returns>
    public UuidValue FromGuid(Guid guid)
    {
        return UuidValue.FromGuid(guid);
    }

    /// <summary>Compares two UUID values in network-byte order.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>A negative, zero, or positive value.</returns>
    public int CompareValues(UuidValue? left, UuidValue? right)
    {
        return UuidValue.Compare(left, right);
    }

    internal static void ThrowIfInvalidV7Timestamp(long unixMilliseconds)
    {
        if (unixMilliseconds < 0 || unixMilliseconds > BigEndianTools.MaxUInt48)
        {
            throw new IdentifierException(IdentifierErrorCode.TimestampOverflow, "UUIDv7 timestamp is outside the 48-bit range");
        }
    }
}
