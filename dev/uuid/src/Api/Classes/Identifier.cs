using System;

namespace uuid;

/// <summary>Provides format-independent operations for every identifier format supported by the library.</summary>
/// <remarks>Classification checks UUID, ULID, KSUID, ObjectId, Snowflake, and then Nano ID. Because some text can match more than one format, the first successful format in that order wins.</remarks>
public class Identifier : IIdentifier
{
    internal static readonly IIdentifierFormat[] Formats = new IIdentifierFormat[]
    {
        new UuidIdentifierFormat(),
        new UlidIdentifierFormat(),
        new KsuidIdentifierFormat(),
        new ObjectIdIdentifierFormat(),
        new SnowflakeIdentifierFormat(),
        new NanoIdIdentifierFormat()
    };

    /// <summary>Checks whether text is a supported identifier and returns its format and canonical representation.</summary>
    /// <param name="text">The identifier text to inspect.</param>
    /// <returns>A successful result with the detected kind and normalized text, or a failed result when no format accepts the input.</returns>
    public IdentifierValidationResult Validate(string? text)
    {
        return Classify(text);
    }

    /// <summary>Checks whether text is valid for one explicitly requested identifier format.</summary>
    /// <param name="text">The identifier text to inspect.</param>
    /// <param name="kind">The UUID, ULID, KSUID, Snowflake, ObjectId, or Nano ID format to require.</param>
    /// <returns>A format-specific validation result without automatic classification. Unknown or unsupported kinds fail with <see cref="IdentifierErrorCode.UnsupportedKind"/>.</returns>
    public IdentifierValidationResult ValidateKind(string? text, IdentifierKind kind)
    {
        if (kind == IdentifierKind.Uuid)
        {
            return new Uuid().Validate(text);
        }

        if (kind == IdentifierKind.Ulid)
        {
            return new Ulid().Validate(text);
        }

        if (kind == IdentifierKind.Ksuid)
        {
            return new Ksuid().Validate(text);
        }

        if (kind == IdentifierKind.Snowflake)
        {
            SnowflakeParseResult result = new Snowflake().DefaultGenerator().Parse(text);
            if (!result.Ok)
            {
                return IdentifierValidationResult.Failure(result.Code, result.Error);
            }

            return IdentifierValidationResult.Success(IdentifierKind.Snowflake, result.NormalizedText);
        }

        if (kind == IdentifierKind.ObjectId)
        {
            return new ObjectId().Validate(text);
        }

        if (kind == IdentifierKind.NanoId)
        {
            return new NanoId().Validate(text);
        }

        return IdentifierValidationResult.Failure(IdentifierErrorCode.UnsupportedKind, "requested identifier kind is unknown");
    }

    /// <summary>Parses identifier text without requiring the caller to choose a format first.</summary>
    /// <param name="text">The identifier text to parse.</param>
    /// <returns>A result containing the parsed value, detected format, and normalized text.</returns>
    public IdentifierParseResult Parse(string? text)
    {
        if (text == null)
        {
            return IdentifierParseResult.Failure(IdentifierErrorCode.NullInput, "identifier text is null");
        }

        foreach (IIdentifierFormat format in Formats)
        {
            IdentifierParseResult result = format.Parse(text);
            if (result.Ok)
            {
                return result;
            }
        }

        return IdentifierParseResult.Failure(IdentifierErrorCode.UnsupportedKind, "identifier kind is unknown");
    }

    /// <summary>Converts supported identifier text to the canonical spelling for its detected format.</summary>
    /// <param name="text">The identifier text to normalize.</param>
    /// <returns>The normalized identifier text.</returns>
    /// <exception cref="IdentifierException">The text does not match a supported identifier format.</exception>
    public string NormalizeText(string? text)
    {
        IdentifierValidationResult validation = Validate(text);
        if (!validation.Ok)
        {
            throw new IdentifierException(validation.Code, validation.Error);
        }

        return validation.NormalizedText;
    }

