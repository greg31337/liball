namespace version;

/// <summary>
/// Defines the supported nuget package availability values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum NuGetPackageAvailability
{
    /// <summary>
    /// Indicates that no specific value is selected.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Uses available behavior.
    /// </summary>
    Available = 1,
    /// <summary>
    /// Uses unavailable behavior.
    /// </summary>
    Unavailable = 2
}
