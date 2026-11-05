using System;

namespace uuid;

/// <summary>Creates, parses, formats, and validates 12-byte MongoDB-style ObjectIds.</summary>
/// <remarks>An ObjectId stores a 32-bit Unix-second timestamp, five process-unique random bytes, and a 24-bit counter. Canonical text is 24 lowercase hexadecimal characters.</remarks>
public class ObjectId : IObjectId
{
    /// <summary>Creates an ObjectId for the current UTC second.</summary>
    /// <returns>A new value using the process-wide random component and next counter.</returns>
    public ObjectIdValue New()
    {
        return ObjectIdGenerator.New();
    }

    /// <summary>Creates an ObjectId for a supplied instant.</summary>
    /// <param name="time">The instant whose whole Unix second is stored.</param>
    /// <returns>A new ObjectId.</returns>
    /// <exception cref="IdentifierException">The timestamp does not fit in the unsigned 32-bit ObjectId field.</exception>
    public ObjectIdValue NewAt(DateTimeOffset time)
    {
        ThrowIfInvalidTimestamp(UnixTimeTools.ToUnixSeconds(time));
        return ObjectIdGenerator.NewAt(time);
    }

    /// <summary>Creates an ObjectId for a supplied Unix second.</summary>
    /// <param name="unixSeconds">Seconds since the Unix epoch.</param>
    /// <returns>A new ObjectId.</returns>
    /// <exception cref="IdentifierException">The timestamp is negative or greater than <see cref="ObjectIdLimits.MaxUnixSeconds"/>.</exception>
    public ObjectIdValue NewAtUnixSeconds(long unixSeconds)
    {
        ThrowIfInvalidTimestamp(unixSeconds);
        return ObjectIdGenerator.NewAtUnixSeconds(unixSeconds);
    }

    /// <summary>Parses 24-character hexadecimal ObjectId text without throwing for invalid input.</summary>
    /// <param name="text">The ObjectId text; upper- and lowercase hexadecimal are accepted.</param>
    /// <returns>A result containing the value and lowercase canonical text, or a failure reason.</returns>
    public ObjectIdParseResult Parse(string? text)
    {
        if (text == null)
        {
            return ObjectIdParseResult.Failure(IdentifierErrorCode.NullInput, "ObjectId text is null");
        }

        IdentifierValidationResult validation = Validate(text);
        if (!validation.Ok)
        {
            return ObjectIdParseResult.Failure(validation.Code, validation.Error);
        }

        byte[] bytes = HexTools.ParseObjectId(text);
        return ObjectIdParseResult.Success(new ObjectIdValue(bytes), validation.NormalizedText);
    }

    /// <summary>Parses ObjectId text without throwing for invalid input.</summary>
    /// <param name="text">The ObjectId text to parse.</param>
    /// <returns>The same result contract as <see cref="Parse"/>.</returns>
    public ObjectIdParseResult TryParse(string? text)
    {
        return Parse(text);
    }

    /// <summary>Parses ObjectId text and returns the decoded value.</summary>
    /// <param name="text">The 24-character hexadecimal text.</param>
    /// <returns>The decoded ObjectId.</returns>
    /// <exception cref="IdentifierException">The text is null, has the wrong length, or contains non-hexadecimal characters.</exception>
    public ObjectIdValue ParseValue(string? text)
    {
        ObjectIdParseResult result = Parse(text);
        if (!result.Ok || result.Value == null)
        {
            throw new IdentifierException(result.Code, result.Error);
        }

        return result.Value;
    }

    /// <summary>Formats an ObjectId as canonical lowercase hexadecimal text.</summary>
    /// <param name="value">The ObjectId to format.</param>
    /// <returns>A 24-character string.</returns>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public string Format(ObjectIdValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return value.ToHexString();
    }

    /// <summary>Formats an ObjectId as uppercase hexadecimal text.</summary>
    /// <param name="value">The ObjectId to format.</param>
    /// <returns>A 24-character uppercase string.</returns>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public string FormatUpper(ObjectIdValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return value.ToUpperHexString();
    }

    /// <summary>Checks ObjectId text for exact length and hexadecimal characters.</summary>
    /// <param name="text">The text to validate.</param>
    /// <returns>A result containing lowercase canonical text or a failure reason.</returns>
    public IdentifierValidationResult Validate(string? text)
    {
        return HexTools.ValidateFixedHex(text, ObjectIdLimits.ByteLength, IdentifierKind.ObjectId);
    }

    /// <summary>Compares two ObjectId values in byte order.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>A negative, zero, or positive value.</returns>
    public int CompareValues(ObjectIdValue? left, ObjectIdValue? right)
    {
        return ObjectIdValue.Compare(left, right);
    }

    internal static void ThrowIfInvalidTimestamp(long unixSeconds)
    {
        if (unixSeconds < 0 || unixSeconds > ObjectIdLimits.MaxUnixSeconds)
        {
            throw new IdentifierException(IdentifierErrorCode.TimestampOverflow, "ObjectId timestamp is outside the unsigned 32-bit range");
        }
    }
}
