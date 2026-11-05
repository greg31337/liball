namespace version;

/// <summary>
/// Defines the supported version dialect values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum VersionDialect
{
    /// <summary>
    /// Indicates that no specific value is selected.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Uses semver behavior.
    /// </summary>
    SemVer = 1,
    /// <summary>
    /// Uses node semver behavior.
    /// </summary>
    NodeSemVer = 2,
    /// <summary>
    /// Uses nuget behavior.
    /// </summary>
    NuGet = 3,
    /// <summary>
    /// Uses maven behavior.
    /// </summary>
    Maven = 4,
    /// <summary>
    /// Uses go behavior.
    /// </summary>
    Go = 5,
    /// <summary>
    /// Uses cargo behavior.
    /// </summary>
    Cargo = 6,
    /// <summary>
    /// Uses rubygems behavior.
    /// </summary>
    RubyGems = 7,
    /// <summary>
    /// Uses python behavior.
    /// </summary>
    Python = 8
}
