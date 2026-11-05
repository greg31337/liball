namespace version;

/// <summary>
/// Defines the supported version token values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum VersionTokenKind
{
    /// <summary>
    /// Uses end behavior.
    /// </summary>
    End = 0,
    /// <summary>
    /// Uses numeric identifier behavior.
    /// </summary>
    NumericIdentifier = 1,
    /// <summary>
    /// Uses text identifier behavior.
    /// </summary>
    TextIdentifier = 2,
    /// <summary>
    /// Uses separator behavior.
    /// </summary>
    Separator = 3,
    /// <summary>
    /// Uses operator behavior.
    /// </summary>
    Operator = 4,
    /// <summary>
    /// Uses wildcard behavior.
    /// </summary>
    Wildcard = 5,
    /// <summary>
    /// Uses range delimiter behavior.
    /// </summary>
    RangeDelimiter = 6,
    /// <summary>
    /// Uses whitespace behavior.
    /// </summary>
    Whitespace = 7,
    /// <summary>
    /// Indicates that no specific value is selected.
    /// </summary>
    Unknown = 8
}
