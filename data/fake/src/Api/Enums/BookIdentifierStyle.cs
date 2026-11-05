namespace fake;

/// <summary>
/// Selects a textual presentation for synthetic book and serial-publication identifiers.
/// </summary>
public enum BookIdentifierStyle
{
    /// <summary>
    /// Uses only the identifier characters with no label or separators.
    /// </summary>
    Compact = 0,

    /// <summary>
    /// Separates readability groups with hyphens.
    /// </summary>
    Hyphenated = 1,

    /// <summary>
    /// Separates readability groups with spaces.
    /// </summary>
    Spaced = 2,

    /// <summary>
    /// Adds the identifier-system label and uses hyphenated readability groups.
    /// </summary>
    Labeled = 3
}
