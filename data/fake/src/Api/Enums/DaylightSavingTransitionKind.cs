namespace fake;

/// <summary>
/// Identifies the local-clock discontinuity represented by a daylight-saving transition.
/// </summary>
public enum DaylightSavingTransitionKind
{
    /// <summary>
    /// The local clock moves forward and a range of wall-clock values does not exist.
    /// </summary>
    Gap = 0,

    /// <summary>
    /// The local clock moves backward and a range of wall-clock values occurs twice.
    /// </summary>
    Overlap = 1
}
