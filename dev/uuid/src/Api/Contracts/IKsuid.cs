using System;

namespace uuid;

/// <summary>Defines KSUID generation, parsing, formatting, validation, and comparison operations.</summary>
public interface IKsuid
{
    /// <summary>Creates a KSUID for the current time.</summary>
    KsuidValue New();
    /// <summary>Creates a KSUID for an instant.</summary>
    KsuidValue NewAt(DateTimeOffset time);
    /// <summary>Creates a KSUID for a Unix second.</summary>
    KsuidValue NewAtUnixSeconds(long unixSeconds);
    /// <summary>Parses KSUID text without throwing.</summary>
    KsuidParseResult Parse(string? text);
    /// <summary>Tries to parse KSUID text without throwing.</summary>
    KsuidParseResult TryParse(string? text);
    /// <summary>Parses KSUID text or throws.</summary>
    KsuidValue ParseValue(string? text);
    /// <summary>Formats a KSUID.</summary>
    string Format(KsuidValue? value);
    /// <summary>Validates KSUID text.</summary>
    IdentifierValidationResult Validate(string? text);
    /// <summary>Compares two KSUID values.</summary>
    int CompareValues(KsuidValue? left, KsuidValue? right);
}
