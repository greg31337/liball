namespace version;

/// <summary>
/// Provides public Maven version, range, metadata, artifact, and dependency resolution operations.
/// </summary>
/// <remarks>
/// The service evaluates only caller-supplied metadata and dependency models and does not contact repositories.
/// </remarks>
public class MavenOperations : IMavenResolution
{
    /// <summary>
    /// Initializes a stateless Maven operations service.
    /// </summary>
    public MavenOperations()
    {
    }

    /// <summary>
    /// Parses Maven ComparableVersion text without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed Maven version or diagnostics.</returns>
    public VersionParseResult ParseVersion(string text)
    {
        return MavenVersionParser.Parse(text);
    }

    /// <summary>
    /// Normalizes Maven ComparableVersion text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The normalized Maven version text.</returns>
    public string NormalizeVersion(string text)
    {
        VersionParseResult result = MavenVersionParser.Parse(text);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid Maven version.", "text");
        }

        return MavenVersionFormatter.FormatNormalized(text);
    }

    /// <summary>
    /// Parses a Maven soft requirement, interval, interval union, or exclusion without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed range or diagnostics.</returns>
    public VersionRangeParseResult ParseRange(string text)
    {
        return MavenRangeParser.Parse(text);
    }

    /// <summary>
    /// Compares two Maven versions using ComparableVersion ordering.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareVersions(string leftText, string rightText)
    {
        return MavenVersionComparer.Compare(leftText, rightText);
    }

    /// <summary>
    /// Parses Maven repository metadata without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="metadataXml">The metadata XML to process.</param>
    /// <returns>A result containing the parsed metadata or diagnostics.</returns>
    public MavenMetadataParseResult ParseMetadata(string metadataXml)
    {
        return MavenMetadataParser.ParseWithDiagnostics(metadataXml);
    }

    /// <summary>
    /// Parses the available version list from Maven repository metadata.
    /// </summary>
    /// <param name="metadataXml">The metadata XML to process.</param>
    /// <returns>The available version text in metadata order.</returns>
    public string[] ParseMetadataVersions(string metadataXml)
    {
        return MavenMetadataParser.ParseVersions(metadataXml);
    }

    /// <summary>
    /// Parses the snapshot artifact entries from Maven repository metadata.
    /// </summary>
    /// <param name="metadataXml">The metadata XML to process.</param>
    /// <returns>The snapshot artifact entries in metadata order.</returns>
    public MavenSnapshotVersionMetadata[] ParseSnapshotVersions(string metadataXml)
    {
        return MavenMetadataParser.ParseSnapshotVersions(metadataXml);
    }

    /// <summary>
    /// Resolves the declared release or highest non-snapshot repository version.
    /// </summary>
    /// <param name="metadata">The repository metadata to examine.</param>
    /// <returns>The resolved artifact identity, or null when no release is available.</returns>
    public MavenArtifactVersionIdentity? ResolveReleaseVersion(MavenRepositoryMetadata metadata)
    {
        return MavenRepositoryVersionResolver.ResolveReleaseVersion(metadata);
    }

    /// <summary>
    /// Resolves the declared latest or highest repository version.
    /// </summary>
    /// <param name="metadata">The repository metadata to examine.</param>
    /// <returns>The resolved artifact identity, or null when no version is available.</returns>
    public MavenArtifactVersionIdentity? ResolveLatestVersion(MavenRepositoryMetadata metadata)
    {
        return MavenRepositoryVersionResolver.ResolveLatestVersion(metadata);
    }

    /// <summary>
    /// Selects the highest repository version accepted by an explicit Maven interval range.
    /// </summary>
    /// <param name="metadata">The repository metadata to examine.</param>
    /// <param name="rangeText">The explicit Maven interval range to apply.</param>
    /// <returns>The resolved artifact identity, or null when no version matches.</returns>
    public MavenArtifactVersionIdentity? ResolveHighestRangeVersion(
        MavenRepositoryMetadata metadata,
        string rangeText)
    {
        return MavenRepositoryVersionResolver.ResolveHighestRangeVersion(metadata, rangeText);
    }

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
        string? classifier)
    {
        return MavenRepositoryVersionResolver.ResolveSnapshotVersion(metadata, extension, classifier);
    }

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
        string? classifier)
    {
        return MavenRepositoryVersionResolver.ResolveSnapshotVersionWithDiagnostics(
            metadata,
            extension,
            classifier);
    }

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
        string? classifier)
    {
        return MavenSnapshotVersionResolver.ResolveSnapshotVersion(
            snapshotIdentity,
            metadata,
            extension,
            classifier);
    }

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
        string? classifier)
    {
        return MavenSnapshotVersionResolver.ResolveSnapshotVersion(
            groupIdText,
            artifactIdText,
            baseSnapshotVersionText,
            metadata,
            extension,
            classifier);
    }

    /// <summary>
    /// Resolves a release or timestamped snapshot artifact from artifact metadata.
    /// </summary>
    /// <param name="artifactMetadata">The artifact metadata to examine.</param>
    /// <returns>A result containing the resolved artifact identity or diagnostics.</returns>
    public MavenArtifactVersionResolutionResult ResolveArtifactVersion(
        MavenArtifactMetadata artifactMetadata)
    {
        System.ArgumentNullException.ThrowIfNull(artifactMetadata);

        MavenArtifactVersionIdentity identity = artifactMetadata.GetIdentity();
        if (!artifactMetadata.HasSnapshotMetadata())
        {
            return MavenArtifactVersionResolutionResult.CreateSuccess(identity);
        }

        MavenSnapshotMetadata snapshot = artifactMetadata.GetSnapshotMetadata();
        if (!snapshot.GetSnapshotIdentity().HasSameIdentity(identity)
            || !MavenSnapshotVersionResolver.EndsWithSnapshot(identity.GetVersionText()))
        {
            return CreateArtifactFailure(
                "Maven artifact snapshot metadata does not match its artifact identity.");
        }

        if (snapshot.IsLocalCopy())
        {
            return MavenArtifactVersionResolutionResult.CreateSuccess(identity);
        }

        if (!snapshot.HasTimestamp() || snapshot.GetBuildNumber() <= 0)
        {
            return CreateArtifactFailure(
                "Remote Maven artifact snapshot metadata requires a timestamp and build number.");
        }

        string? classifier = null;
        if (identity.HasClassifier())
        {
            classifier = identity.GetClassifier();
        }

        MavenArtifactVersionIdentity resolved = MavenSnapshotVersionResolver.CreateResolvedIdentity(
            identity,
            identity.GetPackaging(),
            classifier,
            MavenSnapshotVersionResolver.CreateTimestampedVersion(
                identity.GetVersionText(),
                snapshot));
        return MavenArtifactVersionResolutionResult.CreateSuccess(resolved);
    }

    /// <summary>
    /// Resolves Maven dependencies from the supplied models.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to resolve.</param>
    /// <param name="availableModels">The available dependency models to examine.</param>
    /// <returns>A result containing the resolved dependency graph and conflicts.</returns>
    public MavenDependencyResolutionResult ResolveDependencies(
        MavenDependency[] rootDependencies,
        MavenDependencyModel[] availableModels)
    {
        return MavenDependencyResolver.Resolve(rootDependencies, availableModels);
    }

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
        MavenDependencyModel[] availableModels)
    {
        return MavenDependencyResolver.Resolve(
            rootDependencies,
            rootDependencyManagement,
            availableModels);
    }

    internal MavenArtifactVersionResolutionResult CreateArtifactFailure(string message)
    {
        return MavenArtifactVersionResolutionResult.CreateFailure(
            VersionDiagnostic.FromIndex(
                MavenMetadataDiagnosticCodes.MissingSnapshotMetadata,
                message,
                "",
                0));
    }
}
