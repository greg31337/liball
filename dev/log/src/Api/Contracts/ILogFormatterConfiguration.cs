using System;

namespace log;

/// <summary>
/// Defines callback and child-formatter composition operations.
/// </summary>
public interface ILogFormatterConfiguration
{
    /// <summary>Sets the callback used to format events.</summary>
    void SetFormatAction(Func<LogEvent, string> formatAction);
    /// <summary>Clears the format callback.</summary>
    void ClearFormatAction();
    /// <summary>Sets UTC or local event timestamp rendering.</summary>
    void SetTimestampMode(LogTimestampMode mode);
    /// <summary>Returns the configured event timestamp rendering mode.</summary>
    LogTimestampMode GetTimestampMode();
    /// <summary>Sets captured or stable name-based field ordering.</summary>
    void SetFieldOrder(LogFieldOrder order);
    /// <summary>Returns the configured field ordering.</summary>
    LogFieldOrder GetFieldOrder();
    /// <summary>Formats an event and applies line-ending configuration.</summary>
    string FormatLine(LogEvent logEvent);
    /// <summary>Dispatches formatting by name.</summary>
    string Dispatch(string name, LogEvent logEvent);
    /// <summary>Dispatches line formatting by name.</summary>
    string DispatchLine(string name, LogEvent logEvent);
    /// <summary>Adds a child formatter.</summary>
    void AddFormatter(LogFormatter formatter);
    /// <summary>Replaces the child formatters.</summary>
    void SetFormatters(LogFormatter[] formatters);
    /// <summary>Removes all child formatters.</summary>
    void ClearFormatters();
    /// <summary>Finds a formatter by name.</summary>
    LogFormatter FindFormatter(string name);
    /// <summary>Reports whether a formatter name can be resolved.</summary>
    bool CanFormat(string name);
}
