namespace log;

/// <summary>
/// Contains the flattened payload passed to a custom EventSource write action.
/// </summary>
public class LogEventSourceEntry
{
    /// <summary>
    /// The UTC timestamp captured for the event.
    /// </summary>
    public string Timestamp;
    /// <summary>
    /// The event severity.
    /// </summary>
    public string Level;
    /// <summary>
    /// The normalized dotted logger name.
    /// </summary>
    public string LoggerName;
    /// <summary>
    /// The application-defined numeric event identifier.
    /// </summary>
    public int EventId;
    /// <summary>
    /// The application-defined event name.
    /// </summary>
    public string EventName;
    /// <summary>
    /// The human-readable message.
    /// </summary>
    public string Message;
    /// <summary>
    /// The formatter-produced EventSource payload.
    /// </summary>
    public string Payload;
    /// <summary>
    /// The formatted exception text.
    /// </summary>
    public string Exception;

    internal LogEventSourceEntry(string timestamp, string level, string loggerName, int eventId, string eventName, string message, string payload, string exceptionText)
    {
        Timestamp = LogResult.NormalizeText(timestamp);
        Level = LogResult.NormalizeText(level);
        LoggerName = LogResult.NormalizeText(loggerName);
        EventId = eventId;
        EventName = LogResult.NormalizeText(eventName);
        Message = LogResult.NormalizeText(message);
        Payload = LogResult.NormalizeText(payload);
        Exception = LogResult.NormalizeText(exceptionText);
    }
}