    /// <summary>Returns the canonical text supplied by a format-independent identifier value.</summary>
    /// <param name="value">The identifier value to normalize.</param>
    /// <returns>The value's canonical text.</returns>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public string NormalizeValue(IIdentifierValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return value.ToCanonicalString();
    }

    /// <summary>Extracts the creation timestamp carried by an identifier.</summary>
    /// <param name="text">Text for a UUID, ULID, KSUID, Snowflake, or ObjectId.</param>
    /// <returns>A timestamp result. It fails for invalid text, Nano IDs, and UUID versions without a supported timestamp.</returns>
    /// <remarks>Snowflake text is decoded with the Twitter-default epoch and bit allocation. Use a configured <see cref="SnowflakeGenerator"/> when the value uses different settings.</remarks>
    public IdentifierTimestampResult TimestampText(string? text)
    {
        IdentifierParseResult parsed = Parse(text);
        if (!parsed.Ok || parsed.Value == null)
        {
            return IdentifierTimestampResult.Failure(parsed.Code, parsed.Error);
        }

        return TimestampParsedValue(parsed.Value);
    }

    /// <summary>Extracts the timestamp carried by a format-independent identifier value.</summary>
    /// <param name="value">A parsed identifier value.</param>
    /// <returns>A timestamp result. It fails for Nano IDs, UUID versions without a supported timestamp, and custom values whose canonical text is invalid.</returns>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public IdentifierTimestampResult TimestampValue(IIdentifierValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        ITimestampedIdentifierValue? timestamped = value as ITimestampedIdentifierValue;
        if (timestamped != null)
        {
            return timestamped.Timestamp();
        }

        return TimestampText(value.ToCanonicalString());
    }

    /// <summary>Compares two supported identifiers in their natural normalized order.</summary>
    /// <param name="left">The first identifier text.</param>
    /// <param name="right">The second identifier text.</param>
    /// <returns>A negative value when the first identifier sorts earlier, zero when equal, or a positive value when it sorts later.</returns>
    /// <remarks>Identifiers of different formats are ordered by <see cref="IdentifierKind"/>. Snowflakes are compared numerically; other same-format identifiers are compared by canonical ordinal text.</remarks>
    /// <exception cref="IdentifierException">Either input is not a supported identifier.</exception>
    public int CompareText(string? left, string? right)
    {
        IdentifierParseResult leftResult = Parse(left);
        if (!leftResult.Ok || leftResult.Value == null)
        {
            throw new IdentifierException(leftResult.Code, leftResult.Error);
        }

        IdentifierParseResult rightResult = Parse(right);
        if (!rightResult.Ok || rightResult.Value == null)
        {
            throw new IdentifierException(rightResult.Code, rightResult.Error);
        }

        return CompareValues(leftResult.Value, rightResult.Value);
    }

    /// <summary>Compares two format-independent identifier values in their natural canonical order.</summary>
    /// <param name="left">The first identifier value.</param>
    /// <param name="right">The second identifier value.</param>
    /// <returns>A negative value when the first identifier sorts earlier, zero when equal, or a positive value when it sorts later.</returns>
    /// <remarks>Values of different formats are ordered by <see cref="IdentifierKind"/>. Snowflakes are compared numerically; other same-format values are compared by canonical ordinal text.</remarks>
    /// <exception cref="ArgumentNullException">Either value is <c>null</c>.</exception>
    /// <exception cref="IdentifierException">A Snowflake value does not provide valid canonical decimal text.</exception>
    public int CompareValues(IIdentifierValue? left, IIdentifierValue? right)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        IdentifierKind leftKind = left.Kind();
        IdentifierKind rightKind = right.Kind();
        if (leftKind != rightKind)
        {
            return CompareKind(leftKind, rightKind);
        }

        string leftText = left.ToCanonicalString();
        string rightText = right.ToCanonicalString();
        if (leftKind == IdentifierKind.Snowflake)
        {
            return CompareSnowflakes(leftText, rightText);
        }

        return string.CompareOrdinal(leftText, rightText);
    }

    /// <summary>Determines which supported format accepts the supplied text.</summary>
    /// <param name="text">The identifier text to classify.</param>
    /// <returns>A validation result containing the selected format and normalized text, or an unknown-format error.</returns>
    /// <remarks>The first matching format wins in this order: UUID, ULID, KSUID, ObjectId, Snowflake, then Nano ID.</remarks>
    public IdentifierValidationResult Classify(string? text)
    {
        if (text == null)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.NullInput, "identifier text is null");
        }

        foreach (IIdentifierFormat format in Formats)
        {
            IdentifierValidationResult result = format.Validate(text);
            if (result.Ok)
            {
                return result;
            }
        }

        return IdentifierValidationResult.Failure(IdentifierErrorCode.UnsupportedKind, "identifier kind is unknown");
    }

    /// <summary>Returns only the detected format for identifier text.</summary>
    /// <param name="text">The identifier text to classify.</param>
    /// <returns>The detected format, or <see cref="IdentifierKind.Unknown"/> when classification fails.</returns>
    public IdentifierKind ClassifyKind(string? text)
    {
        return Classify(text).Kind;
    }

    internal static IdentifierTimestampResult TimestampFromUnixMilliseconds(IdentifierKind kind, long unixMilliseconds)
    {
        IdentifierValidationResult validation = UnixTimeTools.ValidateUnixMilliseconds(unixMilliseconds);
        if (!validation.Ok)
        {
            return IdentifierTimestampResult.Failure(validation.Code, validation.Error);
        }

        long unixSeconds = UnixTimeTools.MillisecondsToSeconds(unixMilliseconds);
        DateTimeOffset time = UnixTimeTools.FromUnixMilliseconds(unixMilliseconds);
        return IdentifierTimestampResult.Success(kind, unixMilliseconds, unixSeconds, time);
    }

    internal static IdentifierTimestampResult TimestampParsedValue(IIdentifierValue value)
    {
        ITimestampedIdentifierValue? timestamped = value as ITimestampedIdentifierValue;
        if (timestamped == null)
        {
            return IdentifierTimestampResult.Failure(IdentifierErrorCode.UnsupportedKind, "identifier kind does not carry a timestamp");
        }

        return timestamped.Timestamp();
    }

    internal static IdentifierTimestampResult TimestampFromUnixSeconds(IdentifierKind kind, long unixSeconds)
    {
        IdentifierValidationResult validation = UnixTimeTools.ValidateUnixSeconds(unixSeconds);
        if (!validation.Ok)
        {
            return IdentifierTimestampResult.Failure(validation.Code, validation.Error);
        }

        long unixMilliseconds = UnixTimeTools.SecondsToMilliseconds(unixSeconds);
        DateTimeOffset time = UnixTimeTools.FromUnixSeconds(unixSeconds);
        return IdentifierTimestampResult.Success(kind, unixMilliseconds, unixSeconds, time);
    }

    internal static int CompareKind(IdentifierKind left, IdentifierKind right)
    {
        int leftValue = (int)left;
        int rightValue = (int)right;
        if (leftValue < rightValue)
        {
            return -1;
        }

        if (leftValue > rightValue)
        {
            return 1;
        }

        return 0;
    }

    internal static int CompareSnowflakes(string left, string right)
    {
        SnowflakeParseResult leftResult = SnowflakeGenerator.ParseDecimalString(left, SnowflakeSettings.TwitterDefault());
        SnowflakeParseResult rightResult = SnowflakeGenerator.ParseDecimalString(right, SnowflakeSettings.TwitterDefault());
        if (!leftResult.Ok || leftResult.Value == null)
        {
            throw new IdentifierException(leftResult.Code, leftResult.Error);
        }

        if (!rightResult.Ok || rightResult.Value == null)
        {
            throw new IdentifierException(rightResult.Code, rightResult.Error);
        }

        ulong leftValue = leftResult.Value.ToUInt64();
        ulong rightValue = rightResult.Value.ToUInt64();
        if (leftValue < rightValue)
        {
            return -1;
        }

        if (leftValue > rightValue)
        {
            return 1;
        }

        return 0;
    }
}
