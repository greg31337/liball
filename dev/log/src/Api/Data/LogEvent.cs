using System;

namespace log;

/// <summary>
/// Contains the structured data captured for one logging operation.
/// </summary>
/// <remarks>
/// Fields are public snapshot data because properties are outside this library's syntax subset. Factory methods normalize names and retain only valid structured fields.
/// </remarks>
public class LogEvent
{
    /// <summary>
    /// The UTC timestamp captured for the event.
    /// </summary>
    public DateTimeOffset Timestamp;
    /// <summary>
    /// The event severity.
    /// </summary>
    public LogLevel Level;
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
    /// The original message template before placeholder rendering.
    /// </summary>
    public string MessageTemplate;
    /// <summary>
    /// The message after positional values were bound to placeholders.
    /// </summary>
    public string RenderedMessage;
    /// <summary>
    /// The event's validated structured fields.
    /// </summary>
    public LogField[] Fields;
    /// <summary>
    /// The number of supplied fields omitted because their names were invalid.
    /// </summary>
    public int InvalidFieldCount;
    /// <summary>
    /// The exception attached to the event, or null when none was supplied.
    /// </summary>
    public Exception? ExceptionValue;
    /// <summary>
    /// The current activity trace identifier, or an empty string when unavailable.
    /// </summary>
    public string TraceId;
    /// <summary>
    /// The current activity span identifier, or an empty string when unavailable.
    /// </summary>
    public string SpanId;
    /// <summary>
    /// The parent activity span identifier, or an empty string when unavailable.
    /// </summary>
    public string ParentSpanId;
    /// <summary>
    /// The captured managed thread identifier.
    /// </summary>
    public int ThreadId;
    /// <summary>
    /// The captured process identifier.
    /// </summary>
    public int ProcessId;
    /// <summary>
    /// The captured machine name.
    /// </summary>
    public string MachineName;
    /// <summary>
    /// The captured application name.
    /// </summary>
    public string ApplicationName;
    /// <summary>
    /// The optional source file hint.
    /// </summary>
    public string SourceFile;
    /// <summary>
    /// The optional source member hint.
    /// </summary>
    public string SourceMemberName;
    /// <summary>
    /// The optional one-based source line number.
    /// </summary>
    public int SourceLineNumber;
    /// <summary>
    /// Whether the object passed validation and can be used normally.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// The validation error text, or an empty string for a valid object.
    /// </summary>
    public string ErrorMessage;

    /// <summary>
    /// Creates a log event from its complete field representation.
    /// </summary>
    /// <param name="timestamp">The event timestamp.</param>
    /// <param name="level">The event level.</param>
    /// <param name="loggerName">The logger name.</param>
    /// <param name="eventId">The numeric event identifier.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="renderedMessage">The rendered message.</param>
    /// <param name="fields">The structured fields.</param>
    /// <param name="exceptionValue">The attached exception.</param>
    /// <param name="traceId">The activity trace identifier.</param>
    /// <param name="spanId">The activity span identifier.</param>
    /// <param name="parentSpanId">The parent activity span identifier.</param>
    /// <param name="threadId">The managed thread identifier.</param>
    /// <param name="processId">The process identifier.</param>
    /// <param name="machineName">The machine name.</param>
    /// <param name="applicationName">The application name.</param>
    /// <param name="sourceFile">The source file hint.</param>
    /// <param name="sourceMemberName">The source member hint.</param>
    /// <param name="sourceLineNumber">The source line hint.</param>
    public LogEvent(DateTimeOffset timestamp, LogLevel level, string loggerName, int eventId, string eventName, string messageTemplate, string renderedMessage, LogField[] fields, Exception? exceptionValue, string traceId, string spanId, string parentSpanId, int threadId, int processId, string machineName, string applicationName, string sourceFile, string sourceMemberName, int sourceLineNumber)
    {
        Timestamp = timestamp;
        Level = level;
        LoggerName = LogName.NormalizeLoggerName(loggerName);
        EventId = eventId;
        EventName = LogResult.NormalizeText(eventName);
        MessageTemplate = LogResult.NormalizeText(messageTemplate);
        RenderedMessage = LogResult.NormalizeText(renderedMessage);
        Fields = LogReservedFields.Resolve(fields);
        InvalidFieldCount = LogEvent.CountInvalidFields(fields);
        ExceptionValue = exceptionValue;
        TraceId = LogResult.NormalizeText(traceId);
        SpanId = LogResult.NormalizeText(spanId);
        ParentSpanId = LogResult.NormalizeText(parentSpanId);
        ThreadId = LogEvent.NormalizeNonNegative(threadId);
        ProcessId = LogEvent.NormalizeNonNegative(processId);
        MachineName = LogResult.NormalizeText(machineName);
        ApplicationName = LogResult.NormalizeText(applicationName);
        SourceFile = LogResult.NormalizeText(sourceFile);
        SourceMemberName = LogResult.NormalizeText(sourceMemberName);
        SourceLineNumber = LogEvent.NormalizeNonNegative(sourceLineNumber);
        IsValid = true;
        ErrorMessage = "";

        if (LoggerName.Length > 0)
        {
            string loggerNameError = LogName.ValidateLoggerName(LoggerName);
            if (loggerNameError.Length > 0)
            {
                IsValid = false;
                ErrorMessage = loggerNameError;
            }
        }
    }

