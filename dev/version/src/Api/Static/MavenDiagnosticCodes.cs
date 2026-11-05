namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by Maven operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class MavenDiagnosticCodes
{
    /// <summary>
    /// Identifies the missing version condition.
    /// </summary>
    public const string MissingVersion = "MAVEN_VERSION_MISSING_VERSION";
    /// <summary>
    /// Identifies the invalid version condition.
    /// </summary>
    public const string InvalidVersion = "MAVEN_VERSION_INVALID_VERSION";
}
