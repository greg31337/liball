namespace version;

/// <summary>
/// Defines the supported range values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum RangeSyntax
{
    /// <summary>
    /// Indicates that no specific value is selected.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Uses node semver behavior.
    /// </summary>
    NodeSemVer = 1,
    /// <summary>
    /// Uses cargo behavior.
    /// </summary>
    Cargo = 2,
    /// <summary>
    /// Uses nuget behavior.
    /// </summary>
    NuGet = 3,
    /// <summary>
    /// Uses maven behavior.
    /// </summary>
    Maven = 4,
    /// <summary>
    /// Uses rubygems behavior.
    /// </summary>
    RubyGems = 5,
    /// <summary>
    /// Uses python behavior.
    /// </summary>
    Python = 6
}