    /// <summary>
    /// Creates a log event using default settings.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="loggerName">The dotted logger or category name.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="renderedMessage">The already rendered message text.</param>
    /// <param name="fields">The structured fields to copy or process.</param>
    /// <returns>The resulting log event.</returns>
    internal static LogEvent Create(LogLevel level, string loggerName, string messageTemplate, string renderedMessage, LogField[] fields)
    {
        return LogEvent.CreateWithClock(LogClock.SystemClock(), level, loggerName, messageTemplate, renderedMessage, fields);
    }

    internal static LogEvent CreateWithClock(ILogClock clock, LogLevel level, string loggerName, string messageTemplate, string renderedMessage, LogField[] fields)
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        if (clock != null)
        {
            timestamp = clock.UtcNow();
        }

        return LogEvent.CreateAt(timestamp, level, loggerName, 0, "", messageTemplate, renderedMessage, fields, null);
    }

    /// <summary>
    /// Creates an event at an explicit timestamp with optional identity and exception data.
    /// </summary>
    /// <param name="timestamp">The UTC event timestamp.</param>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="loggerName">The dotted logger or category name.</param>
    /// <param name="eventId">The application-defined event identifier.</param>
    /// <param name="eventName">The application-defined event name or EventSource event name.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="renderedMessage">The already rendered message text.</param>
    /// <param name="fields">The structured fields to copy or process.</param>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <returns>The resulting log event.</returns>
    internal static LogEvent CreateAt(DateTimeOffset timestamp, LogLevel level, string loggerName, int eventId, string eventName, string messageTemplate, string renderedMessage, LogField[] fields, Exception? exceptionValue)
    {
        return new LogEvent(timestamp, level, loggerName, eventId, eventName, messageTemplate, renderedMessage, fields, exceptionValue, "", "", "", Environment.CurrentManagedThreadId, Environment.ProcessId, Environment.MachineName, AppDomain.CurrentDomain.FriendlyName, "", "", 0);
    }

    internal static LogField[] CopyValidFields(LogField[] fields)
    {
        if (fields == null)
        {
            return new LogField[0];
        }

        int count = 0;
        foreach (LogField field in fields)
        {
            if (field != null && field.IsValid)
            {
                count = count + 1;
            }
        }

        LogField[] copy = new LogField[count];
        int index = 0;
        foreach (LogField field in fields)
        {
            if (field != null && field.IsValid)
            {
                copy[index] = new LogField(field.Name, field.Value);
                index = index + 1;
            }
        }

        return copy;
    }

    internal static int CountInvalidFields(LogField[] fields)
    {
        if (fields == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LogField field in fields)
        {
            if (field == null || !field.IsValid)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static int NormalizeNonNegative(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value;
    }
}
