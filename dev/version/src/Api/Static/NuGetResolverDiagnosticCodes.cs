namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by NuGet Resolver operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class NuGetResolverDiagnosticCodes
{
    /// <summary>
    /// Identifies the unsatisfied constraints condition.
    /// </summary>
    public const string UnsatisfiedConstraints = "NUGET_RESOLVE_UNSATISFIED_CONSTRAINTS";
    /// <summary>
    /// Identifies the cyclic dependency condition.
    /// </summary>
    public const string CyclicDependency = "NUGET_RESOLVE_CYCLIC_DEPENDENCY";
    /// <summary>
    /// Identifies the incompatible framework condition.
    /// </summary>
    public const string IncompatibleFramework = "NUGET_RESOLVE_INCOMPATIBLE_FRAMEWORK";
    /// <summary>
    /// Identifies the missing package metadata condition.
    /// </summary>
    public const string MissingPackageMetadata = "NUGET_RESOLVE_MISSING_PACKAGE_METADATA";
    /// <summary>
    /// Identifies the ambiguous source matches condition.
    /// </summary>
    public const string AmbiguousSourceMatches = "NUGET_RESOLVE_AMBIGUOUS_SOURCE_MATCHES";
}
