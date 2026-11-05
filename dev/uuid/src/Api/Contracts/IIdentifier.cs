namespace uuid;

/// <summary>Defines format-independent identifier parsing, validation, normalization, timestamp, and comparison operations.</summary>
public interface IIdentifier
{
    /// <summary>Validates identifier text.</summary>
    IdentifierValidationResult Validate(string? text);

    /// <summary>Validates identifier text as a requested format without automatic classification.</summary>
    IdentifierValidationResult ValidateKind(string? text, IdentifierKind kind);

    /// <summary>Parses identifier text.</summary>
    IdentifierParseResult Parse(string? text);

    /// <summary>Normalizes identifier text.</summary>
    string NormalizeText(string? text);

    /// <summary>Returns a value's canonical text.</summary>
    string NormalizeValue(IIdentifierValue? value);

    /// <summary>Extracts a timestamp from identifier text.</summary>
    IdentifierTimestampResult TimestampText(string? text);

    /// <summary>Extracts a timestamp from an identifier value.</summary>
    IdentifierTimestampResult TimestampValue(IIdentifierValue? value);

    /// <summary>Compares two identifier strings.</summary>
    int CompareText(string? left, string? right);

    /// <summary>Compares two identifier values.</summary>
    int CompareValues(IIdentifierValue? left, IIdentifierValue? right);

    /// <summary>Classifies and validates identifier text.</summary>
    IdentifierValidationResult Classify(string? text);

    /// <summary>Returns the detected identifier kind.</summary>
    IdentifierKind ClassifyKind(string? text);
}
