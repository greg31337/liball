namespace version;

/// <summary>
/// Defines stable diagnostic codes emitted by RubyGems Requirement operations.
/// </summary>
/// <remarks>
/// Compare these constants with diagnostic codes returned by result objects; messages may provide additional context for a specific input.
/// </remarks>
public static class RubyGemsRequirementDiagnosticCodes
{
    /// <summary>
    /// Identifies the missing requirement condition.
    /// </summary>
    public const string MissingRequirement = "RUBYGEMS_REQUIREMENT_MISSING_REQUIREMENT";
    /// <summary>
    /// Identifies the missing version condition.
    /// </summary>
    public const string MissingVersion = "RUBYGEMS_REQUIREMENT_MISSING_VERSION";
    /// <summary>
    /// Identifies the invalid operator condition.
    /// </summary>
    public const string InvalidOperator = "RUBYGEMS_REQUIREMENT_INVALID_OPERATOR";
    /// <summary>
    /// Identifies the invalid version condition.
    /// </summary>
    public const string InvalidVersion = "RUBYGEMS_REQUIREMENT_INVALID_VERSION";
}
