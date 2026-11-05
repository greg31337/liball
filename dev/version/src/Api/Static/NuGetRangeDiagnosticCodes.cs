namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by NuGet Range operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class NuGetRangeDiagnosticCodes
{
    /// <summary>
    /// Identifies the missing version condition.
    /// </summary>
    public const string MissingVersion = "NUGET_RANGE_MISSING_VERSION";
    /// <summary>
    /// Identifies the invalid delimiter condition.
    /// </summary>
    public const string InvalidDelimiter = "NUGET_RANGE_INVALID_DELIMITER";
    /// <summary>
    /// Identifies the invalid version condition.
    /// </summary>
    public const string InvalidVersion = "NUGET_RANGE_INVALID_VERSION";
    /// <summary>
    /// Identifies the invalid range condition.
    /// </summary>
    public const string InvalidRange = "NUGET_RANGE_INVALID_RANGE";
}
