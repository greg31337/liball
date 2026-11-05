namespace currencycodes;

/// <summary>
/// Selects the ordinal comparison used by official-name lookups.
/// </summary>
public enum NameMatchMode
{
    /// <summary>
    /// Match names using an ordinal case-sensitive comparison.
    /// </summary>
    ExactOrdinal = 0,

    /// <summary>
    /// Match names using an ordinal case-insensitive comparison.
    /// </summary>
    ExactOrdinalIgnoreCase = 1
}
