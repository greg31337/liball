namespace version;

/// <summary>
/// Defines the supported prerelease values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum PrereleasePolicy
{
    /// <summary>
    /// Uses default behavior.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Uses include behavior.
    /// </summary>
    Include = 1,
    /// <summary>
    /// Uses exclude behavior.
    /// </summary>
    Exclude = 2,
    /// <summary>
    /// Uses explicit only behavior.
    /// </summary>
    ExplicitOnly = 3,
    /// <summary>
    /// Uses auto if no stable candidate behavior.
    /// </summary>
    AutoIfNoStableCandidate = 4
}
