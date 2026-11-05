using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Creates compact or expanded JSON formatters for structured log events.
/// </summary>
internal class JsonLogFormatter
{
    /// <summary>
    /// Creates a newline-delimited compact JSON formatter.
    /// </summary>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter Compact()
    {
        LogFormatter formatter = LogFormatter.Custom(LogFormatter.CompactJsonName, JsonLogFormatter.FormatCompact);
        formatter.IsConfigurableBuiltIn = true;
        formatter.FormatAction = null;
        formatter.AppendNewLine = true;
        formatter.NewLine = "\n";
        return formatter;
    }

    /// <summary>
    /// Creates an indented JSON formatter with grouped event, activity, runtime, source, field, and exception data.
    /// </summary>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter Full()
    {
        LogFormatter formatter = LogFormatter.Custom(LogFormatter.JsonName, JsonLogFormatter.FormatFull);
        formatter.IsConfigurableBuiltIn = true;
        formatter.FormatAction = null;
        return formatter;
    }

    internal static string FormatCompact(LogEvent logEvent)
    {
        return JsonLogFormatter.FormatCompactConfigured(logEvent, LogTimestampMode.Utc, LogFieldOrder.Captured);
    }

    internal static string FormatCompactConfigured(LogEvent logEvent, LogTimestampMode timestampMode, LogFieldOrder fieldOrder)
    {
        if (logEvent == null)
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append('{');
        JsonLogFormatter.AppendStringProperty(builder, "ts", LogFormatter.FormatTimestamp(logEvent.Timestamp, timestampMode), false);
        JsonLogFormatter.AppendStringProperty(builder, "level", logEvent.Level.ToString(), true);
        JsonLogFormatter.AppendStringProperty(builder, "logger", logEvent.LoggerName, true);
        JsonLogFormatter.AppendStringProperty(builder, "message", logEvent.RenderedMessage, true);
        JsonLogFormatter.AppendStringProperty(builder, "template", logEvent.MessageTemplate, true);
        JsonLogFormatter.AppendNumberProperty(builder, "eventId", logEvent.EventId, true);
        JsonLogFormatter.AppendStringProperty(builder, "eventName", logEvent.EventName, true);
        JsonLogFormatter.AppendStringProperty(builder, "traceId", logEvent.TraceId, true);
        JsonLogFormatter.AppendStringProperty(builder, "spanId", logEvent.SpanId, true);
        JsonLogFormatter.AppendStringProperty(builder, "parentSpanId", logEvent.ParentSpanId, true);
        JsonLogFormatter.AppendNumberProperty(builder, "threadId", logEvent.ThreadId, true);
        JsonLogFormatter.AppendNumberProperty(builder, "processId", logEvent.ProcessId, true);
        JsonLogFormatter.AppendStringProperty(builder, "machine", logEvent.MachineName, true);
        JsonLogFormatter.AppendStringProperty(builder, "application", logEvent.ApplicationName, true);
        builder.Append(",\"fields\":");
        JsonLogFormatter.AppendOrderedFields(builder, logEvent.Fields, fieldOrder);
        builder.Append(",\"exception\":");
        JsonLogFormatter.AppendException(builder, logEvent.ExceptionValue);
        builder.Append('}');
        return builder.ToString();
    }

    internal static string FormatFull(LogEvent logEvent)
    {
        return JsonLogFormatter.FormatFullConfigured(logEvent, LogTimestampMode.Utc, LogFieldOrder.Captured);
    }

