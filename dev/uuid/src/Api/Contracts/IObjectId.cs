using System;

namespace uuid;

/// <summary>Defines ObjectId generation, parsing, formatting, validation, and comparison operations.</summary>
public interface IObjectId
{
    /// <summary>Creates an ObjectId for the current time.</summary>
    ObjectIdValue New();
    /// <summary>Creates an ObjectId for an instant.</summary>
    ObjectIdValue NewAt(DateTimeOffset time);
    /// <summary>Creates an ObjectId for a Unix second.</summary>
    ObjectIdValue NewAtUnixSeconds(long unixSeconds);
    /// <summary>Parses ObjectId text without throwing.</summary>
    ObjectIdParseResult Parse(string? text);
    /// <summary>Tries to parse ObjectId text without throwing.</summary>
    ObjectIdParseResult TryParse(string? text);
    /// <summary>Parses ObjectId text or throws.</summary>
    ObjectIdValue ParseValue(string? text);
    /// <summary>Formats an ObjectId as lowercase text.</summary>
    string Format(ObjectIdValue? value);
    /// <summary>Formats an ObjectId as uppercase text.</summary>
    string FormatUpper(ObjectIdValue? value);
    /// <summary>Validates ObjectId text.</summary>
    IdentifierValidationResult Validate(string? text);
    /// <summary>Compares two ObjectId values.</summary>
    int CompareValues(ObjectIdValue? left, ObjectIdValue? right);
}
