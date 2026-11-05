namespace version;

/// <summary>
/// Defines the supported node semver increment release values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum NodeSemVerIncrementReleaseType
{
    /// <summary>
    /// Uses major behavior.
    /// </summary>
    Major = 0,
    /// <summary>
    /// Uses minor behavior.
    /// </summary>
    Minor = 1,
    /// <summary>
    /// Uses patch behavior.
    /// </summary>
    Patch = 2,
    /// <summary>
    /// Uses premajor behavior.
    /// </summary>
    Premajor = 3,
    /// <summary>
    /// Uses preminor behavior.
    /// </summary>
    Preminor = 4,
    /// <summary>
    /// Uses prepatch behavior.
    /// </summary>
    Prepatch = 5,
    /// <summary>
    /// Uses prerelease behavior.
    /// </summary>
    Prerelease = 6,
    /// <summary>
    /// Uses release behavior.
    /// </summary>
    Release = 7
}
