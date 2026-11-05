namespace version;

/// <summary>
/// Provides public NuGet version, range, package, source, and target-framework operations.
/// </summary>
/// <remarks>
/// The service evaluates only caller-supplied package metadata and does not contact package sources.
/// </remarks>
public class NuGetOperations : INuGetResolution
{
    /// <summary>
    /// Initializes a stateless NuGet operations service.
    /// </summary>
    public NuGetOperations()
    {
    }

    /// <summary>
    /// Parses NuGet version text without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed version or diagnostics.</returns>
    public VersionParseResult ParseVersion(string text)
    {
        return NuGetVersionParser.Parse(text);
    }

    /// <summary>
    /// Normalizes a NuGet package version, or returns an empty string when the input is invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The normalized package identity version text.</returns>
    public string NormalizeVersion(string text)
    {
        return NuGetVersionFormatter.Normalize(text);
    }

    /// <summary>
    /// Parses a NuGet interval range without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed range or diagnostics.</returns>
    public VersionRangeParseResult ParseRange(string text)
    {
        return NuGetRangeParser.Parse(text);
    }

    /// <summary>
    /// Parses a NuGet floating version pattern.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The parsed floating pattern, whose valid field reports whether parsing succeeded.</returns>
    public NuGetFloatingVersion ParseFloatingVersion(string text)
    {
        return NuGetFloatingVersion.Parse(text);
    }

