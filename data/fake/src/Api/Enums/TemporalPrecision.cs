namespace fake;

/// <summary>
/// Selects the fractional-second precision of temporal values and text.
/// </summary>
public enum TemporalPrecision
{
    /// <summary>
    /// Truncates to whole seconds.
    /// </summary>
    Seconds = 0,

    /// <summary>
    /// Truncates to milliseconds.
    /// </summary>
    Milliseconds = 1,

    /// <summary>
    /// Truncates to microseconds.
    /// </summary>
    Microseconds = 2,

    /// <summary>
    /// Preserves all 100-nanosecond .NET ticks.
    /// </summary>
    Ticks = 3
}
