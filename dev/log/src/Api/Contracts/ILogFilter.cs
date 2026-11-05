namespace log;

/// <summary>
/// Defines an event selection rule for a logger or sink.
/// </summary>
public interface ILogFilter
{
    /// <summary>
    /// Evaluates one event and returns Include, Exclude, or Neutral.
    /// </summary>
    /// <param name="logEvent">The event to evaluate.</param>
    /// <returns>The filter decision.</returns>
    LogDecision Decide(LogEvent? logEvent);
}
