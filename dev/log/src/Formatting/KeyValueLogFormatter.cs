using System;

namespace log;

/// <summary>
/// Creates a single-line formatter made of escaped key-value pairs.
/// </summary>
internal class KeyValueLogFormatter
{
    /// <summary>
    /// Creates a one-line key-value formatter that quotes whitespace and control characters.
    /// </summary>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter KeyValue()
    {
        LogFormatter formatter = LogFormatter.Custom(LogFormatter.KeyValueName, KeyValueLogFormatter.FormatKeyValue);
        formatter.IsConfigurableBuiltIn = true;
        formatter.FormatAction = null;
        formatter.AppendNewLine = true;
        formatter.NewLine = Environment.NewLine;
        return formatter;
    }

    internal static string FormatKeyValue(LogEvent logEvent)
    {
        return KeyValueLogFormatter.FormatConfigured(logEvent, LogTimestampMode.Utc, LogFieldOrder.Captured);
    }

    internal static string FormatConfigured(LogEvent logEvent, LogTimestampMode timestampMode, LogFieldOrder fieldOrder)
    {
        if (logEvent == null)
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool needsSpace = false;
        needsSpace = KeyValueLogFormatter.AppendStringPair(builder, "timestamp", LogFormatter.FormatTimestamp(logEvent.Timestamp, timestampMode), needsSpace);
        needsSpace = KeyValueLogFormatter.AppendStringPair(builder, "level", logEvent.Level.ToString(), needsSpace);
        if (logEvent.LoggerName.Length > 0)
        {
            needsSpace = KeyValueLogFormatter.AppendStringPair(builder, "logger", logEvent.LoggerName, needsSpace);
        }

        needsSpace = KeyValueLogFormatter.AppendNumberPair(builder, "eventId", logEvent.EventId, needsSpace);
        if (logEvent.EventName.Length > 0)
        {
            needsSpace = KeyValueLogFormatter.AppendStringPair(builder, "eventName", logEvent.EventName, needsSpace);
        }

        if (logEvent.MessageTemplate.Length > 0)
        {
            needsSpace = KeyValueLogFormatter.AppendStringPair(builder, "messageTemplate", logEvent.MessageTemplate, needsSpace);
        }

        needsSpace = KeyValueLogFormatter.AppendStringPair(builder, "message", LogFormatter.FormatDefault(logEvent), needsSpace);
        needsSpace = KeyValueLogFormatter.AppendOptionalStringPair(builder, "traceId", logEvent.TraceId, needsSpace);
        needsSpace = KeyValueLogFormatter.AppendOptionalStringPair(builder, "spanId", logEvent.SpanId, needsSpace);
        needsSpace = KeyValueLogFormatter.AppendOptionalStringPair(builder, "parentSpanId", logEvent.ParentSpanId, needsSpace);
        needsSpace = KeyValueLogFormatter.AppendNumberPair(builder, "threadId", logEvent.ThreadId, needsSpace);
        needsSpace = KeyValueLogFormatter.AppendNumberPair(builder, "processId", logEvent.ProcessId, needsSpace);
        needsSpace = KeyValueLogFormatter.AppendOptionalStringPair(builder, "machineName", logEvent.MachineName, needsSpace);
        needsSpace = KeyValueLogFormatter.AppendOptionalStringPair(builder, "applicationName", logEvent.ApplicationName, needsSpace);
        needsSpace = KeyValueLogFormatter.AppendOptionalStringPair(builder, "sourceFile", logEvent.SourceFile, needsSpace);
        needsSpace = KeyValueLogFormatter.AppendOptionalStringPair(builder, "sourceMemberName", logEvent.SourceMemberName, needsSpace);
        if (logEvent.SourceLineNumber > 0)
        {
            needsSpace = KeyValueLogFormatter.AppendNumberPair(builder, "sourceLineNumber", logEvent.SourceLineNumber, needsSpace);
        }

        needsSpace = KeyValueLogFormatter.AppendOrderedFields(builder, logEvent.Fields, fieldOrder, needsSpace);
        KeyValueLogFormatter.AppendException(builder, logEvent.ExceptionValue, needsSpace);
        return builder.ToString();
    }

    internal static bool AppendFields(System.Text.StringBuilder builder, LogField[] fields, bool needsSpace)
    {
        return KeyValueLogFormatter.AppendOrderedFields(builder, fields, LogFieldOrder.Captured, needsSpace);
    }

    internal static bool AppendOrderedFields(System.Text.StringBuilder builder, LogField[] fields, LogFieldOrder fieldOrder, bool needsSpace)
    {
        if (fields == null)
        {
            return needsSpace;
        }

        bool nextNeedsSpace = needsSpace;
        LogField[] orderedFields = LogFormatter.OrderFields(fields, fieldOrder);
        foreach (LogField field in orderedFields)
        {
            if (field == null || !field.IsValid)
            {
                continue;
            }

            nextNeedsSpace = KeyValueLogFormatter.AppendValuePair(builder, field.Name, field.Value, nextNeedsSpace);
        }

        return nextNeedsSpace;
    }

