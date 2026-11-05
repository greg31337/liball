namespace chart;
/// <summary>
/// Specifies the supported tick kind values.
/// </summary>
public enum ChartTickKind
{
    /// <summary>
    /// Uses the automatic option for the tick kind.
    /// </summary>
    Automatic = 0,
    /// <summary>
    /// Uses the explicit option for the tick kind.
    /// </summary>
    Explicit = 1,
    /// <summary>
    /// Uses the numeric option for the tick kind.
    /// </summary>
    Numeric = 2,
    /// <summary>
    /// Uses the log option for the tick kind.
    /// </summary>
    Log = 3,
    /// <summary>
    /// Uses the time option for the tick kind.
    /// </summary>
    Time = 4,
    /// <summary>
    /// Uses the category option for the tick kind.
    /// </summary>
    Category = 5,
    /// <summary>
    /// Uses the radial option for the tick kind.
    /// </summary>
    Radial = 6,
    /// <summary>
    /// Uses the angular option for the tick kind.
    /// </summary>
    Angular = 7
}
