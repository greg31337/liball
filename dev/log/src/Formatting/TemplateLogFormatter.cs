using System;

namespace log;

/// <summary>
/// Creates text formatters driven by an output template.
/// </summary>
internal class TemplateLogFormatter
{
    internal const string DefaultTemplate = "{Timestamp} [{Level}] {Logger}: {Message}{Fields}{Exception}";

    /// <summary>
    /// Creates a formatter using the default output template.
    /// </summary>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter Template()
    {
        return TemplateLogFormatter.WithTemplate(DefaultTemplate);
    }

    /// <summary>
    /// Creates a template log formatter configured with the supplied template.
    /// </summary>
    /// <param name="outputTemplate">The output template containing built-in or structured-field placeholders.</param>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter WithTemplate(string outputTemplate)
    {
        string normalizedTemplate = TemplateLogFormatter.NormalizeTemplate(outputTemplate);
        LogFormatter formatter = LogFormatter.Custom(LogFormatter.TemplateName, logEvent => TemplateLogFormatter.FormatTemplate(logEvent, normalizedTemplate));
        formatter.IsConfigurableBuiltIn = true;
        formatter.FormatAction = null;
        formatter.OutputTemplate = normalizedTemplate;
        formatter.AppendNewLine = true;
        formatter.NewLine = Environment.NewLine;
        return formatter;
    }

    internal static string FormatDefaultTemplate(LogEvent logEvent)
    {
        return TemplateLogFormatter.FormatTemplate(logEvent, DefaultTemplate);
    }

    internal static string FormatTemplate(LogEvent logEvent, string outputTemplate)
    {
        return TemplateLogFormatter.FormatConfigured(logEvent, outputTemplate, LogTimestampMode.Utc, LogFieldOrder.Captured);
    }

    internal static string FormatConfigured(LogEvent logEvent, string outputTemplate, LogTimestampMode timestampMode, LogFieldOrder fieldOrder)
    {
        if (logEvent == null)
        {
            return "";
        }

        LogTemplate template = LogTemplateParser.ParseTemplate(TemplateLogFormatter.NormalizeTemplate(outputTemplate));
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        foreach (LogTemplateToken token in template.Tokens)
        {
            if (token == null)
            {
                continue;
            }

            if (token.IsLiteral)
            {
                builder.Append(token.Text);
                continue;
            }

            if (token.IsPlaceholder && token.IsValid)
            {
                TemplateLogFormatter.AppendConfiguredPlaceholder(builder, logEvent, token, timestampMode, fieldOrder);
            }
        }

        return builder.ToString();
    }

    internal static void AppendPlaceholder(System.Text.StringBuilder builder, LogEvent logEvent, LogTemplateToken token)
    {
        TemplateLogFormatter.AppendConfiguredPlaceholder(builder, logEvent, token, LogTimestampMode.Utc, LogFieldOrder.Captured);
    }

    internal static void AppendConfiguredPlaceholder(System.Text.StringBuilder builder, LogEvent logEvent, LogTemplateToken token, LogTimestampMode timestampMode, LogFieldOrder fieldOrder)
    {
        string name = LogResult.NormalizeText(token.Name);
        if (TemplateLogFormatter.NameEquals(name, "Timestamp"))
        {
            TemplateLogFormatter.AppendConfiguredTimestamp(builder, logEvent.Timestamp, token, timestampMode);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "UtcTimestamp"))
        {
            TemplateLogFormatter.AppendConfiguredTimestamp(builder, logEvent.Timestamp, token, LogTimestampMode.Utc);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "Level"))
        {
            builder.Append(logEvent.Level.ToString());
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "Logger") || TemplateLogFormatter.NameEquals(name, "LoggerName"))
        {
            builder.Append(logEvent.LoggerName);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "Message") || TemplateLogFormatter.NameEquals(name, "RenderedMessage"))
        {
            TextLogFormatter.AppendSafeText(builder, LogFormatter.FormatDefault(logEvent));
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "MessageTemplate"))
        {
            TextLogFormatter.AppendSafeText(builder, logEvent.MessageTemplate);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "EventId"))
        {
            TemplateLogFormatter.AppendInteger(builder, logEvent.EventId, token);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "EventName"))
        {
            builder.Append(logEvent.EventName);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "TraceId"))
        {
            builder.Append(logEvent.TraceId);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "SpanId"))
        {
            builder.Append(logEvent.SpanId);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "ParentSpanId"))
        {
            builder.Append(logEvent.ParentSpanId);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "ThreadId"))
        {
            TemplateLogFormatter.AppendInteger(builder, logEvent.ThreadId, token);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "ProcessId"))
        {
            TemplateLogFormatter.AppendInteger(builder, logEvent.ProcessId, token);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "MachineName"))
        {
            builder.Append(logEvent.MachineName);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "ApplicationName"))
        {
            builder.Append(logEvent.ApplicationName);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "SourceFile"))
        {
            builder.Append(logEvent.SourceFile);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "SourceMemberName"))
        {
            builder.Append(logEvent.SourceMemberName);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "SourceLineNumber"))
        {
            TemplateLogFormatter.AppendInteger(builder, logEvent.SourceLineNumber, token);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "Fields") || TemplateLogFormatter.NameEquals(name, "Properties"))
        {
            TextLogFormatter.AppendOrderedFields(builder, logEvent.Fields, fieldOrder);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "Exception"))
        {
            TextLogFormatter.AppendExceptionSummary(builder, logEvent.ExceptionValue);
            return;
        }

        if (TemplateLogFormatter.NameEquals(name, "NewLine"))
        {
            builder.Append(Environment.NewLine);
            return;
        }

        TemplateLogFormatter.AppendFieldValue(builder, logEvent.Fields, name);
    }

    internal static void AppendFieldValue(System.Text.StringBuilder builder, LogField[] fields, string name)
    {
        if (fields == null)
        {
            return;
        }

        foreach (LogField field in fields)
        {
            if (field == null || !field.IsValid)
            {
                continue;
            }

            if (TemplateLogFormatter.NameEquals(field.Name, name))
            {
                TextLogFormatter.AppendValue(builder, field.Value);
                return;
            }
        }
    }

    internal static void AppendTimestamp(System.Text.StringBuilder builder, DateTimeOffset timestamp, LogTemplateToken token)
    {
        TemplateLogFormatter.AppendConfiguredTimestamp(builder, timestamp, token, LogTimestampMode.Utc);
    }

    internal static void AppendConfiguredTimestamp(System.Text.StringBuilder builder, DateTimeOffset timestamp, LogTemplateToken token, LogTimestampMode timestampMode)
    {
        if (token != null && token.HasFormat)
        {
            DateTimeOffset configuredTimestamp = timestamp.ToUniversalTime();
            if (timestampMode == LogTimestampMode.Local)
            {
                configuredTimestamp = timestamp.ToLocalTime();
            }

            builder.Append(configuredTimestamp.ToString(token.Format, System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        builder.Append(LogFormatter.FormatTimestamp(timestamp, timestampMode));
    }

    internal static void AppendInteger(System.Text.StringBuilder builder, int value, LogTemplateToken token)
    {
        if (token != null && token.HasFormat)
        {
            builder.Append(value.ToString(token.Format, System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        builder.Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    internal static bool NameEquals(string left, string right)
    {
        return string.Equals(LogResult.NormalizeText(left), LogResult.NormalizeText(right), StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeTemplate(string outputTemplate)
    {
        string normalizedTemplate = LogResult.NormalizeText(outputTemplate);
        if (normalizedTemplate.Length == 0)
        {
            return DefaultTemplate;
        }

        return normalizedTemplate;
    }
}
