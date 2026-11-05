namespace log;

/// <summary>
/// Specifies the calendar interval used to select a rolling file name.
/// </summary>
public enum LogRollingInterval
{
    /// <summary>
    /// Disables time-based rolling.
    /// </summary>
    None = 0,
    /// <summary>
    /// Uses one file sequence per UTC hour.
    /// </summary>
    Hour = 1,
    /// <summary>
    /// Uses one file sequence per UTC day.
    /// </summary>
    Day = 2,
    /// <summary>
    /// Uses one file sequence per UTC month.
    /// </summary>
    Month = 3,
    /// <summary>
    /// Uses one file sequence per UTC year.
    /// </summary>
    Year = 4
}