    internal static bool AppendException(System.Text.StringBuilder builder, Exception? exceptionValue, bool needsSpace)
    {
        if (exceptionValue == null)
        {
            return needsSpace;
        }

        bool nextNeedsSpace = KeyValueLogFormatter.AppendStringPair(builder, "exceptionType", exceptionValue.GetType().Name, needsSpace);
        nextNeedsSpace = KeyValueLogFormatter.AppendStringPair(builder, "exceptionMessage", exceptionValue.Message, nextNeedsSpace);
        return KeyValueLogFormatter.AppendStringPair(builder, "exception", exceptionValue.ToString(), nextNeedsSpace);
    }

    internal static bool AppendOptionalStringPair(System.Text.StringBuilder builder, string key, string value, bool needsSpace)
    {
        string normalizedValue = LogResult.NormalizeText(value);
        if (normalizedValue.Length == 0)
        {
            return needsSpace;
        }

        return KeyValueLogFormatter.AppendStringPair(builder, key, normalizedValue, needsSpace);
    }

    internal static bool AppendStringPair(System.Text.StringBuilder builder, string key, string value, bool needsSpace)
    {
        KeyValueLogFormatter.AppendPairStart(builder, key, needsSpace);
        KeyValueLogFormatter.AppendToken(builder, value);
        return true;
    }

    internal static bool AppendNumberPair(System.Text.StringBuilder builder, string key, int value, bool needsSpace)
    {
        KeyValueLogFormatter.AppendPairStart(builder, key, needsSpace);
        builder.Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return true;
    }

    internal static bool AppendValuePair(System.Text.StringBuilder builder, string key, LogValue value, bool needsSpace)
    {
        KeyValueLogFormatter.AppendPairStart(builder, key, needsSpace);
        KeyValueLogFormatter.AppendValue(builder, value);
        return true;
    }

    internal static void AppendPairStart(System.Text.StringBuilder builder, string key, bool needsSpace)
    {
        if (needsSpace)
        {
            builder.Append(' ');
        }

        builder.Append(LogResult.NormalizeText(key));
        builder.Append('=');
    }

    internal static void AppendValue(System.Text.StringBuilder builder, LogValue value)
    {
        if (value == null || value.Kind == LogValueKind.Null)
        {
            builder.Append("null");
            return;
        }

        if (value.Kind == LogValueKind.Bool)
        {
            TextLogFormatter.AppendBool(builder, value.BoolValue);
            return;
        }

        if (value.Kind == LogValueKind.Integer)
        {
            builder.Append(value.IntegerValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        if (value.Kind == LogValueKind.Decimal)
        {
            builder.Append(value.DecimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        if (value.Kind == LogValueKind.FloatingPoint)
        {
            TextLogFormatter.AppendFloatingPoint(builder, value.FloatingPointValue);
            return;
        }

        System.Text.StringBuilder text = new System.Text.StringBuilder();
        TextLogFormatter.AppendValue(text, value);
        KeyValueLogFormatter.AppendToken(builder, text.ToString());
    }

    internal static void AppendToken(System.Text.StringBuilder builder, string value)
    {
        string normalizedValue = LogResult.NormalizeText(value);
        if (!KeyValueLogFormatter.NeedsQuote(normalizedValue))
        {
            builder.Append(normalizedValue);
            return;
        }

        KeyValueLogFormatter.AppendQuotedToken(builder, normalizedValue);
    }

    internal static bool NeedsQuote(string value)
    {
        string normalizedValue = LogResult.NormalizeText(value);
        if (normalizedValue.Length == 0)
        {
            return true;
        }

        int index = 0;
        while (index < normalizedValue.Length)
        {
            char character = normalizedValue[index];
            if (character == ' ' || character == '\r' || character == '\n' || character == '\t' || character == '"' || character == '\\' || character == '=')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void AppendQuotedToken(System.Text.StringBuilder builder, string value)
    {
        string normalizedValue = LogResult.NormalizeText(value);
        builder.Append('"');
        int index = 0;
        while (index < normalizedValue.Length)
        {
            char character = normalizedValue[index];
            if (character == '"')
            {
                builder.Append("\\\"");
            }
            else if (character == '\\')
            {
                builder.Append("\\\\");
            }
            else if (character == '\r')
            {
                builder.Append("\\r");
            }
            else if (character == '\n')
            {
                builder.Append("\\n");
            }
            else if (character == '\t')
            {
                builder.Append("\\t");
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }

        builder.Append('"');
    }
}
