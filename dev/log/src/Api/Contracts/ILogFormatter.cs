namespace log;

/// <summary>
/// Defines conversion of a structured event into destination text.
/// </summary>
public interface ILogFormatter
{
    /// <summary>
    /// Formats one event without requiring a line ending.
    /// </summary>
    /// <param name="logEvent">The event to format.</param>
    /// <returns>The formatted text.</returns>
    string Format(LogEvent? logEvent);
}