    internal static string FormatFullConfigured(LogEvent logEvent, LogTimestampMode timestampMode, LogFieldOrder fieldOrder)
    {
        if (logEvent == null)
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append('{');
        JsonLogFormatter.AppendFullStringProperty(builder, "timestamp", LogFormatter.FormatTimestamp(logEvent.Timestamp, timestampMode), 1, false);
        JsonLogFormatter.AppendFullStringProperty(builder, "level", logEvent.Level.ToString(), 1, true);
        JsonLogFormatter.AppendFullStringProperty(builder, "loggerName", logEvent.LoggerName, 1, true);
        JsonLogFormatter.AppendFullEvent(builder, logEvent, true);
        JsonLogFormatter.AppendFullMessage(builder, logEvent, true);
        JsonLogFormatter.AppendFullActivity(builder, logEvent, true);
        JsonLogFormatter.AppendFullRuntime(builder, logEvent, true);
        JsonLogFormatter.AppendFullSource(builder, logEvent, true);
        JsonLogFormatter.AppendFullOrderedFields(builder, logEvent.Fields, fieldOrder, true);
        JsonLogFormatter.AppendFullNumberProperty(builder, "invalidFieldCount", logEvent.InvalidFieldCount, 1, true);
        JsonLogFormatter.AppendFullExceptionProperty(builder, logEvent.ExceptionValue, true);
        builder.Append('\n');
        builder.Append('}');
        return builder.ToString();
    }

    internal static void AppendFields(System.Text.StringBuilder builder, LogField[] fields)
    {
        JsonLogFormatter.AppendOrderedFields(builder, fields, LogFieldOrder.Captured);
    }

    internal static void AppendOrderedFields(System.Text.StringBuilder builder, LogField[] fields, LogFieldOrder fieldOrder)
    {
        builder.Append('{');
        bool needsComma = false;
        LogField[] orderedFields = LogFormatter.OrderFields(fields, fieldOrder);
        if (orderedFields != null)
        {
            foreach (LogField field in orderedFields)
            {
                if (field == null || !field.IsValid)
                {
                    continue;
                }

                if (needsComma)
                {
                    builder.Append(',');
                }

                JsonLogFormatter.AppendJsonString(builder, field.Name);
                builder.Append(':');
                JsonLogFormatter.AppendValue(builder, field.Value);
                needsComma = true;
            }
        }

        builder.Append('}');
    }

