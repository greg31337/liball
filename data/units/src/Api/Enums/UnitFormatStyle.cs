namespace units;

/// <summary>
/// Identifies the textual representation used to format a unit.
/// </summary>
public enum UnitFormatStyle
{
    /// <summary>
    /// Formats a unit using its compact symbol.
    /// </summary>
    Symbol = 1,
    /// <summary>
    /// Formats a unit using its human-readable name.
    /// </summary>
    Name = 2,
    /// <summary>
    /// Formats a deterministic expression suitable for interchange.
    /// </summary>
    Canonical = 3,
    /// <summary>
    /// Formats a unit as canonical base dimensions.
    /// </summary>
    Decomposed = 4
}
