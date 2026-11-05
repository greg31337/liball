using System;

namespace uuid;

/// <summary>Defines ULID generation, parsing, conversion, validation, and comparison operations.</summary>
public interface IUlid
{
    /// <summary>Creates a ULID for the current time.</summary>
    UlidValue New();
    /// <summary>Creates a ULID for an instant.</summary>
    UlidValue NewAt(DateTimeOffset time);
    /// <summary>Creates a ULID for a Unix millisecond.</summary>
    UlidValue NewAtUnixMilliseconds(long unixMilliseconds);
    /// <summary>Creates a process-wide monotonic ULID.</summary>
    UlidValue NewMonotonic();
    /// <summary>Creates a ULID from UUID-compatible bytes.</summary>
    UlidValue FromUuidBytes(byte[]? bytes);
    /// <summary>Returns UUID-compatible bytes.</summary>
    byte[] ToUuidBytes(UlidValue? value);
    /// <summary>Parses ULID text without throwing.</summary>
    UlidParseResult Parse(string? text);
    /// <summary>Tries to parse ULID text without throwing.</summary>
    UlidParseResult TryParse(string? text);
    /// <summary>Parses ULID text or throws.</summary>
    UlidValue ParseValue(string? text);
    /// <summary>Formats a ULID.</summary>
    string Format(UlidValue? value);
    /// <summary>Validates ULID text.</summary>
    IdentifierValidationResult Validate(string? text);
    /// <summary>Compares two ULID values.</summary>
    int CompareValues(UlidValue? left, UlidValue? right);
}
