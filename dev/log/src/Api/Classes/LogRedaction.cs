namespace log;

/// <summary>
/// Redacts selected structured fields without mutating the supplied values.
/// </summary>
public class LogRedaction : ILogRedaction
{
    /// <summary>Creates a redaction service.</summary>
    public LogRedaction()
    {
    }

    /// <summary>Creates the canonical redacted value.</summary>
    public LogValue CreateRedactedValue()
    {
        return LogRedactor.RedactedValue();
    }

    /// <summary>Redacts matching fields.</summary>
    public LogField[] RedactFields(LogField[] fields, string[] fieldNames)
    {
        return LogRedactor.RedactFields(fields, fieldNames);
    }

    /// <summary>Redacts matching event fields.</summary>
    public LogEvent RedactEvent(LogEvent logEvent, string[] fieldNames)
    {
        return LogRedactor.RedactEvent(logEvent, fieldNames);
    }
}
