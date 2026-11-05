using System;

namespace uuid;

/// <summary>Defines UUID generation, parsing, conversion, validation, and comparison operations.</summary>
public interface IUuid
{
    /// <summary>Parses UUID text without throwing.</summary>
    UuidParseResult Parse(string? text);
    /// <summary>Tries to parse UUID text without throwing.</summary>
    UuidParseResult TryParse(string? text);
    /// <summary>Parses UUID text or throws.</summary>
    UuidValue ParseValue(string? text);
    /// <summary>Creates a random version 4 UUID.</summary>
    UuidValue NewV4();
    /// <summary>Creates a version 7 UUID for the current time.</summary>
    UuidValue NewV7();
    /// <summary>Creates a version 7 UUID for an instant.</summary>
    UuidValue NewV7At(DateTimeOffset time);
    /// <summary>Creates a version 7 UUID for a Unix millisecond.</summary>
    UuidValue NewV7AtUnixMilliseconds(long unixMilliseconds);
    /// <summary>Creates a process-wide monotonic version 7 UUID.</summary>
    UuidValue NewMonotonicV7();
    /// <summary>Creates a version 3 name UUID.</summary>
    UuidValue FromNameV3(UuidValue? namespaceId, string? name);
    /// <summary>Creates a version 5 name UUID.</summary>
    UuidValue FromNameV5(UuidValue? namespaceId, string? name);
    /// <summary>Converts a version 1 UUID to version 6 layout.</summary>
    UuidValue V1ToV6(UuidValue? value);
    /// <summary>Converts a version 6 UUID to version 1 layout.</summary>
    UuidValue V6ToV1(UuidValue? value);
    /// <summary>Converts a .NET GUID to UUID byte order.</summary>
    UuidValue FromGuid(Guid guid);
    /// <summary>Extracts a timestamp from a UUID value.</summary>
    IdentifierTimestampResult TimestampValue(UuidValue? value);
    /// <summary>Extracts a timestamp from UUID text.</summary>
    IdentifierTimestampResult TimestampText(string? text);
    /// <summary>Validates UUID text.</summary>
    IdentifierValidationResult Validate(string? text);
    /// <summary>Validates RFC 9562 variant bits.</summary>
    IdentifierValidationResult ValidateRfc9562Variant(string? text);
    /// <summary>Validates an expected UUID version.</summary>
    IdentifierValidationResult ValidateVersion(string? text, UuidVersion version);
    /// <summary>Compares two UUID values.</summary>
    int CompareValues(UuidValue? left, UuidValue? right);
}
