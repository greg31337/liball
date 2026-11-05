namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by Maven Range operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class MavenRangeDiagnosticCodes
{
    /// <summary>
    /// Identifies the missing range condition.
    /// </summary>
    public const string MissingRange = "MAVEN_RANGE_MISSING_RANGE";
    /// <summary>
    /// Identifies the missing version condition.
    /// </summary>
    public const string MissingVersion = "MAVEN_RANGE_MISSING_VERSION";
    /// <summary>
    /// Identifies the invalid delimiter condition.
    /// </summary>
    public const string InvalidDelimiter = "MAVEN_RANGE_INVALID_DELIMITER";
    /// <summary>
    /// Identifies the invalid version condition.
    /// </summary>
    public const string InvalidVersion = "MAVEN_RANGE_INVALID_VERSION";
    /// <summary>
    /// Identifies the invalid range condition.
    /// </summary>
    public const string InvalidRange = "MAVEN_RANGE_INVALID_RANGE";
}
