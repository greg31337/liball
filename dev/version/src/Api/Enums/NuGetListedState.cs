namespace version;

/// <summary>
/// Defines the supported nuget listed values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum NuGetListedState
{
    /// <summary>
    /// Indicates that no specific value is selected.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Uses listed behavior.
    /// </summary>
    Listed = 1,
    /// <summary>
    /// Uses unlisted behavior.
    /// </summary>
    Unlisted = 2
}
