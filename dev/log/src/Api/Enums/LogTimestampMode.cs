namespace log;

/// <summary>
/// Selects how event timestamps are rendered by built-in formatters.
/// </summary>
public enum LogTimestampMode
{
    /// <summary>
    /// Renders timestamps in Coordinated Universal Time.
    /// </summary>
    Utc = 0,
    /// <summary>
    /// Renders timestamps in the process local time zone.
    /// </summary>
    Local = 1
}