    /// <summary>
    /// Compares two NuGet versions using case-insensitive prerelease precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareVersions(string leftText, string rightText)
    {
        return NuGetVersionComparer.Compare(leftText, rightText);
    }

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
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        return NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
            dependency,
            availablePackages,
            sourcePriorities);
    }

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
        VersionSelectionOptions selectionOptions)
    {
        return NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
            dependency,
            availablePackages,
            sourcePriorities,
            selectionOptions);
    }

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
        VersionSelectionOptions selectionOptions)
    {
        return NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
            dependency,
            availablePackages,
            sourcePriorities,
            rejectionPolicy,
            selectionOptions);
    }

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
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        return NuGetPackageVersionResolver.ResolveFloatingVersion(
            packageIdText,
            floatingVersionText,
            availablePackages,
            sourcePriorities);
    }

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
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        return NuGetPackageVersionResolver.ResolveFloatingVersion(
            packageIdText,
            floatingVersionText,
            availablePackages,
            sourcePriorities,
            rejectionPolicy);
    }

    /// <summary>
    /// Parses NuGet package metadata from nuspec XML.
    /// </summary>
    /// <param name="nuspecXml">The nuspec XML to process.</param>
    /// <returns>The parsed package metadata.</returns>
    public NuGetPackageMetadata ParsePackageMetadata(string nuspecXml)
    {
        return NuGetNuspecParser.ParsePackageMetadata(nuspecXml);
    }

    /// <summary>
    /// Parses NuGet dependency groups from nuspec XML.
    /// </summary>
    /// <param name="nuspecXml">The nuspec XML to process.</param>
    /// <returns>The parsed dependency groups.</returns>
    public NuGetDependencyGroup[] ParseDependencyGroups(string nuspecXml)
    {
        return NuGetNuspecParser.ParseDependencyGroups(nuspecXml);
    }

    /// <summary>
    /// Parses a NuGet target framework without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="targetFrameworkText">The target framework text to process.</param>
    /// <returns>A result containing the parsed target framework or diagnostics.</returns>
    public NuGetTargetFrameworkParseResult ParseTargetFramework(string? targetFrameworkText)
    {
        return NuGetTargetFrameworkParser.Parse(targetFrameworkText);
    }

    /// <summary>
    /// Parses a NuGet target framework fallback list without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="fallbackText">The fallback text to process.</param>
    /// <returns>A result containing the parsed fallback list or diagnostics.</returns>
    public NuGetTargetFrameworkFallbackParseResult ParseTargetFrameworkFallbackList(string? fallbackText)
    {
        return NuGetTargetFrameworkParser.ParseFallbackList(fallbackText);
    }

    /// <summary>
    /// Reports whether target-framework text is compatible.
    /// </summary>
    /// <param name="projectFrameworkText">The project target framework.</param>
    /// <param name="candidateFrameworkText">The candidate target framework.</param>
    /// <returns><c>true</c> when the candidate is compatible; otherwise, <c>false</c>.</returns>
    public bool IsTargetFrameworkCompatible(string projectFrameworkText, string candidateFrameworkText)
    {
        return NuGetTargetFrameworkCompatibility.IsCompatible(projectFrameworkText, candidateFrameworkText);
    }

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
        string fallbackFrameworkListText)
    {
        return NuGetTargetFrameworkCompatibility.IsCompatible(
            projectFrameworkText,
            candidateFrameworkText,
            fallbackFrameworkListText);
    }

    /// <summary>
    /// Reports whether parsed target frameworks are compatible.
    /// </summary>
    /// <param name="projectFramework">The project target framework.</param>
    /// <param name="candidateFramework">The candidate target framework.</param>
    /// <returns><c>true</c> when the candidate is compatible; otherwise, <c>false</c>.</returns>
    public bool AreTargetFrameworksCompatible(
        NuGetTargetFramework projectFramework,
        NuGetTargetFramework candidateFramework)
    {
        return NuGetTargetFrameworkCompatibility.IsCompatible(projectFramework, candidateFramework);
    }

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
        string fallbackFrameworkListText)
    {
        return NuGetTargetFrameworkCompatibility.IsCompatible(
            projectFramework,
            candidateFramework,
            fallbackFrameworkListText);
    }

    /// <summary>
    /// Selects the best dependency group for target-framework text.
    /// </summary>
    /// <param name="projectFrameworkText">The project target framework.</param>
    /// <param name="dependencyGroups">The dependency groups to examine.</param>
    /// <returns>The best compatible group, or null when none is compatible.</returns>
    public NuGetDependencyGroup? SelectBestDependencyGroup(
        string projectFrameworkText,
        NuGetDependencyGroup[] dependencyGroups)
    {
        return NuGetTargetFrameworkCompatibility.SelectBestDependencyGroup(
            projectFrameworkText,
            dependencyGroups);
    }

    /// <summary>
    /// Selects the best package dependency group for target-framework text.
    /// </summary>
    /// <param name="projectFrameworkText">The project target framework.</param>
    /// <param name="packageMetadata">The package metadata to examine.</param>
    /// <returns>The best compatible group, or null when none is compatible.</returns>
    public NuGetDependencyGroup? SelectBestPackageDependencyGroup(
        string projectFrameworkText,
        NuGetPackageMetadata packageMetadata)
    {
        return NuGetTargetFrameworkCompatibility.SelectBestDependencyGroup(
            projectFrameworkText,
            packageMetadata);
    }

    /// <summary>
    /// Selects the best dependency group for a parsed target framework.
    /// </summary>
    /// <param name="projectFramework">The project target framework.</param>
    /// <param name="dependencyGroups">The dependency groups to examine.</param>
    /// <returns>The best compatible group, or null when none is compatible.</returns>
    public NuGetDependencyGroup? SelectBestParsedDependencyGroup(
        NuGetTargetFramework projectFramework,
        NuGetDependencyGroup[] dependencyGroups)
    {
        return NuGetTargetFrameworkCompatibility.SelectBestDependencyGroup(
            projectFramework,
            dependencyGroups);
    }

    /// <summary>
    /// Selects the best package dependency group for a parsed target framework.
    /// </summary>
    /// <param name="projectFramework">The project target framework.</param>
    /// <param name="packageMetadata">The package metadata to examine.</param>
    /// <returns>The best compatible group, or null when none is compatible.</returns>
    public NuGetDependencyGroup? SelectBestParsedPackageDependencyGroup(
        NuGetTargetFramework projectFramework,
        NuGetPackageMetadata packageMetadata)
    {
        return NuGetTargetFrameworkCompatibility.SelectBestDependencyGroup(
            projectFramework,
            packageMetadata);
    }

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
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        return NuGetDependencyGraphResolver.Resolve(
            rootDependencies,
            PreparePackagesForResolution(availablePackages, sourcePriorities));
    }

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
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        return NuGetDependencyGraphResolver.Resolve(
            rootDependencies,
            PreparePackagesForResolution(availablePackages, sourcePriorities),
            rejectionPolicy);
    }

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
        string projectFrameworkText)
    {
        return NuGetDependencyGraphResolver.Resolve(
            rootDependencies,
            PreparePackagesForResolution(availablePackages, sourcePriorities),
            projectFrameworkText);
    }

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
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        return NuGetDependencyGraphResolver.Resolve(
            rootDependencies,
            PreparePackagesForResolution(availablePackages, sourcePriorities),
            projectFrameworkText,
            rejectionPolicy);
    }

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
        NuGetTargetFramework projectFramework)
    {
        return NuGetDependencyGraphResolver.Resolve(
            rootDependencies,
            PreparePackagesForResolution(availablePackages, sourcePriorities),
            projectFramework);
    }

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
        NuGetPackageRejectionPolicy rejectionPolicy)
    {
        return NuGetDependencyGraphResolver.Resolve(
            rootDependencies,
            PreparePackagesForResolution(availablePackages, sourcePriorities),
            projectFramework,
            rejectionPolicy);
    }

    internal NuGetSourcePackageMetadata[] PreparePackagesForResolution(
        NuGetSourcePackageMetadata[] availablePackages,
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        System.ArgumentNullException.ThrowIfNull(availablePackages);
        System.ArgumentNullException.ThrowIfNull(sourcePriorities);
        NuGetPackageVersionResolver.ValidateAvailablePackages(availablePackages);
        NuGetPackageVersionResolver.ValidateSourcePriorities(sourcePriorities);

        if (sourcePriorities.Length == 0)
        {
            NuGetSourcePackageMetadata[] copy = new NuGetSourcePackageMetadata[availablePackages.Length];
            System.Array.Copy(availablePackages, copy, availablePackages.Length);
            return copy;
        }

        System.Collections.Generic.List<NuGetSourcePackageMetadata> selected =
            new System.Collections.Generic.List<NuGetSourcePackageMetadata>();
        int packageIndex = 0;

        while (packageIndex < availablePackages.Length)
        {
            NuGetSourcePackageMetadata package = availablePackages[packageIndex];
            NuGetPackageSourcePriority? priority = NuGetPackageVersionResolver.FindSourcePriority(
                sourcePriorities,
                package.GetSource());

            if (priority != null && priority.IsEnabled())
            {
                AddPreferredPackage(selected, package, sourcePriorities);
            }

            packageIndex = packageIndex + 1;
        }

        return selected.ToArray();
    }

    internal void AddPreferredPackage(
        System.Collections.Generic.List<NuGetSourcePackageMetadata> selected,
        NuGetSourcePackageMetadata package,
        NuGetPackageSourcePriority[] sourcePriorities)
    {
        int index = 0;

        while (index < selected.Count)
        {
            if (selected[index].GetIdentity().HasSameIdentity(package.GetIdentity()))
            {
                if (NuGetPackageVersionResolver.CompareSourcePriority(
                    package,
                    selected[index],
                    sourcePriorities) < 0)
                {
                    selected[index] = package;
                }

                return;
            }

            index = index + 1;
        }

        selected.Add(package);
    }
}
