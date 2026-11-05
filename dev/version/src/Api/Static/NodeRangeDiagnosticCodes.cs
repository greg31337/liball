namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by Node Range operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class NodeRangeDiagnosticCodes
{
    /// <summary>
    /// Identifies the null input condition.
    /// </summary>
    public const string NullInput = "NODE_RANGE_NULL_INPUT";
    /// <summary>
    /// Identifies the input too long condition.
    /// </summary>
    public const string InputTooLong = "NODE_RANGE_INPUT_TOO_LONG";
    /// <summary>
    /// Identifies the missing comparator condition.
    /// </summary>
    public const string MissingComparator = "NODE_RANGE_MISSING_COMPARATOR";
    /// <summary>
    /// Identifies the missing version condition.
    /// </summary>
    public const string MissingVersion = "NODE_RANGE_MISSING_VERSION";
    /// <summary>
    /// Identifies the invalid operator condition.
    /// </summary>
    public const string InvalidOperator = "NODE_RANGE_INVALID_OPERATOR";
    /// <summary>
    /// Identifies the invalid version condition.
    /// </summary>
    public const string InvalidVersion = "NODE_RANGE_INVALID_VERSION";
}
