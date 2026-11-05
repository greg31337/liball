namespace log;

/// <summary>
/// Selects how structured event fields are ordered by built-in formatters.
/// </summary>
public enum LogFieldOrder
{
    /// <summary>
    /// Preserves the field order captured by the event.
    /// </summary>
    Captured = 0,
    /// <summary>
    /// Orders fields by name using ordinal comparison for stable output.
    /// </summary>
    Name = 1
}