    internal static void AppendValue(System.Text.StringBuilder builder, LogValue value)
    {
        if (value == null || value.Kind == LogValueKind.Null)
        {
            builder.Append("null");
            return;
        }

        if (value.Kind == LogValueKind.String)
        {
            JsonLogFormatter.AppendJsonString(builder, value.TextValue);
            return;
        }

        if (value.Kind == LogValueKind.Bool)
        {
            JsonLogFormatter.AppendBool(builder, value.BoolValue);
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
            JsonLogFormatter.AppendFloatingPoint(builder, value.FloatingPointValue);
            return;
        }

        if (value.Kind == LogValueKind.DateTime)
        {
            JsonLogFormatter.AppendJsonString(builder, value.DateTimeValue.UtcDateTime.ToString("O"));
            return;
        }

        if (value.Kind == LogValueKind.Duration)
        {
            JsonLogFormatter.AppendJsonString(builder, value.DurationValue.ToString("c", System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        if (value.Kind == LogValueKind.Exception)
        {
            JsonLogFormatter.AppendJsonString(builder, value.TextValue);
            return;
        }

        if (value.Kind == LogValueKind.ObjectText)
        {
            JsonLogFormatter.AppendJsonString(builder, value.TextValue);
            return;
        }

        if (value.Kind == LogValueKind.Array)
        {
            JsonLogFormatter.AppendArray(builder, value.ArrayValue);
            return;
        }

        if (value.Kind == LogValueKind.Dictionary)
        {
            JsonLogFormatter.AppendDictionary(builder, value.DictionaryValue);
            return;
        }

        if (value.Kind == LogValueKind.Raw)
        {
            JsonLogFormatter.AppendRaw(builder, value.RawValue);
            return;
        }

        builder.Append("null");
    }

    internal static void AppendArray(System.Text.StringBuilder builder, LogValue[] values)
    {
        builder.Append('[');
        bool needsComma = false;
        if (values != null)
        {
            foreach (LogValue value in values)
            {
                if (needsComma)
                {
                    builder.Append(',');
                }

                JsonLogFormatter.AppendValue(builder, value);
                needsComma = true;
            }
        }

        builder.Append(']');
    }

    internal static void AppendDictionary(System.Text.StringBuilder builder, Dictionary<string, LogValue> values)
    {
        builder.Append('{');
        bool needsComma = false;
        if (values != null)
        {
            foreach (KeyValuePair<string, LogValue> entry in values)
            {
                if (entry.Key == null)
                {
                    continue;
                }

                if (needsComma)
                {
                    builder.Append(',');
                }

                JsonLogFormatter.AppendJsonString(builder, entry.Key);
                builder.Append(':');
                JsonLogFormatter.AppendValue(builder, entry.Value);
                needsComma = true;
            }
        }

        builder.Append('}');
    }

    internal static void AppendException(System.Text.StringBuilder builder, Exception? exceptionValue)
    {
        if (exceptionValue == null)
        {
            builder.Append("null");
            return;
        }

        string exceptionType = "";
        string? fullName = exceptionValue.GetType().FullName;
        if (fullName != null)
        {
            exceptionType = fullName;
        }

        builder.Append('{');
        JsonLogFormatter.AppendStringProperty(builder, "type", exceptionType, false);
        JsonLogFormatter.AppendStringProperty(builder, "message", exceptionValue.Message, true);
        JsonLogFormatter.AppendStringProperty(builder, "text", exceptionValue.ToString(), true);
        builder.Append('}');
    }

    internal static void AppendFullEvent(System.Text.StringBuilder builder, LogEvent logEvent, bool needsComma)
    {
        JsonLogFormatter.AppendFullObjectStart(builder, "event", 1, needsComma);
        JsonLogFormatter.AppendFullNumberProperty(builder, "id", logEvent.EventId, 2, false);
        JsonLogFormatter.AppendFullStringProperty(builder, "name", logEvent.EventName, 2, true);
        JsonLogFormatter.AppendFullObjectEnd(builder, 1);
    }

    internal static void AppendFullMessage(System.Text.StringBuilder builder, LogEvent logEvent, bool needsComma)
    {
        JsonLogFormatter.AppendFullObjectStart(builder, "message", 1, needsComma);
        JsonLogFormatter.AppendFullStringProperty(builder, "template", logEvent.MessageTemplate, 2, false);
        JsonLogFormatter.AppendFullStringProperty(builder, "rendered", logEvent.RenderedMessage, 2, true);
        JsonLogFormatter.AppendFullObjectEnd(builder, 1);
    }

    internal static void AppendFullActivity(System.Text.StringBuilder builder, LogEvent logEvent, bool needsComma)
    {
        JsonLogFormatter.AppendFullObjectStart(builder, "activity", 1, needsComma);
        JsonLogFormatter.AppendFullStringProperty(builder, "traceId", logEvent.TraceId, 2, false);
        JsonLogFormatter.AppendFullStringProperty(builder, "spanId", logEvent.SpanId, 2, true);
        JsonLogFormatter.AppendFullStringProperty(builder, "parentSpanId", logEvent.ParentSpanId, 2, true);
        JsonLogFormatter.AppendFullObjectEnd(builder, 1);
    }

    internal static void AppendFullRuntime(System.Text.StringBuilder builder, LogEvent logEvent, bool needsComma)
    {
        JsonLogFormatter.AppendFullObjectStart(builder, "runtime", 1, needsComma);
        JsonLogFormatter.AppendFullNumberProperty(builder, "threadId", logEvent.ThreadId, 2, false);
        JsonLogFormatter.AppendFullNumberProperty(builder, "processId", logEvent.ProcessId, 2, true);
        JsonLogFormatter.AppendFullStringProperty(builder, "machineName", logEvent.MachineName, 2, true);
        JsonLogFormatter.AppendFullStringProperty(builder, "applicationName", logEvent.ApplicationName, 2, true);
        JsonLogFormatter.AppendFullObjectEnd(builder, 1);
    }

    internal static void AppendFullSource(System.Text.StringBuilder builder, LogEvent logEvent, bool needsComma)
    {
        JsonLogFormatter.AppendFullObjectStart(builder, "source", 1, needsComma);
        JsonLogFormatter.AppendFullStringProperty(builder, "file", logEvent.SourceFile, 2, false);
        JsonLogFormatter.AppendFullStringProperty(builder, "memberName", logEvent.SourceMemberName, 2, true);
        JsonLogFormatter.AppendFullNumberProperty(builder, "lineNumber", logEvent.SourceLineNumber, 2, true);
        JsonLogFormatter.AppendFullObjectEnd(builder, 1);
    }

    internal static void AppendFullFields(System.Text.StringBuilder builder, LogField[] fields, bool needsComma)
    {
        JsonLogFormatter.AppendFullOrderedFields(builder, fields, LogFieldOrder.Captured, needsComma);
    }

    internal static void AppendFullOrderedFields(System.Text.StringBuilder builder, LogField[] fields, LogFieldOrder fieldOrder, bool needsComma)
    {
        if (needsComma)
        {
            builder.Append(',');
        }

        builder.Append('\n');
        JsonLogFormatter.AppendIndent(builder, 1);
        JsonLogFormatter.AppendJsonString(builder, "fields");
        builder.Append(": [");

        bool needsFieldComma = false;
        LogField[] orderedFields = LogFormatter.OrderFields(fields, fieldOrder);
        if (orderedFields != null)
        {
            foreach (LogField field in orderedFields)
            {
                if (field == null || !field.IsValid)
                {
                    continue;
                }

                if (needsFieldComma)
                {
                    builder.Append(',');
                }

                builder.Append('\n');
                JsonLogFormatter.AppendIndent(builder, 2);
                builder.Append('{');
                JsonLogFormatter.AppendFullStringProperty(builder, "name", field.Name, 3, false);
                JsonLogFormatter.AppendFullValueDescription(builder, field.Value, 3, true);
                builder.Append('\n');
                JsonLogFormatter.AppendIndent(builder, 2);
                builder.Append('}');
                needsFieldComma = true;
            }
        }

        if (needsFieldComma)
        {
            builder.Append('\n');
            JsonLogFormatter.AppendIndent(builder, 1);
        }

        builder.Append(']');
    }

    internal static void AppendFullValueDescription(System.Text.StringBuilder builder, LogValue value, int indentLevel, bool needsComma)
    {
        LogValue normalizedValue = value;
        if (normalizedValue == null)
        {
            normalizedValue = LogValue.Null();
        }

        JsonLogFormatter.AppendFullStringProperty(builder, "kind", normalizedValue.Kind.ToString(), indentLevel, needsComma);
        JsonLogFormatter.AppendFullValueProperty(builder, "value", normalizedValue, indentLevel, true);
        JsonLogFormatter.AppendFullBoolProperty(builder, "isTruncated", normalizedValue.IsTruncated, indentLevel, true);
        JsonLogFormatter.AppendFullNumberProperty(builder, "originalLength", normalizedValue.OriginalLength, indentLevel, true);
        JsonLogFormatter.AppendFullNumberProperty(builder, "capturedLength", normalizedValue.CapturedLength, indentLevel, true);
        JsonLogFormatter.AppendFullNumberProperty(builder, "maximumLength", normalizedValue.MaximumLength, indentLevel, true);
        JsonLogFormatter.AppendFullNumberProperty(builder, "capturedDepth", normalizedValue.CapturedDepth, indentLevel, true);
    }

    internal static void AppendFullExceptionProperty(System.Text.StringBuilder builder, Exception? exceptionValue, bool needsComma)
    {
        if (needsComma)
        {
            builder.Append(',');
        }

        builder.Append('\n');
        JsonLogFormatter.AppendIndent(builder, 1);
        JsonLogFormatter.AppendJsonString(builder, "exception");
        builder.Append(": ");
        JsonLogFormatter.AppendException(builder, exceptionValue);
    }

    internal static void AppendFullObjectStart(System.Text.StringBuilder builder, string name, int indentLevel, bool needsComma)
    {
        if (needsComma)
        {
            builder.Append(',');
        }

        builder.Append('\n');
        JsonLogFormatter.AppendIndent(builder, indentLevel);
        JsonLogFormatter.AppendJsonString(builder, name);
        builder.Append(": {");
    }

    internal static void AppendFullObjectEnd(System.Text.StringBuilder builder, int indentLevel)
    {
        builder.Append('\n');
        JsonLogFormatter.AppendIndent(builder, indentLevel);
        builder.Append('}');
    }

    internal static void AppendFullStringProperty(System.Text.StringBuilder builder, string name, string value, int indentLevel, bool needsComma)
    {
        if (needsComma)
        {
            builder.Append(',');
        }

        builder.Append('\n');
        JsonLogFormatter.AppendIndent(builder, indentLevel);
        JsonLogFormatter.AppendJsonString(builder, name);
        builder.Append(": ");
        JsonLogFormatter.AppendJsonString(builder, value);
    }

    internal static void AppendFullNumberProperty(System.Text.StringBuilder builder, string name, int value, int indentLevel, bool needsComma)
    {
        if (needsComma)
        {
            builder.Append(',');
        }

        builder.Append('\n');
        JsonLogFormatter.AppendIndent(builder, indentLevel);
        JsonLogFormatter.AppendJsonString(builder, name);
        builder.Append(": ");
        builder.Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    internal static void AppendFullBoolProperty(System.Text.StringBuilder builder, string name, bool value, int indentLevel, bool needsComma)
    {
        if (needsComma)
        {
            builder.Append(',');
        }

        builder.Append('\n');
        JsonLogFormatter.AppendIndent(builder, indentLevel);
        JsonLogFormatter.AppendJsonString(builder, name);
        builder.Append(": ");
        JsonLogFormatter.AppendBool(builder, value);
    }

    internal static void AppendFullValueProperty(System.Text.StringBuilder builder, string name, LogValue value, int indentLevel, bool needsComma)
    {
        if (needsComma)
        {
            builder.Append(',');
        }

        builder.Append('\n');
        JsonLogFormatter.AppendIndent(builder, indentLevel);
        JsonLogFormatter.AppendJsonString(builder, name);
        builder.Append(": ");
        JsonLogFormatter.AppendValue(builder, value);
    }

    internal static void AppendIndent(System.Text.StringBuilder builder, int indentLevel)
    {
        int count = 0;
        int spaces = indentLevel * 2;
        while (count < spaces)
        {
            builder.Append(' ');
            count = count + 1;
        }
    }

    internal static void AppendStringProperty(System.Text.StringBuilder builder, string name, string value, bool needsComma)
    {
        if (needsComma)
        {
            builder.Append(',');
        }

        JsonLogFormatter.AppendJsonString(builder, name);
        builder.Append(':');
        JsonLogFormatter.AppendJsonString(builder, value);
    }

    internal static void AppendNumberProperty(System.Text.StringBuilder builder, string name, int value, bool needsComma)
    {
        if (needsComma)
        {
            builder.Append(',');
        }

        JsonLogFormatter.AppendJsonString(builder, name);
        builder.Append(':');
        builder.Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    internal static void AppendBool(System.Text.StringBuilder builder, bool value)
    {
        if (value)
        {
            builder.Append("true");
            return;
        }

        builder.Append("false");
    }

    internal static void AppendFloatingPoint(System.Text.StringBuilder builder, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            builder.Append("null");
            return;
        }

        builder.Append(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
    }

    internal static void AppendRaw(System.Text.StringBuilder builder, string value)
    {
        string normalizedValue = LogResult.NormalizeText(value);
        if (normalizedValue.Length == 0)
        {
            builder.Append("null");
            return;
        }

        builder.Append(normalizedValue);
    }

    internal static void AppendJsonString(System.Text.StringBuilder builder, string value)
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
            else if (character == '\b')
            {
                builder.Append("\\b");
            }
            else if (character == '\f')
            {
                builder.Append("\\f");
            }
            else if (character == '\n')
            {
                builder.Append("\\n");
            }
            else if (character == '\r')
            {
                builder.Append("\\r");
            }
            else if (character == '\t')
            {
                builder.Append("\\t");
            }
            else if (character < ' ')
            {
                builder.Append("\\u");
                builder.Append(((int)character).ToString("x4"));
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
