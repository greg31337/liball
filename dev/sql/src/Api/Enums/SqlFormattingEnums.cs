namespace sql;

/// <summary>Specifies the supported keyword case.</summary>
public enum SqlKeywordCase
{
    /// <summary>Represents preserve.</summary>
    Preserve = 0,
    /// <summary>Represents upper.</summary>
    Upper = 1,
    /// <summary>Represents lower.</summary>
    Lower = 2
}

/// <summary>Specifies the supported comma style.</summary>
public enum SqlCommaStyle
{
    /// <summary>Represents inline.</summary>
    Inline = 0,
    /// <summary>Represents leading.</summary>
    Leading = 1
}

/// <summary>Specifies casing applied to unquoted identifiers.</summary>
public enum SqlIdentifierCase
{
    /// <summary>Preserves identifier spelling.</summary>
    Preserve = 0,
    /// <summary>Writes unquoted identifiers in upper case.</summary>
    Upper = 1,
    /// <summary>Writes unquoted identifiers in lower case.</summary>
    Lower = 2
}

/// <summary>Specifies how normalized generation handles trailing commas.</summary>
public enum SqlTrailingCommaPolicy
{
    /// <summary>Does not add trailing commas.</summary>
    Never = 0,
    /// <summary>Adds a trailing comma only in constructs supported by the selected write dialect.</summary>
    WhenSupported = 1
}

/// <summary>Specifies the overall formatting mode.</summary>
public enum SqlFormatMode
{
    /// <summary>Uses the normal readable layout.</summary>
    Pretty = 0,
    /// <summary>Collapses generated layout onto compact lines.</summary>
    Compact = 1,
    /// <summary>Removes optional whitespace while retaining valid token separation.</summary>
    Minimal = 2,
    /// <summary>Writes preserved source tokens without normalizing their spelling or layout.</summary>
    RoundTrip = 3
}
