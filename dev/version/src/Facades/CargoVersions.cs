namespace version;

/// <summary>
/// Provides the public entry points for Cargo version operations.
/// </summary>
/// <remarks>
/// Parsing methods return result objects for ordinary invalid input. Comparison, formatting, and resolver helpers apply the rules of this facade's ecosystem.
/// </remarks>
internal static class CargoVersions
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
        VersionParseResult semVerResult = SemVerParser.ParseStrict(text);
        if (semVerResult.IsFailure())
        {
            return VersionParseResult.CreateFailure(text, VersionDialect.Cargo, semVerResult.GetDiagnostics());
        }

        UniversalVersion semVerVersion = semVerResult.GetVersion();
        UniversalVersion cargoVersion = new UniversalVersion(
            text,
            VersionDialect.Cargo,
            semVerVersion.GetNumericComponents(),
            semVerVersion.GetPrereleaseIdentifiers(),
            semVerVersion.GetBuildIdentifiers(),
            semVerVersion.GetMissingComponentCount(),
            semVerVersion.HasLeadingVPrefix(),
            semVerVersion.HasRevisionComponent(),
            semVerVersion.GetRevisionComponent(),
            null);

        return VersionParseResult.CreateSuccess(text, VersionDialect.Cargo, cargoVersion);
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
            VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.Cargo));
    }

    /// <summary>
    /// Parses a dependency version requirement from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseRequirement(string text)
    {
        return CargoRangeParser.Parse(text);
    }

    /// <summary>
    /// Parses a dependency version requirement from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseRequirement(string text, VersionRangeParseOptions options)
    {
        return VersionOptionDispatcher.ParseRange(
            text,
            VersionOptionDispatcher.CreateRangeOptionsForSyntax(options, RangeSyntax.Cargo));
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
        return ParseRequirement(text);
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
        return ParseRequirement(text, options);
    }

    /// <summary>
    /// Parses a manifest from text.
    /// </summary>
    /// <remarks>
    /// The complete input is interpreted according to the dialect represented by this API.
    /// </remarks>
    /// <param name="manifestText">The manifest text to process.</param>
    /// <returns>The resulting cargo manifest.</returns>
    public static CargoManifest ParseManifest(string manifestText)
    {
        return CargoManifestParser.Parse(manifestText);
    }

    /// <summary>
    /// Resolves a features from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="manifest">The manifest used by the operation.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="requestedFeatures">The requested features to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="includeDefaultFeatures">Whether to enable include default features.</param>
    /// <param name="includeDevelopmentDependencies">Whether to enable include development dependencies.</param>
    /// <returns>The resulting cargo feature resolution.</returns>
    public static CargoFeatureResolution ResolveFeatures(
        CargoManifest manifest,
        CargoResolverOptions options,
        string[] requestedFeatures,
        bool includeDefaultFeatures,
        bool includeDevelopmentDependencies)
    {
        return CargoFeatureResolver.Resolve(
            manifest,
            options,
            requestedFeatures,
            includeDefaultFeatures,
            includeDevelopmentDependencies);
    }

    /// <summary>
    /// Filters the supplied values using candidates rules.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static CargoPackageMetadata[] FilterCandidates(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options)
    {
        return CargoCandidateFilter.Filter(dependency, candidates, options);
    }

    /// <summary>
    /// Filters the supplied values using candidates rules.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="selectedFeatures">The selected features used by the operation.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static CargoPackageMetadata[] FilterCandidates(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options,
        CargoResolvedDependencyFeatures? selectedFeatures)
    {
        return CargoCandidateFilter.Filter(dependency, candidates, options, selectedFeatures);
    }

    /// <summary>
    /// Selects a highest candidate from the supplied candidates.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting cargo package metadata.</returns>
    public static CargoPackageMetadata? SelectHighestCandidate(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options)
    {
        return CargoCandidateFilter.SelectHighest(dependency, candidates, options);
    }

    /// <summary>
    /// Selects a highest candidate from the supplied candidates.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="selectedFeatures">The selected features used by the operation.</param>
    /// <returns>The resulting cargo package metadata.</returns>
    public static CargoPackageMetadata? SelectHighestCandidate(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options,
        CargoResolvedDependencyFeatures? selectedFeatures)
    {
        return CargoCandidateFilter.SelectHighest(dependency, candidates, options, selectedFeatures);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static CargoDependencyResolutionResult ResolveDependencies(
        CargoDependency[] rootDependencies,
        CargoPackageMetadata[] availablePackages,
        CargoResolverOptions options)
    {
        return CargoDependencyResolver.Resolve(rootDependencies, availablePackages, options);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="manifest">The manifest used by the operation.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static CargoDependencyResolutionResult ResolveDependencies(
        CargoManifest manifest,
        CargoPackageMetadata[] availablePackages,
        CargoResolverOptions options)
    {
        return CargoDependencyResolver.Resolve(manifest, availablePackages, options);
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
        UniversalVersion left = ParseVersion(leftText);
        UniversalVersion right = ParseVersion(rightText);
        return left.CompareToVersion(right, VersionComparisonMode.Precedence);
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
            VersionOptionDispatcher.CreateComparisonOptionsForDialect(options, VersionDialect.Cargo));
    }

    internal static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = Parse(text);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid Cargo version text.", "text");
        }

        return result.GetVersion();
    }
}
