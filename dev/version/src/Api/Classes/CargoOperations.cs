namespace version;

/// <summary>
/// Provides public Cargo version, requirement, manifest, feature, candidate, and dependency operations.
/// </summary>
/// <remarks>
/// The service evaluates only caller-supplied manifests and finite package metadata.
/// </remarks>
public class CargoOperations : ICargoResolution
{
    /// <summary>
    /// Initializes a stateless Cargo operations service.
    /// </summary>
    public CargoOperations()
    {
    }

    /// <summary>
    /// Parses a Cargo package version without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed Cargo version or diagnostics.</returns>
    public VersionParseResult ParseVersion(string text)
    {
        return CargoVersions.Parse(text);
    }

    /// <summary>
    /// Compares two Cargo package versions using Semantic Versioning precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareVersions(string leftText, string rightText)
    {
        return CargoVersions.Compare(leftText, rightText);
    }

    /// <summary>
    /// Parses a Cargo dependency version requirement without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed requirement or diagnostics.</returns>
    public VersionRangeParseResult ParseRequirement(string text)
    {
        return CargoRangeParser.Parse(text);
    }

    /// <summary>
    /// Parses Cargo manifest text.
    /// </summary>
    /// <param name="manifestText">The manifest text to process.</param>
    /// <returns>The resulting Cargo manifest.</returns>
    public CargoManifest ParseManifest(string manifestText)
    {
        return CargoManifestParser.Parse(manifestText);
    }

    /// <summary>
    /// Resolves the enabled features and active dependencies for a Cargo manifest.
    /// </summary>
    /// <param name="manifest">The manifest used by the operation.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="requestedFeatures">The requested features to examine.</param>
    /// <param name="includeDefaultFeatures">Whether default features are included.</param>
    /// <param name="includeDevelopmentDependencies">Whether development dependencies are included.</param>
    /// <returns>The resulting Cargo feature resolution.</returns>
    public CargoFeatureResolution ResolveFeatures(
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
    /// Filters finite package candidates using requirement, prerelease, yanked, and source policies.
    /// </summary>
    /// <param name="dependency">The dependency to satisfy.</param>
    /// <param name="candidates">The package candidates to examine.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing matching candidates in input order.</returns>
    public CargoPackageMetadata[] FilterCandidates(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options)
    {
        return CargoCandidateFilter.Filter(dependency, candidates, options);
    }

    /// <summary>
    /// Filters finite package candidates and requires the selected dependency features.
    /// </summary>
    /// <param name="dependency">The dependency to satisfy.</param>
    /// <param name="candidates">The package candidates to examine.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="selectedFeatures">The selected dependency features to require.</param>
    /// <returns>A new array containing matching candidates in input order.</returns>
    public CargoPackageMetadata[] FilterCandidatesWithFeatures(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options,
        CargoResolvedDependencyFeatures selectedFeatures)
    {
        return CargoCandidateFilter.Filter(dependency, candidates, options, selectedFeatures);
    }

    /// <summary>
    /// Selects the highest package candidate accepted by the dependency and policies.
    /// </summary>
    /// <param name="dependency">The dependency to satisfy.</param>
    /// <param name="candidates">The package candidates to examine.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The highest matching candidate, or null when no candidate matches.</returns>
    public CargoPackageMetadata? SelectHighestCandidate(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options)
    {
        return CargoCandidateFilter.SelectHighest(dependency, candidates, options);
    }

    /// <summary>
    /// Selects the highest package candidate that also supports selected dependency features.
    /// </summary>
    /// <param name="dependency">The dependency to satisfy.</param>
    /// <param name="candidates">The package candidates to examine.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="selectedFeatures">The selected dependency features to require.</param>
    /// <returns>The highest matching candidate, or null when no candidate matches.</returns>
    public CargoPackageMetadata? SelectHighestCandidateWithFeatures(
        CargoDependency dependency,
        CargoPackageMetadata[] candidates,
        CargoResolverOptions options,
        CargoResolvedDependencyFeatures selectedFeatures)
    {
        return CargoCandidateFilter.SelectHighest(
            dependency,
            candidates,
            options,
            selectedFeatures);
    }

    /// <summary>
    /// Resolves Cargo dependencies from the supplied package metadata.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to examine.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="yankedVersions">The yanked versions to exclude unless the supplied options admit them.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the resolved dependency graph and conflicts.</returns>
    public CargoDependencyResolutionResult ResolveDependencies(
        CargoDependency[] rootDependencies,
        CargoPackageMetadata[] availablePackages,
        CargoYankedVersion[] yankedVersions,
        CargoResolverOptions options)
    {
        CargoPackageMetadata[] packages = ApplyYankedVersions(availablePackages, yankedVersions);
        return CargoDependencyResolver.Resolve(rootDependencies, packages, options);
    }

    /// <summary>
    /// Resolves dependencies activated by a Cargo manifest from supplied package metadata.
    /// </summary>
    /// <param name="manifest">The root manifest to resolve.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="yankedVersions">The yanked versions to exclude unless the supplied options admit them.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the resolved dependency graph and conflicts.</returns>
    public CargoDependencyResolutionResult ResolveManifestDependencies(
        CargoManifest manifest,
        CargoPackageMetadata[] availablePackages,
        CargoYankedVersion[] yankedVersions,
        CargoResolverOptions options)
    {
        CargoPackageMetadata[] packages = ApplyYankedVersions(availablePackages, yankedVersions);
        return CargoDependencyResolver.Resolve(manifest, packages, options);
    }

    internal CargoPackageMetadata[] ApplyYankedVersions(
        CargoPackageMetadata[] availablePackages,
        CargoYankedVersion[] yankedVersions)
    {
        System.ArgumentNullException.ThrowIfNull(availablePackages);
        System.ArgumentNullException.ThrowIfNull(yankedVersions);

        CargoPackageMetadata[] packages = new CargoPackageMetadata[availablePackages.Length];
        int packageIndex = 0;

        while (packageIndex < availablePackages.Length)
        {
            CargoPackageMetadata package = availablePackages[packageIndex];
            if (package == null)
            {
                throw new System.ArgumentException("Cargo package arrays cannot contain null values.");
            }

            bool yanked = package.IsYanked() || IsYanked(package.GetIdentity(), yankedVersions);
            if (yanked == package.IsYanked())
            {
                packages[packageIndex] = package;
            }
            else
            {
                packages[packageIndex] = new CargoPackageMetadata(
                    package.GetIdentity(),
                    package.GetDependencies(),
                    package.GetFeatures(),
                    package.GetRegistrySource(),
                    yanked);
            }

            packageIndex = packageIndex + 1;
        }

        return packages;
    }

    internal bool IsYanked(
        CargoPackageVersionIdentity identity,
        CargoYankedVersion[] yankedVersions)
    {
        int index = 0;

        while (index < yankedVersions.Length)
        {
            if (yankedVersions[index] == null)
            {
                throw new System.ArgumentException("Cargo yanked version arrays cannot contain null values.");
            }

            if (identity.HasSameIdentity(yankedVersions[index].GetIdentity()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
