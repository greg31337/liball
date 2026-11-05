namespace log;

/// <summary>
/// Defines redaction of structured values, fields, and events.
/// </summary>
public interface ILogRedaction
{
    /// <summary>Creates the canonical redacted value.</summary>
    LogValue CreateRedactedValue();
    /// <summary>Redacts matching fields.</summary>
    LogField[] RedactFields(LogField[] fields, string[] fieldNames);
    /// <summary>Redacts matching event fields.</summary>
    LogEvent RedactEvent(LogEvent logEvent, string[] fieldNames);
}
