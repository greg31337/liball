using System;

namespace log;

/// <summary>
/// Defines the small logging contract applications can depend on without coupling to logger configuration or global state.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Reports whether the logger would accept the specified level.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>true when the level is enabled; otherwise, false.</returns>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// Writes a specified-level event with positional template values.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="messageTemplate">The message template to bind and render.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    LogResult WriteWithValues(LogLevel level, string? messageTemplate, object?[]? values);

    /// <summary>
    /// Writes a specified-level event with an attached exception and positional template values.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template to bind and render.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    LogResult WriteWithExceptionAndValues(LogLevel level, Exception? exceptionValue, string? messageTemplate, object?[]? values);

    /// <summary>
    /// Writes an already constructed event.
    /// </summary>
    /// <param name="logEvent">The event to evaluate and write.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    LogResult WriteEvent(LogEvent? logEvent);
}
