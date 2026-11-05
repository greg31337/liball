namespace log;

/// <summary>
/// Defines dynamic structured fields to add while a logger constructs an event.
/// </summary>
public interface ILogEnricher
{
    /// <summary>
    /// Creates fields for the event currently being constructed.
    /// </summary>
    /// <param name="level">The event level.</param>
    /// <param name="loggerName">The normalized logger name.</param>
    /// <returns>The fields to add, or an empty array when no fields apply.</returns>
    LogField[] Enrich(LogLevel level, string loggerName);
}
