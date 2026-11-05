namespace version;

/// <summary>
/// Defines public NuGet version, range, package, source, and target-framework operations.
/// </summary>
public interface INuGetResolution
{
    /// <summary>
    /// Parses NuGet version text without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed version or diagnostics.</returns>
    public VersionParseResult ParseVersion(string text);

    /// <summary>
    /// Normalizes a NuGet package version, or returns an empty string when the input is invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The normalized package identity version text.</returns>
    public string NormalizeVersion(string text);

    /// <summary>
    /// Parses a NuGet interval range without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed range or diagnostics.</returns>
    public VersionRangeParseResult ParseRange(string text);

    /// <summary>
    /// Parses a NuGet floating version pattern.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The parsed floating pattern, whose valid field reports whether parsing succeeded.</returns>
    public NuGetFloatingVersion ParseFloatingVersion(string text);

    /// <summary>
    /// Compares two NuGet versions using case-insensitive prerelease precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareVersions(string leftText, string rightText);

    /// <summary>
    /// Selects the lowest applicable package version.
    /// </summary>
    /// <param name="dependency">The dependency to satisfy.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <returns>The selected package, or null when no package is applicable.</returns>
    public NuGetSourcePackageMetadata? ResolveLowestApplicableVersion(
        NuGetDependency dependency,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities);

    /// <summary>
    /// Selects the lowest applicable package version using explicit selection options.
    /// </summary>
    /// <param name="dependency">The dependency to satisfy.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <param name="selectionOptions">The selection options to apply.</param>
    /// <returns>The selected package, or null when no package is applicable.</returns>
    public NuGetSourcePackageMetadata? ResolveLowestApplicableVersionWithOptions(
        NuGetDependency dependency,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        VersionSelectionOptions selectionOptions);

    /// <summary>
    /// Selects the lowest applicable package version using rejection and selection policies.
    /// </summary>
    /// <param name="dependency">The dependency to satisfy.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <param name="rejectionPolicy">The package rejection policy to apply.</param>
    /// <param name="selectionOptions">The selection options to apply.</param>
    /// <returns>The selected package, or null when no package is applicable.</returns>
    public NuGetSourcePackageMetadata? ResolveLowestApplicableVersionWithPolicies(
        NuGetDependency dependency,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        NuGetPackageRejectionPolicy rejectionPolicy,
        VersionSelectionOptions selectionOptions);

    /// <summary>
    /// Selects the highest package matching a floating version pattern.
    /// </summary>
    /// <param name="packageIdText">The package identifier text to match.</param>
    /// <param name="floatingVersionText">The floating version pattern to match.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <returns>The selected package, or null when no package is applicable.</returns>
    public NuGetSourcePackageMetadata? ResolveHighestFloatingVersion(
        string packageIdText,
        string floatingVersionText,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities);

    /// <summary>
    /// Selects the highest package matching a floating version pattern and rejection policy.
    /// </summary>
    /// <param name="packageIdText">The package identifier text to match.</param>
    /// <param name="floatingVersionText">The floating version pattern to match.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <param name="rejectionPolicy">The package rejection policy to apply.</param>
    /// <returns>The selected package, or null when no package is applicable.</returns>
    public NuGetSourcePackageMetadata? ResolveHighestFloatingVersionWithPolicy(
        string packageIdText,
        string floatingVersionText,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        NuGetPackageRejectionPolicy rejectionPolicy);

    /// <summary>
    /// Parses NuGet package metadata from nuspec XML.
    /// </summary>
    /// <param name="nuspecXml">The nuspec XML to process.</param>
    /// <returns>The parsed package metadata.</returns>
    public NuGetPackageMetadata ParsePackageMetadata(string nuspecXml);

    /// <summary>
    /// Parses NuGet dependency groups from nuspec XML.
    /// </summary>
    /// <param name="nuspecXml">The nuspec XML to process.</param>
    /// <returns>The parsed dependency groups.</returns>
    public NuGetDependencyGroup[] ParseDependencyGroups(string nuspecXml);

    /// <summary>
    /// Parses a NuGet target framework without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="targetFrameworkText">The target framework text to process.</param>
    /// <returns>A result containing the parsed target framework or diagnostics.</returns>
    public NuGetTargetFrameworkParseResult ParseTargetFramework(string? targetFrameworkText);

    /// <summary>
    /// Parses a NuGet target framework fallback list without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="fallbackText">The fallback text to process.</param>
    /// <returns>A result containing the parsed fallback list or diagnostics.</returns>
    public NuGetTargetFrameworkFallbackParseResult ParseTargetFrameworkFallbackList(string? fallbackText);

    /// <summary>
    /// Reports whether target-framework text is compatible.
    /// </summary>
    /// <param name="projectFrameworkText">The project target framework.</param>
    /// <param name="candidateFrameworkText">The candidate target framework.</param>
    /// <returns><c>true</c> when the candidate is compatible; otherwise, <c>false</c>.</returns>
    public bool IsTargetFrameworkCompatible(string projectFrameworkText, string candidateFrameworkText);

    /// <summary>
    /// Reports whether target-framework text is compatible through direct or fallback matching.
    /// </summary>
    /// <param name="projectFrameworkText">The project target framework.</param>
    /// <param name="candidateFrameworkText">The candidate target framework.</param>
    /// <param name="fallbackFrameworkListText">The fallback target frameworks.</param>
    /// <returns><c>true</c> when the candidate is compatible; otherwise, <c>false</c>.</returns>
    public bool IsTargetFrameworkCompatibleWithFallback(
        string projectFrameworkText,
        string candidateFrameworkText,
        string fallbackFrameworkListText);

