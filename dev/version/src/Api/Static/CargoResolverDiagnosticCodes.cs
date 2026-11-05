namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by Cargo Resolver operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class CargoResolverDiagnosticCodes
{
    /// <summary>
    /// Identifies the unsatisfied constraints condition.
    /// </summary>
    public const string UnsatisfiedConstraints = "CARGO_RESOLVE_UNSATISFIED_CONSTRAINTS";
    /// <summary>
    /// Identifies the missing package metadata condition.
    /// </summary>
    public const string MissingPackageMetadata = "CARGO_RESOLVE_MISSING_PACKAGE_METADATA";
    /// <summary>
    /// Identifies the cyclic dependency condition.
    /// </summary>
    public const string CyclicDependency = "CARGO_RESOLVE_CYCLIC_DEPENDENCY";
    /// <summary>
    /// Identifies the backtracking limit exceeded condition.
    /// </summary>
    public const string BacktrackingLimitExceeded = "CARGO_RESOLVE_BACKTRACKING_LIMIT_EXCEEDED";
}
