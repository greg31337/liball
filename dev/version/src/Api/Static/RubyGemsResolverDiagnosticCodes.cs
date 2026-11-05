namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by RubyGems Resolver operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class RubyGemsResolverDiagnosticCodes
{
    /// <summary>
    /// Identifies the unsatisfied constraints condition.
    /// </summary>
    public const string UnsatisfiedConstraints = "RUBYGEMS_RESOLVE_UNSATISFIED_CONSTRAINTS";
    /// <summary>
    /// Identifies the missing specification metadata condition.
    /// </summary>
    public const string MissingSpecificationMetadata = "RUBYGEMS_RESOLVE_MISSING_SPECIFICATION_METADATA";
    /// <summary>
    /// Identifies the cyclic dependency condition.
    /// </summary>
    public const string CyclicDependency = "RUBYGEMS_RESOLVE_CYCLIC_DEPENDENCY";
    /// <summary>
    /// Identifies the backtracking limit exceeded condition.
    /// </summary>
    public const string BacktrackingLimitExceeded = "RUBYGEMS_RESOLVE_BACKTRACKING_LIMIT_EXCEEDED";
}
