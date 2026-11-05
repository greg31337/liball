namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by Maven Resolver operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class MavenResolverDiagnosticCodes
{
    /// <summary>
    /// Identifies the unresolved artifact coordinate condition.
    /// </summary>
    public const string UnresolvedArtifactCoordinate = "MAVEN_RESOLVE_UNRESOLVED_ARTIFACT_COORDINATE";
    /// <summary>
    /// Identifies the conflicting ranges condition.
    /// </summary>
    public const string ConflictingRanges = "MAVEN_RESOLVE_CONFLICTING_RANGES";
    /// <summary>
    /// Identifies the unsupported feature condition.
    /// </summary>
    public const string UnsupportedFeature = "MAVEN_RESOLVE_UNSUPPORTED_FEATURE";
}
