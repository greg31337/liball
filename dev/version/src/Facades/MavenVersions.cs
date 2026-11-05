namespace version;

/// <summary>
/// Provides the public entry points for Maven version operations.
/// </summary>
/// <remarks>
/// Parsing methods return result objects for ordinary invalid input. Comparison, formatting, and resolver helpers apply the rules of this facade's ecosystem.
/// </remarks>
internal static class MavenVersions
{
    /// <summary>
    /// Parses version text using this API's ecosystem rules.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult Parse(string text)
    {
        return MavenVersionParser.Parse(text);
    }

    /// <summary>
    /// Parses version text using this API's ecosystem rules.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult Parse(string text, VersionParseOptions options)
    {
        return VersionOptionDispatcher.Parse(
            text,
            VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.Maven));
    }

    /// <summary>
    /// Parses a version range from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseRange(string text)
    {
        return MavenRangeParser.Parse(text);
    }

    /// <summary>
    /// Parses a version range from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseRange(string text, VersionRangeParseOptions options)
    {
        return VersionOptionDispatcher.ParseRange(
            text,
            VersionOptionDispatcher.CreateRangeOptionsForSyntax(options, RangeSyntax.Maven));
    }

    /// <summary>
    /// Compares two versions using the selected ecosystem precedence rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int Compare(string leftText, string rightText)
    {
        return MavenVersionComparer.Compare(leftText, rightText);
    }

    /// <summary>
    /// Compares two versions using the selected ecosystem precedence rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int Compare(string leftText, string rightText, VersionComparisonOptions options)
    {
        return VersionOptionDispatcher.Compare(
            leftText,
            rightText,
            VersionOptionDispatcher.CreateComparisonOptionsForDialect(options, VersionDialect.Maven));
    }

    /// <summary>
    /// Formats the supplied value as normalized text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public static string FormatNormalized(string text)
    {
        VersionParseResult result = Parse(text);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid Maven version.");
        }

        return result.GetVersion().ToNormalizedString();
    }

    /// <summary>
    /// Formats the supplied value as canonical text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public static string FormatCanonical(string text)
    {
        VersionParseResult result = Parse(text);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid Maven version.");
        }

        return result.GetVersion().ToCanonicalString();
    }

    /// <summary>
    /// Formats the supplied value as normalized text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string FormatNormalized(UniversalVersion version)
    {
        return VersionFormatter.FormatNormalized(version);
    }

    /// <summary>
    /// Formats the supplied value as canonical text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string FormatCanonical(UniversalVersion version)
    {
        return VersionFormatter.FormatCanonical(version);
    }

    /// <summary>
    /// Parses a metadata from text.
    /// </summary>
    /// <remarks>
    /// The complete input is interpreted according to the dialect represented by this API.
    /// </remarks>
    /// <param name="metadataXml">The metadata xml used by the operation.</param>
    /// <returns>The resulting maven repository metadata.</returns>
    public static MavenRepositoryMetadata ParseMetadata(string metadataXml)
    {
        return MavenMetadataParser.Parse(metadataXml);
    }

    /// <summary>
    /// Parses a metadata with diagnostics from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="metadataXml">The metadata xml used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static MavenMetadataParseResult ParseMetadataWithDiagnostics(string metadataXml)
    {
        return MavenMetadataParser.ParseWithDiagnostics(metadataXml);
    }

    /// <summary>
    /// Parses a metadata versions from text.
    /// </summary>
    /// <remarks>
    /// The complete input is interpreted according to the dialect represented by this API.
    /// </remarks>
    /// <param name="metadataXml">The metadata xml used by the operation.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static string[] ParseMetadataVersions(string metadataXml)
    {
        return MavenMetadataParser.ParseVersions(metadataXml);
    }

    /// <summary>
    /// Parses a snapshot versions from text.
    /// </summary>
    /// <remarks>
    /// The complete input is interpreted according to the dialect represented by this API.
    /// </remarks>
    /// <param name="metadataXml">The metadata xml used by the operation.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static MavenSnapshotVersionMetadata[] ParseSnapshotVersions(string metadataXml)
    {
        return MavenMetadataParser.ParseSnapshotVersions(metadataXml);
    }

    /// <summary>
    /// Resolves a release version from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="metadata">The metadata used by the operation.</param>
    /// <returns>The resulting maven artifact version identity.</returns>
    public static MavenArtifactVersionIdentity? ResolveReleaseVersion(MavenRepositoryMetadata metadata)
    {
        return MavenRepositoryVersionResolver.ResolveReleaseVersion(metadata);
    }

    /// <summary>
    /// Resolves a latest version from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="metadata">The metadata used by the operation.</param>
    /// <returns>The resulting maven artifact version identity.</returns>
    public static MavenArtifactVersionIdentity? ResolveLatestVersion(MavenRepositoryMetadata metadata)
    {
        return MavenRepositoryVersionResolver.ResolveLatestVersion(metadata);
    }

    /// <summary>
    /// Resolves a highest range version from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="metadata">The metadata used by the operation.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>The resulting maven artifact version identity.</returns>
    public static MavenArtifactVersionIdentity? ResolveHighestRangeVersion(MavenRepositoryMetadata metadata, string rangeText)
    {
        return MavenRepositoryVersionResolver.ResolveHighestRangeVersion(metadata, rangeText);
    }

    /// <summary>
    /// Resolves a snapshot version from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="metadata">The metadata used by the operation.</param>
    /// <param name="extension">The extension used by the operation.</param>
    /// <param name="classifier">The classifier used by the operation.</param>
    /// <returns>The resulting maven artifact version identity.</returns>
    public static MavenArtifactVersionIdentity? ResolveSnapshotVersion(
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier)
    {
        return MavenRepositoryVersionResolver.ResolveSnapshotVersion(metadata, extension, classifier);
    }

    /// <summary>
    /// Resolves a snapshot version with diagnostics from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="metadata">The metadata used by the operation.</param>
    /// <param name="extension">The extension used by the operation.</param>
    /// <param name="classifier">The classifier used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static MavenArtifactVersionResolutionResult ResolveSnapshotVersionWithDiagnostics(
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier)
    {
        return MavenRepositoryVersionResolver.ResolveSnapshotVersionWithDiagnostics(metadata, extension, classifier);
    }

    /// <summary>
    /// Resolves a snapshot version from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="snapshotIdentity">The snapshot identity used by the operation.</param>
    /// <param name="metadata">The metadata used by the operation.</param>
    /// <param name="extension">The extension used by the operation.</param>
    /// <param name="classifier">The classifier used by the operation.</param>
    /// <returns>The resulting maven artifact version identity.</returns>
    public static MavenArtifactVersionIdentity? ResolveSnapshotVersion(
        MavenArtifactVersionIdentity snapshotIdentity,
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier)
    {
        return MavenSnapshotVersionResolver.ResolveSnapshotVersion(snapshotIdentity, metadata, extension, classifier);
    }

    /// <summary>
    /// Resolves a snapshot version from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <param name="baseSnapshotVersionText">The base snapshot version text to process.</param>
    /// <param name="metadata">The metadata used by the operation.</param>
    /// <param name="extension">The extension used by the operation.</param>
    /// <param name="classifier">The classifier used by the operation.</param>
    /// <returns>The resulting maven artifact version identity.</returns>
    public static MavenArtifactVersionIdentity? ResolveSnapshotVersion(
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
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="availableModels">The available models to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static MavenDependencyResolutionResult ResolveDependencies(
        MavenDependency[] rootDependencies,
        MavenDependencyModel[] availableModels)
    {
        return MavenDependencyResolver.Resolve(rootDependencies, availableModels);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="rootDependencyManagement">The root dependency management used by the operation.</param>
    /// <param name="availableModels">The available models to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static MavenDependencyResolutionResult ResolveDependencies(
        MavenDependency[] rootDependencies,
        MavenDependencyManagement rootDependencyManagement,
        MavenDependencyModel[] availableModels)
    {
        return MavenDependencyResolver.Resolve(rootDependencies, rootDependencyManagement, availableModels);
    }
}
