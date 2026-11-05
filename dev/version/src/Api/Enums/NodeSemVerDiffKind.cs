namespace version;

/// <summary>
/// Defines the supported node semver diff values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum NodeSemVerDiffKind
{
    /// <summary>
    /// Indicates that no specific value is selected.
    /// </summary>
    None = 0,
    /// <summary>
    /// Uses major behavior.
    /// </summary>
    Major = 1,
    /// <summary>
    /// Uses minor behavior.
    /// </summary>
    Minor = 2,
    /// <summary>
    /// Uses patch behavior.
    /// </summary>
    Patch = 3,
    /// <summary>
    /// Uses prerelease behavior.
    /// </summary>
    Prerelease = 4,
    /// <summary>
    /// Uses build behavior.
    /// </summary>
    Build = 5
}
