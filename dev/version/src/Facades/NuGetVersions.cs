namespace version;

/// <summary>
/// Provides the public entry points for NuGet version operations.
/// </summary>
/// <remarks>
/// Parsing methods return result objects for ordinary invalid input. Comparison, formatting, and resolver helpers apply the rules of this facade's ecosystem.
/// </remarks>
internal static class NuGetVersions
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
        return NuGetVersionParser.Parse(text);
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
            VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.NuGet));
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
        return NuGetRangeParser.Parse(text);
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
            VersionOptionDispatcher.CreateRangeOptionsForSyntax(options, RangeSyntax.NuGet));
    }

    /// <summary>
    /// Parses a floating from text.
    /// </summary>
    /// <remarks>
    /// The complete input is interpreted according to the dialect represented by this API.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting nuget floating version.</returns>
    public static NuGetFloatingVersion ParseFloating(string text)
    {
        return NuGetFloatingVersion.Parse(text);
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
        return NuGetVersionComparer.Compare(leftText, rightText);
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
            VersionOptionDispatcher.CreateComparisonOptionsForDialect(options, VersionDialect.NuGet));
    }

    /// <summary>
    /// Parses a package metadata from text.
    /// </summary>
    /// <remarks>
    /// The complete input is interpreted according to the dialect represented by this API.
    /// </remarks>
    /// <param name="nuspecXml">The nuspec xml used by the operation.</param>
    /// <returns>The resulting nuget package metadata.</returns>
    public static NuGetPackageMetadata ParsePackageMetadata(string nuspecXml)
    {
        return NuGetNuspecParser.ParsePackageMetadata(nuspecXml);
    }

    /// <summary>
    /// Parses a dependency groups from text.
    /// </summary>
    /// <remarks>
    /// The complete input is interpreted according to the dialect represented by this API.
    /// </remarks>
    /// <param name="nuspecXml">The nuspec xml used by the operation.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static NuGetDependencyGroup[] ParseDependencyGroups(string nuspecXml)
    {
        return NuGetNuspecParser.ParseDependencyGroups(nuspecXml);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NuGetDependencyResolutionResult ResolveDependencies(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages)
    {
        return NuGetDependencyGraphResolver.Resolve(rootDependencies, availablePackages);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="rejectionPolicy">The rejection policy used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NuGetDependencyResolutionResult ResolveDependencies(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        return NuGetDependencyGraphResolver.Resolve(rootDependencies, availablePackages, rejectionPolicy);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="projectFrameworkText">The project framework text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NuGetDependencyResolutionResult ResolveDependencies(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        string projectFrameworkText)
    {
        return NuGetDependencyGraphResolver.Resolve(rootDependencies, availablePackages, projectFrameworkText);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="projectFrameworkText">The project framework text to process.</param>
    /// <param name="rejectionPolicy">The rejection policy used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NuGetDependencyResolutionResult ResolveDependencies(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        string projectFrameworkText,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        return NuGetDependencyGraphResolver.Resolve(
            rootDependencies,
            availablePackages,
            projectFrameworkText,
            rejectionPolicy);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="projectFramework">The project framework used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NuGetDependencyResolutionResult ResolveDependencies(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetTargetFramework projectFramework)
    {
        return NuGetDependencyGraphResolver.Resolve(rootDependencies, availablePackages, projectFramework);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="projectFramework">The project framework used by the operation.</param>
    /// <param name="rejectionPolicy">The rejection policy used by the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NuGetDependencyResolutionResult ResolveDependencies(
        NuGetDependency[] rootDependencies,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetTargetFramework projectFramework,
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        return NuGetDependencyGraphResolver.Resolve(
            rootDependencies,
            availablePackages,
            projectFramework,
            rejectionPolicy);
    }

    /// <summary>
    /// Resolves a lowest applicable version from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="sourcePriorities">The source priorities to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting nuget source package metadata.</returns>
    public static NuGetSourcePackageMetadata? ResolveLowestApplicableVersion(
        NuGetDependency dependency,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        return NuGetPackageVersionResolver.ResolveLowestApplicableVersion(dependency, availablePackages, sourcePriorities);
    }

    /// <summary>
    /// Resolves a lowest applicable version from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="sourcePriorities">The source priorities to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="selectionOptions">The options that control the operation.</param>
    /// <returns>The resulting nuget source package metadata.</returns>
    public static NuGetSourcePackageMetadata? ResolveLowestApplicableVersion(
        NuGetDependency dependency,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities,
        VersionSelectionOptions selectionOptions)
    {
        return NuGetPackageVersionResolver.ResolveLowestApplicableVersion(dependency, availablePackages, sourcePriorities, selectionOptions);
    }

    /// <summary>
    /// Resolves a floating version from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="packageIdText">The package id text to process.</param>
    /// <param name="floatingVersionText">The floating version text to process.</param>
    /// <param name="availablePackages">The available packages to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="sourcePriorities">The source priorities to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting nuget source package metadata.</returns>
    public static NuGetSourcePackageMetadata? ResolveFloatingVersion(
        string packageIdText,
        string floatingVersionText,
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        return NuGetPackageVersionResolver.ResolveFloatingVersion(packageIdText, floatingVersionText, availablePackages, sourcePriorities);
    }

    /// <summary>
    /// Parses a target framework from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="targetFrameworkText">The target framework text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NuGetTargetFrameworkParseResult ParseTargetFramework(string? targetFrameworkText)
    {
        return NuGetTargetFrameworkParser.Parse(targetFrameworkText);
    }

    /// <summary>
    /// Parses a target framework fallback list from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="fallbackText">The fallback text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NuGetTargetFrameworkFallbackParseResult ParseTargetFrameworkFallbackList(string? fallbackText)
    {
        return NuGetTargetFrameworkParser.ParseFallbackList(fallbackText);
    }

    /// <summary>
    /// Reports whether target framework compatible.
    /// </summary>
    /// <param name="projectFrameworkText">The project framework text to process.</param>
    /// <param name="candidateFrameworkText">The candidate framework text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool IsTargetFrameworkCompatible(string projectFrameworkText, string candidateFrameworkText)
    {
        return NuGetTargetFrameworkCompatibility.IsCompatible(projectFrameworkText, candidateFrameworkText);
    }

    /// <summary>
    /// Reports whether target framework compatible.
    /// </summary>
    /// <param name="projectFrameworkText">The project framework text to process.</param>
    /// <param name="candidateFrameworkText">The candidate framework text to process.</param>
    /// <param name="fallbackFrameworkListText">The fallback framework list text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool IsTargetFrameworkCompatible(
        string projectFrameworkText,
        string candidateFrameworkText,
        string fallbackFrameworkListText)
    {
        return NuGetTargetFrameworkCompatibility.IsCompatible(
            projectFrameworkText,
            candidateFrameworkText,
            fallbackFrameworkListText);
    }

    /// <summary>
    /// Reports whether target framework compatible.
    /// </summary>
    /// <param name="projectFramework">The project framework used by the operation.</param>
    /// <param name="candidateFramework">The candidate framework used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool IsTargetFrameworkCompatible(
        NuGetTargetFramework projectFramework,
        NuGetTargetFramework candidateFramework)
    {
        return NuGetTargetFrameworkCompatibility.IsCompatible(projectFramework, candidateFramework);
    }

    /// <summary>
    /// Reports whether target framework compatible.
    /// </summary>
    /// <param name="projectFramework">The project framework used by the operation.</param>
    /// <param name="candidateFramework">The candidate framework used by the operation.</param>
    /// <param name="fallbackFrameworkListText">The fallback framework list text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool IsTargetFrameworkCompatible(
        NuGetTargetFramework projectFramework,
        NuGetTargetFramework candidateFramework,
        string fallbackFrameworkListText)
    {
        return NuGetTargetFrameworkCompatibility.IsCompatible(
            projectFramework,
            candidateFramework,
            fallbackFrameworkListText);
    }

    /// <summary>
    /// Selects a best dependency group from the supplied candidates.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="projectFrameworkText">The project framework text to process.</param>
    /// <param name="dependencyGroups">The dependency groups to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting nuget dependency group.</returns>
    public static NuGetDependencyGroup? SelectBestDependencyGroup(
        string projectFrameworkText,
        NuGetDependencyGroup[] dependencyGroups)
    {
        return NuGetTargetFrameworkCompatibility.SelectBestDependencyGroup(projectFrameworkText, dependencyGroups);
    }

    /// <summary>
    /// Selects a best dependency group from the supplied candidates.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="projectFrameworkText">The project framework text to process.</param>
    /// <param name="packageMetadata">The package metadata used by the operation.</param>
    /// <returns>The resulting nuget dependency group.</returns>
    public static NuGetDependencyGroup? SelectBestDependencyGroup(
        string projectFrameworkText,
        NuGetPackageMetadata packageMetadata)
    {
        return NuGetTargetFrameworkCompatibility.SelectBestDependencyGroup(projectFrameworkText, packageMetadata);
    }

    /// <summary>
    /// Selects a best dependency group from the supplied candidates.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="projectFramework">The project framework used by the operation.</param>
    /// <param name="dependencyGroups">The dependency groups to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting nuget dependency group.</returns>
    public static NuGetDependencyGroup? SelectBestDependencyGroup(
        NuGetTargetFramework projectFramework,
        NuGetDependencyGroup[] dependencyGroups)
    {
        return NuGetTargetFrameworkCompatibility.SelectBestDependencyGroup(projectFramework, dependencyGroups);
    }

    /// <summary>
    /// Selects a best dependency group from the supplied candidates.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="projectFramework">The project framework used by the operation.</param>
    /// <param name="packageMetadata">The package metadata used by the operation.</param>
    /// <returns>The resulting nuget dependency group.</returns>
    public static NuGetDependencyGroup? SelectBestDependencyGroup(
        NuGetTargetFramework projectFramework,
        NuGetPackageMetadata packageMetadata)
    {
        return NuGetTargetFrameworkCompatibility.SelectBestDependencyGroup(projectFramework, packageMetadata);
    }
}
