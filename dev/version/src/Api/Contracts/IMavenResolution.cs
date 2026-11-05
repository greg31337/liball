namespace version;

/// <summary>
/// Defines public Maven version, range, metadata, artifact, and dependency resolution operations.
/// </summary>
public interface IMavenResolution
{
    /// <summary>
    /// Parses Maven ComparableVersion text without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed Maven version or diagnostics.</returns>
    public VersionParseResult ParseVersion(string text);

    /// <summary>
    /// Normalizes Maven ComparableVersion text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The normalized Maven version text.</returns>
    public string NormalizeVersion(string text);

    /// <summary>
    /// Parses a Maven soft requirement, interval, interval union, or exclusion without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed range or diagnostics.</returns>
    public VersionRangeParseResult ParseRange(string text);

    /// <summary>
    /// Compares two Maven versions using ComparableVersion ordering.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareVersions(string leftText, string rightText);

    /// <summary>
    /// Parses Maven repository metadata without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="metadataXml">The metadata XML to process.</param>
    /// <returns>A result containing the parsed metadata or diagnostics.</returns>
    public MavenMetadataParseResult ParseMetadata(string metadataXml);

    /// <summary>
    /// Parses the available version list from Maven repository metadata.
    /// </summary>
    /// <param name="metadataXml">The metadata XML to process.</param>
    /// <returns>The available version text in metadata order.</returns>
    public string[] ParseMetadataVersions(string metadataXml);

    /// <summary>
    /// Parses the snapshot artifact entries from Maven repository metadata.
    /// </summary>
    /// <param name="metadataXml">The metadata XML to process.</param>
    /// <returns>The snapshot artifact entries in metadata order.</returns>
    public MavenSnapshotVersionMetadata[] ParseSnapshotVersions(string metadataXml);

    /// <summary>
    /// Resolves the declared release or highest non-snapshot repository version.
    /// </summary>
    /// <param name="metadata">The repository metadata to examine.</param>
    /// <returns>The resolved artifact identity, or null when no release is available.</returns>
    public MavenArtifactVersionIdentity? ResolveReleaseVersion(MavenRepositoryMetadata metadata);

    /// <summary>
    /// Resolves the declared latest or highest repository version.
    /// </summary>
    /// <param name="metadata">The repository metadata to examine.</param>
    /// <returns>The resolved artifact identity, or null when no version is available.</returns>
    public MavenArtifactVersionIdentity? ResolveLatestVersion(MavenRepositoryMetadata metadata);

    /// <summary>
    /// Selects the highest repository version accepted by an explicit Maven interval range.
    /// </summary>
    /// <param name="metadata">The repository metadata to examine.</param>
    /// <param name="rangeText">The explicit Maven interval range to apply.</param>
    /// <returns>The resolved artifact identity, or null when no version matches.</returns>
    public MavenArtifactVersionIdentity? ResolveHighestRangeVersion(
        MavenRepositoryMetadata metadata,
        string rangeText);

    /// <summary>
    /// Resolves a snapshot artifact entry using repository metadata.
    /// </summary>
    /// <param name="metadata">The repository metadata to examine.</param>
    /// <param name="extension">The requested artifact extension.</param>
    /// <param name="classifier">The requested classifier, or null for an unclassified artifact.</param>
    /// <returns>The resolved snapshot identity, or null when no matching entry is available.</returns>
    public MavenArtifactVersionIdentity? ResolveSnapshotVersion(
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier);

    /// <summary>
    /// Resolves a snapshot artifact entry and reports an ordinary missing result through diagnostics.
    /// </summary>
    /// <param name="metadata">The repository metadata to examine.</param>
    /// <param name="extension">The requested artifact extension.</param>
    /// <param name="classifier">The requested classifier, or null for an unclassified artifact.</param>
    /// <returns>A result containing the resolved snapshot identity or diagnostics.</returns>
    public MavenArtifactVersionResolutionResult ResolveSnapshotVersionWithDiagnostics(
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier);

    /// <summary>
    /// Resolves a supplied snapshot identity using repository metadata.
    /// </summary>
    /// <param name="snapshotIdentity">The base snapshot identity.</param>
    /// <param name="metadata">The repository metadata to examine.</param>
    /// <param name="extension">The requested artifact extension.</param>
    /// <param name="classifier">The requested classifier, or null for an unclassified artifact.</param>
    /// <returns>The resolved snapshot identity, or null when no matching entry is available.</returns>
    public MavenArtifactVersionIdentity? ResolveSnapshotVersionForIdentity(
        MavenArtifactVersionIdentity snapshotIdentity,
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier);

    /// <summary>
    /// Resolves a snapshot coordinate using repository metadata.
    /// </summary>
    /// <param name="groupIdText">The Maven group identifier.</param>
    /// <param name="artifactIdText">The Maven artifact identifier.</param>
    /// <param name="baseSnapshotVersionText">The base snapshot version.</param>
    /// <param name="metadata">The repository metadata to examine.</param>
    /// <param name="extension">The requested artifact extension.</param>
    /// <param name="classifier">The requested classifier, or null for an unclassified artifact.</param>
    /// <returns>The resolved snapshot identity, or null when no matching entry is available.</returns>
    public MavenArtifactVersionIdentity? ResolveSnapshotVersionForCoordinate(
        string groupIdText,
        string artifactIdText,
        string baseSnapshotVersionText,
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier);

    /// <summary>
    /// Resolves a release or timestamped snapshot artifact from artifact metadata.
    /// </summary>
    /// <param name="artifactMetadata">The artifact metadata to examine.</param>
    /// <returns>A result containing the resolved artifact identity or diagnostics.</returns>
    public MavenArtifactVersionResolutionResult ResolveArtifactVersion(
        MavenArtifactMetadata artifactMetadata);

    /// <summary>
    /// Resolves Maven dependencies from the supplied models.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to resolve.</param>
    /// <param name="availableModels">The available dependency models to examine.</param>
    /// <returns>A result containing the resolved dependency graph and conflicts.</returns>
    public MavenDependencyResolutionResult ResolveDependencies(
        MavenDependency[] rootDependencies,
        MavenDependencyModel[] availableModels);

    /// <summary>
    /// Resolves Maven dependencies using root dependency-management constraints.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to resolve.</param>
    /// <param name="rootDependencyManagement">The root dependency-management constraints.</param>
    /// <param name="availableModels">The available dependency models to examine.</param>
    /// <returns>A result containing the resolved dependency graph and conflicts.</returns>
    public MavenDependencyResolutionResult ResolveDependenciesWithManagement(
        MavenDependency[] rootDependencies,
        MavenDependencyManagement rootDependencyManagement,
        MavenDependencyModel[] availableModels);
}
