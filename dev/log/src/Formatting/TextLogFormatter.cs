using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Creates the default human-readable text formatter.
/// </summary>
internal class TextLogFormatter
{
    /// <summary>
    /// Creates the default text formatter with timestamp, level, logger, message, fields, and exception summary.
    /// </summary>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter Plain()
    {
        LogFormatter formatter = LogFormatter.Custom(LogFormatter.TextName, TextLogFormatter.FormatPlain);
        formatter.IsConfigurableBuiltIn = true;
        formatter.FormatAction = null;
        formatter.AppendNewLine = true;
        formatter.NewLine = Environment.NewLine;
        return formatter;
    }

    internal static string FormatPlain(LogEvent logEvent)
    {
        return TextLogFormatter.FormatConfigured(logEvent, LogTimestampMode.Utc, LogFieldOrder.Captured);
    }

    internal static string FormatConfigured(LogEvent logEvent, LogTimestampMode timestampMode, LogFieldOrder fieldOrder)
    {
        if (logEvent == null)
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(LogFormatter.FormatTimestamp(logEvent.Timestamp, timestampMode));
        builder.Append(" [");
        builder.Append(logEvent.Level.ToString());
        builder.Append(']');

        if (logEvent.LoggerName.Length > 0)
        {
            builder.Append(' ');
            builder.Append(logEvent.LoggerName);
        }

        string message = TextLogFormatter.SelectMessage(logEvent);
        if (message.Length > 0)
        {
            builder.Append(": ");
            TextLogFormatter.AppendSafeText(builder, message);
        }

        TextLogFormatter.AppendOrderedFields(builder, logEvent.Fields, fieldOrder);
        TextLogFormatter.AppendExceptionSummary(builder, logEvent.ExceptionValue);
        return builder.ToString();
    }

    internal static string SelectMessage(LogEvent logEvent)
    {
        if (logEvent == null)
        {
            return "";
        }

        if (logEvent.RenderedMessage.Length > 0)
        {
            return logEvent.RenderedMessage;
        }

        return logEvent.MessageTemplate;
    }

    internal static void AppendFields(System.Text.StringBuilder builder, LogField[] fields)
    {
        TextLogFormatter.AppendOrderedFields(builder, fields, LogFieldOrder.Captured);
    }

    internal static void AppendOrderedFields(System.Text.StringBuilder builder, LogField[] fields, LogFieldOrder fieldOrder)
    {
        if (!TextLogFormatter.HasFields(fields))
        {
            return;
        }

        builder.Append(" {");
        bool needsComma = false;
        LogField[] orderedFields = LogFormatter.OrderFields(fields, fieldOrder);
        foreach (LogField field in orderedFields)
        {
            if (field == null || !field.IsValid)
            {
                continue;
            }

            if (needsComma)
            {
                builder.Append(", ");
            }

            builder.Append(field.Name);
            builder.Append('=');
            TextLogFormatter.AppendValue(builder, field.Value);
            needsComma = true;
        }

        builder.Append('}');
    }

    internal static bool HasFields(LogField[] fields)
    {
        if (fields == null)
        {
            return false;
        }

        foreach (LogField field in fields)
        {
            if (field != null && field.IsValid)
            {
                return true;
            }
        }

        return false;
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
            TextLogFormatter.AppendSafeText(builder, value.TextValue);
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

        if (value.Kind == LogValueKind.DateTime)
        {
            builder.Append(value.DateTimeValue.UtcDateTime.ToString("O"));
            return;
        }

        if (value.Kind == LogValueKind.Duration)
        {
            builder.Append(value.DurationValue.ToString("c", System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        if (value.Kind == LogValueKind.Exception)
        {
            TextLogFormatter.AppendSafeText(builder, value.TextValue);
            return;
        }

        if (value.Kind == LogValueKind.ObjectText)
        {
            TextLogFormatter.AppendSafeText(builder, value.TextValue);
            return;
        }

        if (value.Kind == LogValueKind.Array)
        {
            TextLogFormatter.AppendArray(builder, value.ArrayValue);
            return;
        }

        if (value.Kind == LogValueKind.Dictionary)
        {
            TextLogFormatter.AppendDictionary(builder, value.DictionaryValue);
            return;
        }

        if (value.Kind == LogValueKind.Raw)
        {
            TextLogFormatter.AppendSafeText(builder, value.RawValue);
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
                    builder.Append(", ");
                }

                TextLogFormatter.AppendValue(builder, value);
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
                    builder.Append(", ");
                }

                builder.Append(entry.Key);
                builder.Append('=');
                TextLogFormatter.AppendValue(builder, entry.Value);
                needsComma = true;
            }
        }

        builder.Append('}');
    }

    internal static void AppendExceptionSummary(System.Text.StringBuilder builder, Exception? exceptionValue)
    {
        if (exceptionValue == null)
        {
            return;
        }

        builder.Append(" exception=");
        builder.Append(exceptionValue.GetType().Name);
        if (exceptionValue.Message.Length > 0)
        {
            builder.Append(": ");
            TextLogFormatter.AppendSafeText(builder, exceptionValue.Message);
        }
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

    internal static void AppendSafeText(System.Text.StringBuilder builder, string value)
    {
        string normalizedValue = LogResult.NormalizeText(value);
        int index = 0;
        while (index < normalizedValue.Length)
        {
            char character = normalizedValue[index];
            if (character == '\r' || character == '\n' || character == '\t')
            {
                builder.Append(' ');
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }
    }
}
