namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by Maven Metadata operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class MavenMetadataDiagnosticCodes
{
    /// <summary>
    /// Identifies the malformed metadata condition.
    /// </summary>
    public const string MalformedMetadata = "MAVEN_METADATA_MALFORMED_METADATA";
    /// <summary>
    /// Identifies the missing snapshot metadata condition.
    /// </summary>
    public const string MissingSnapshotMetadata = "MAVEN_METADATA_MISSING_SNAPSHOT_METADATA";
}
