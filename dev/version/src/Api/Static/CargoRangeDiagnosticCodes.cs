namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by Cargo Range operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class CargoRangeDiagnosticCodes
{
    /// <summary>
    /// Identifies the missing comparator condition.
    /// </summary>
    public const string MissingComparator = "CARGO_RANGE_MISSING_COMPARATOR";
    /// <summary>
    /// Identifies the missing version condition.
    /// </summary>
    public const string MissingVersion = "CARGO_RANGE_MISSING_VERSION";
    /// <summary>
    /// Identifies the invalid operator condition.
    /// </summary>
    public const string InvalidOperator = "CARGO_RANGE_INVALID_OPERATOR";
    /// <summary>
    /// Identifies the invalid version condition.
    /// </summary>
    public const string InvalidVersion = "CARGO_RANGE_INVALID_VERSION";
}
