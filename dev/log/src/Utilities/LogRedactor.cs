using System;

namespace log;

/// <summary>
/// Replaces selected structured field values with a fixed redaction marker.
/// </summary>
internal class LogRedactor
{
    internal const string RedactedText = "[redacted]";

    /// <summary>
    /// Creates the fixed structured value used in place of sensitive data.
    /// </summary>
    /// <returns>The resulting log value.</returns>
    internal static LogValue RedactedValue()
    {
        return LogValue.String(LogRedactor.RedactedText);
    }

    /// <summary>
    /// Returns a copy with configured field names replaced by the redaction marker.
    /// </summary>
    /// <param name="fields">The structured fields to copy or process.</param>
    /// <param name="fieldNames">The field names value.</param>
    /// <returns>A defensive copy of the requested items.</returns>
    internal static LogField[] RedactFields(LogField[] fields, string[] fieldNames)
    {
        if (fields == null)
        {
            return new LogField[0];
        }

        LogField[] redacted = new LogField[fields.Length];
        int index = 0;

        foreach (LogField field in fields)
        {
            if (field == null)
            {
                redacted[index] = new LogField("", null);
                index = index + 1;
                continue;
            }

            if (!field.IsValid)
            {
                redacted[index] = new LogField(field.Name, field.Value);
            }
            else if (LogRedactor.ShouldRedactFieldName(field.Name, fieldNames))
            {
                redacted[index] = new LogField(field.Name, LogRedactor.RedactedValue());
            }
            else
            {
                redacted[index] = new LogField(field.Name, field.Value);
            }

            index = index + 1;
        }

        return redacted;
    }

    /// <summary>
    /// Returns a copy with configured field names replaced by the redaction marker.
    /// </summary>
    /// <param name="logEvent">The event to evaluate, format, or write.</param>
    /// <param name="fieldNames">The field names value.</param>
    /// <returns>The resulting log event.</returns>
    internal static LogEvent RedactEvent(LogEvent logEvent, string[] fieldNames)
    {
        if (logEvent == null)
        {
            return new LogEvent(DateTimeOffset.MinValue, LogLevel.Off, "", 0, "", "", "", new LogField[0], null, "", "", "", 0, 0, "", "", "", "", 0);
        }

        LogField[] fields = LogRedactor.RedactFields(logEvent.Fields, fieldNames);
        return new LogEvent(logEvent.Timestamp, logEvent.Level, logEvent.LoggerName, logEvent.EventId, logEvent.EventName, logEvent.MessageTemplate, logEvent.RenderedMessage, fields, logEvent.ExceptionValue, logEvent.TraceId, logEvent.SpanId, logEvent.ParentSpanId, logEvent.ThreadId, logEvent.ProcessId, logEvent.MachineName, logEvent.ApplicationName, logEvent.SourceFile, logEvent.SourceMemberName, logEvent.SourceLineNumber);
    }

    internal static bool ShouldRedactFieldName(string fieldName, string[] fieldNames)
    {
        if (fieldNames == null)
        {
            return false;
        }

        foreach (string configuredName in fieldNames)
        {
            if (LogRedactor.MatchesConfiguredName(fieldName, configuredName))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool MatchesConfiguredName(string fieldName, string configuredName)
    {
        string normalizedFieldName = LogResult.NormalizeText(fieldName);
        string normalizedConfiguredName = LogResult.NormalizeText(configuredName);

        if (normalizedFieldName.Length == 0 || normalizedConfiguredName.Length == 0)
        {
            return false;
        }

        if (string.Equals(normalizedFieldName, normalizedConfiguredName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (LogRedactor.MatchesSuffixedName(normalizedFieldName, normalizedConfiguredName))
        {
            return true;
        }

        string reservedName = LogReservedFields.ReservedFieldName(normalizedConfiguredName);
        if (string.Equals(normalizedFieldName, reservedName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return LogRedactor.MatchesSuffixedName(normalizedFieldName, reservedName);
    }

    internal static bool MatchesSuffixedName(string fieldName, string configuredName)
    {
        string prefix = configuredName + "_";
        if (!fieldName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (fieldName.Length == prefix.Length)
        {
            return false;
        }

        int index = prefix.Length;
        while (index < fieldName.Length)
        {
            if (!char.IsDigit(fieldName[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
