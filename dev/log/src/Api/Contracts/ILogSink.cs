namespace log;

/// <summary>
/// Defines a logging destination that can participate in level checks and lifecycle operations.
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Returns the stable name used to identify the sink in diagnostics and health data.
    /// </summary>
    /// <returns>The sink name, or an empty string for an unnamed sink.</returns>
    string GetName();

    /// <summary>
    /// Reports whether the sink accepts the specified level before event creation.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>true when the level is enabled; otherwise, false.</returns>
    bool IsEnabledForLevel(LogLevel level);

    /// <summary>
    /// Writes one event to the destination.
    /// </summary>
    /// <param name="logEvent">The event to evaluate and write.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    LogResult Write(LogEvent? logEvent);

    /// <summary>
    /// Flushes buffered destination resources.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    LogResult Flush();

    /// <summary>
    /// Forwards pending events without closing the destination.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    LogResult Drain();

    /// <summary>
    /// Drains and closes the destination.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    LogResult Close();
}