    /// <summary>
    /// Reports whether parsed target frameworks are compatible.
    /// </summary>
    /// <param name="projectFramework">The project target framework.</param>
    /// <param name="candidateFramework">The candidate target framework.</param>
    /// <returns><c>true</c> when the candidate is compatible; otherwise, <c>false</c>.</returns>
    public bool AreTargetFrameworksCompatible(
        NuGetTargetFramework projectFramework,
        NuGetTargetFramework candidateFramework);

    /// <summary>
    /// Reports whether parsed target frameworks are compatible through direct or fallback matching.
    /// </summary>
    /// <param name="projectFramework">The project target framework.</param>
    /// <param name="candidateFramework">The candidate target framework.</param>
    /// <param name="fallbackFrameworkListText">The fallback target frameworks.</param>
    /// <returns><c>true</c> when the candidate is compatible; otherwise, <c>false</c>.</returns>
    public bool AreTargetFrameworksCompatibleWithFallback(
        NuGetTargetFramework projectFramework,
        NuGetTargetFramework candidateFramework,
        string fallbackFrameworkListText);

    /// <summary>
    /// Selects the best dependency group for target-framework text.
    /// </summary>
    /// <param name="projectFrameworkText">The project target framework.</param>
    /// <param name="dependencyGroups">The dependency groups to examine.</param>
    /// <returns>The best compatible group, or null when none is compatible.</returns>
    public NuGetDependencyGroup? SelectBestDependencyGroup(
        string projectFrameworkText,
        NuGetDependencyGroup[] dependencyGroups);

    /// <summary>
    /// Selects the best package dependency group for target-framework text.
    /// </summary>
    /// <param name="projectFrameworkText">The project target framework.</param>
    /// <param name="packageMetadata">The package metadata to examine.</param>
    /// <returns>The best compatible group, or null when none is compatible.</returns>
    public NuGetDependencyGroup? SelectBestPackageDependencyGroup(
        string projectFrameworkText,
        NuGetPackageMetadata packageMetadata);

    /// <summary>
    /// Selects the best dependency group for a parsed target framework.
    /// </summary>
    /// <param name="projectFramework">The project target framework.</param>
    /// <param name="dependencyGroups">The dependency groups to examine.</param>
    /// <returns>The best compatible group, or null when none is compatible.</returns>
    public NuGetDependencyGroup? SelectBestParsedDependencyGroup(
        NuGetTargetFramework projectFramework,
        NuGetDependencyGroup[] dependencyGroups);

    /// <summary>
    /// Selects the best package dependency group for a parsed target framework.
    /// </summary>
    /// <param name="projectFramework">The project target framework.</param>
    /// <param name="packageMetadata">The package metadata to examine.</param>
    /// <returns>The best compatible group, or null when none is compatible.</returns>
    public NuGetDependencyGroup? SelectBestParsedPackageDependencyGroup(
        NuGetTargetFramework projectFramework,
        NuGetPackageMetadata packageMetadata);

    /// <summary>
    /// Resolves NuGet dependencies using the supplied source priorities.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to resolve.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <returns>A result containing the resolved graph and conflicts.</returns>
    public NuGetDependencyResolutionResult ResolveDependencies(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities);

    /// <summary>
    /// Resolves NuGet dependencies using source and rejection policies.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to resolve.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <param name="rejectionPolicy">The package rejection policy to apply.</param>
    /// <returns>A result containing the resolved graph and conflicts.</returns>
    public NuGetDependencyResolutionResult ResolveDependenciesWithPolicy(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        NuGetPackageRejectionPolicy rejectionPolicy);

    /// <summary>
    /// Resolves NuGet dependencies for target-framework text.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to resolve.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <param name="projectFrameworkText">The project target framework.</param>
    /// <returns>A result containing the resolved graph and conflicts.</returns>
    public NuGetDependencyResolutionResult ResolveDependenciesForFramework(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        string projectFrameworkText);

    /// <summary>
    /// Resolves NuGet dependencies for target-framework text using source and rejection policies.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to resolve.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <param name="projectFrameworkText">The project target framework.</param>
    /// <param name="rejectionPolicy">The package rejection policy to apply.</param>
    /// <returns>A result containing the resolved graph and conflicts.</returns>
    public NuGetDependencyResolutionResult ResolveDependenciesForFrameworkWithPolicy(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        string projectFrameworkText,
        NuGetPackageRejectionPolicy rejectionPolicy);

    /// <summary>
    /// Resolves NuGet dependencies for a parsed target framework.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to resolve.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <param name="projectFramework">The project target framework.</param>
    /// <returns>A result containing the resolved graph and conflicts.</returns>
    public NuGetDependencyResolutionResult ResolveDependenciesForParsedFramework(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        NuGetTargetFramework projectFramework);

    /// <summary>
    /// Resolves NuGet dependencies for a parsed target framework using source and rejection policies.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to resolve.</param>
    /// <param name="availablePackages">The available packages to examine.</param>
    /// <param name="sourcePriorities">The source priorities to apply.</param>
    /// <param name="projectFramework">The project target framework.</param>
    /// <param name="rejectionPolicy">The package rejection policy to apply.</param>
    /// <returns>A result containing the resolved graph and conflicts.</returns>
    public NuGetDependencyResolutionResult ResolveDependenciesForParsedFrameworkWithPolicy(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        NuGetTargetFramework projectFramework,
        NuGetPackageRejectionPolicy rejectionPolicy);
}
