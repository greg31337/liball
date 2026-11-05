using System;

namespace uuid;

/// <summary>Creates, parses, formats, and validates 160-bit K-Sortable Unique Identifiers.</summary>
/// <remarks>A KSUID stores seconds since its custom 2014 epoch followed by a 128-bit cryptographically random payload. Canonical text is 27 case-sensitive Base62 characters and sorts in the same order as the bytes.</remarks>
public class Ksuid : IKsuid
{
    /// <summary>Creates a KSUID for the current UTC second with a new random payload.</summary>
    /// <returns>A new KSUID value.</returns>
    public KsuidValue New()
    {
        return KsuidGenerator.New();
    }

    /// <summary>Creates a KSUID for a supplied instant with a new random payload.</summary>
    /// <param name="time">The instant whose whole Unix second is stored.</param>
    /// <returns>A new KSUID value.</returns>
    /// <exception cref="IdentifierException">The instant is before the KSUID epoch or exceeds its 32-bit timestamp range.</exception>
    public KsuidValue NewAt(DateTimeOffset time)
    {
        ThrowIfInvalidTimestamp(UnixTimeTools.ToUnixSeconds(time));
        return KsuidGenerator.NewAt(time);
    }

    /// <summary>Creates a KSUID for a supplied Unix second with a new random payload.</summary>
    /// <param name="unixSeconds">Seconds since the Unix epoch.</param>
    /// <returns>A new KSUID value.</returns>
    /// <exception cref="IdentifierException">The value is outside the KSUID timestamp range.</exception>
    public KsuidValue NewAtUnixSeconds(long unixSeconds)
    {
        ThrowIfInvalidTimestamp(unixSeconds);
        return KsuidGenerator.NewAtUnixSeconds(unixSeconds);
    }

    /// <summary>Parses canonical KSUID text without throwing for invalid input.</summary>
    /// <param name="text">The 27-character, case-sensitive Base62 text.</param>
    /// <returns>A result containing the decoded value and canonical text, or a failure reason.</returns>
    public KsuidParseResult Parse(string? text)
    {
        if (text == null)
        {
            return KsuidParseResult.Failure(IdentifierErrorCode.NullInput, "KSUID text is null");
        }

        IdentifierValidationResult validation = Base62.ValidateKsuidText(text);
        if (!validation.Ok)
        {
            return KsuidParseResult.Failure(validation.Code, validation.Error);
        }

        byte[] bytes = Base62.DecodeKsuidBytes(text);
        return KsuidParseResult.Success(new KsuidValue(bytes), validation.NormalizedText);
    }

    /// <summary>Parses canonical KSUID text without throwing for invalid input.</summary>
    /// <param name="text">The 27-character, case-sensitive Base62 text.</param>
    /// <returns>The same result contract as <see cref="Parse"/>.</returns>
    public KsuidParseResult TryParse(string? text)
    {
        return Parse(text);
    }

    /// <summary>Parses canonical KSUID text and returns the decoded value.</summary>
    /// <param name="text">The 27-character, case-sensitive Base62 text.</param>
    /// <returns>The decoded KSUID.</returns>
    /// <exception cref="IdentifierException">The text is null, malformed, or larger than 160 bits.</exception>
    public KsuidValue ParseValue(string? text)
    {
        KsuidParseResult result = Parse(text);
        if (!result.Ok || result.Value == null)
        {
            throw new IdentifierException(result.Code, result.Error);
        }

        return result.Value;
    }

    /// <summary>Formats a KSUID as canonical 27-character Base62 text.</summary>
    /// <param name="value">The KSUID to format.</param>
    /// <returns>The canonical text.</returns>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public string Format(KsuidValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return value.ToCanonicalString();
    }

    /// <summary>Checks KSUID text for canonical length, alphabet, and 160-bit range.</summary>
    /// <param name="text">The text to validate.</param>
    /// <returns>A validation result containing canonical text or a failure reason.</returns>
    public IdentifierValidationResult Validate(string? text)
    {
        return Base62.ValidateKsuidText(text);
    }

    /// <summary>Compares two KSUID values in byte order.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>A negative, zero, or positive value.</returns>
    public int CompareValues(KsuidValue? left, KsuidValue? right)
    {
        return KsuidValue.Compare(left, right);
    }

    internal static void ThrowIfInvalidTimestamp(long unixSeconds)
    {
        IdentifierValidationResult validation = KsuidTimestamp.ValidateUnixSeconds(unixSeconds);
        if (!validation.Ok)
        {
            throw new IdentifierException(validation.Code, validation.Error);
        }
    }
}
