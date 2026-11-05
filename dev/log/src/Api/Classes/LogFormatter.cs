using System;

namespace log;

/// <summary>
/// Formats events directly or dispatches them to a named child formatter.
/// </summary>
/// <remarks>
/// Format returns content without adding a line ending. FormatLine appends the configured NewLine only when AppendNewLine is enabled.
/// </remarks>
public class LogFormatter : ILogFormatter, ILogFormatterConfiguration
{
    internal const string TextName = "text";
    internal const string CompactJsonName = "compact-json";
    internal const string JsonName = "json";
    internal const string ConsoleTextName = "console-text";
    internal const string KeyValueName = "key-value";
    internal const string TemplateName = "template";
    internal const string ExceptionName = "exception";

    /// <summary>
    /// The normalized name used to identify this object.
    /// </summary>
    internal string Name = TextName;
    /// <summary>
    /// Whether FormatLine appends the configured line ending.
    /// </summary>
    internal bool AppendNewLine;
    /// <summary>
    /// The line-ending text appended by FormatLine.
    /// </summary>
    internal string NewLine = Environment.NewLine;
    internal Func<LogEvent, string>? FormatAction;
    internal ILogFormatter? InterfaceFormatter;
    internal LogFormatter[] Formatters = new LogFormatter[0];
    internal bool IsConfigurableBuiltIn;
    internal bool UseAnsiColor;
    internal string OutputTemplate = "";
    internal LogTimestampMode TimestampMode;
    internal LogFieldOrder FieldOrder;

    internal LogFormatter()
    {
        Name = TextName;
        AppendNewLine = false;
        NewLine = Environment.NewLine;
        FormatAction = null;
        InterfaceFormatter = null;
        Formatters = new LogFormatter[0];
        IsConfigurableBuiltIn = false;
        UseAnsiColor = false;
        OutputTemplate = "";
        TimestampMode = LogTimestampMode.Utc;
        FieldOrder = LogFieldOrder.Captured;
    }

    /// <summary>
    /// Creates a formatter with the supplied name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter Named(string name)
    {
        LogFormatter? builtInFormatter = LogFormatter.CreateBuiltInFormatter(name);
        if (builtInFormatter != null)
        {
            return builtInFormatter;
        }

        LogFormatter formatter = new LogFormatter();
        formatter.Name = LogFormatter.NormalizeName(name);
        return formatter;
    }

    /// <summary>
    /// Creates a formatter that invokes an application callback for each event.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="formatAction">The callback used to format one event.</param>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter Custom(string name, Func<LogEvent, string> formatAction)
    {
        LogFormatter formatter = new LogFormatter();
        formatter.Name = LogFormatter.NormalizeName(name);
        formatter.SetFormatAction(formatAction);
        return formatter;
    }

    /// <summary>
    /// Wraps an interface-based formatter for use by concrete sink APIs.
    /// </summary>
    /// <param name="formatter">The formatter implementation to wrap.</param>
    /// <returns>A concrete adapter that delegates formatting to the supplied implementation.</returns>
    internal static LogFormatter Wrap(ILogFormatter? formatter)
    {
        if (formatter == null)
        {
            return TextLogFormatter.Plain();
        }

        LogFormatter adapter = new LogFormatter();
        adapter.Name = "custom";
        adapter.InterfaceFormatter = formatter;
        return adapter;
    }

    /// <summary>
    /// Sets format action for this formatter.
    /// </summary>
    /// <param name="formatAction">The callback used to format one event.</param>
    public void SetFormatAction(Func<LogEvent, string> formatAction)
    {
        FormatAction = formatAction;
    }

    /// <summary>
    /// Clears format action and restores the default behavior.
    /// </summary>
    public void ClearFormatAction()
    {
        FormatAction = null;
    }

    /// <summary>
    /// Sets UTC or process-local event timestamp rendering for built-in formatters.
    /// </summary>
    /// <param name="mode">The timestamp mode to apply.</param>
    public void SetTimestampMode(LogTimestampMode mode)
    {
        if (mode == LogTimestampMode.Local)
        {
            TimestampMode = LogTimestampMode.Local;
            return;
        }

        TimestampMode = LogTimestampMode.Utc;
    }

    /// <summary>
    /// Returns the configured event timestamp rendering mode.
    /// </summary>
    /// <returns>The normalized timestamp mode.</returns>
    public LogTimestampMode GetTimestampMode()
    {
        return TimestampMode;
    }

    /// <summary>
    /// Sets captured or stable ordinal name-based field ordering for built-in formatters.
    /// </summary>
    /// <param name="order">The field order to apply.</param>
    public void SetFieldOrder(LogFieldOrder order)
    {
        if (order == LogFieldOrder.Name)
        {
            FieldOrder = LogFieldOrder.Name;
            return;
        }

        FieldOrder = LogFieldOrder.Captured;
    }

    /// <summary>
    /// Returns the configured structured field ordering.
    /// </summary>
    /// <returns>The normalized field order.</returns>
    public LogFieldOrder GetFieldOrder()
    {
        return FieldOrder;
    }

    /// <summary>
    /// Formats one event without automatically appending a line ending.
    /// </summary>
    /// <param name="logEvent">The event to evaluate, format, or write.</param>
    /// <returns>The normalized or formatted text.</returns>
    public string Format(LogEvent? logEvent)
    {
        if (logEvent == null)
        {
            return "";
        }

        if (FormatAction != null)
        {
            return LogResult.NormalizeText(FormatAction(logEvent));
        }

        if (InterfaceFormatter != null)
        {
            return LogResult.NormalizeText(InterfaceFormatter.Format(logEvent));
        }

        if (IsConfigurableBuiltIn)
        {
            return FormatConfiguredBuiltIn(logEvent);
        }

        return LogFormatter.FormatDefault(logEvent);
    }

    internal string FormatConfiguredBuiltIn(LogEvent logEvent)
    {
        if (Name == TextName)
        {
            return TextLogFormatter.FormatConfigured(logEvent, TimestampMode, FieldOrder);
        }

        if (Name == CompactJsonName)
        {
            return JsonLogFormatter.FormatCompactConfigured(logEvent, TimestampMode, FieldOrder);
        }

        if (Name == JsonName)
        {
            return JsonLogFormatter.FormatFullConfigured(logEvent, TimestampMode, FieldOrder);
        }

        if (Name == ConsoleTextName)
        {
            return ConsoleTextLogFormatter.FormatConfigured(logEvent, TimestampMode, FieldOrder, UseAnsiColor);
        }

        if (Name == KeyValueName)
        {
            return KeyValueLogFormatter.FormatConfigured(logEvent, TimestampMode, FieldOrder);
        }

        if (Name == TemplateName)
        {
            return TemplateLogFormatter.FormatConfigured(logEvent, OutputTemplate, TimestampMode, FieldOrder);
        }

        return LogFormatter.FormatDefault(logEvent);
    }

    /// <summary>
    /// Formats one event and appends the configured line ending when enabled.
    /// </summary>
    /// <param name="logEvent">The event to evaluate, format, or write.</param>
    /// <returns>The normalized or formatted text.</returns>
    public string FormatLine(LogEvent logEvent)
    {
        string output = Format(logEvent);
        if (!AppendNewLine)
        {
            return output;
        }

        return output + LogFormatter.NormalizeNewLine(NewLine);
    }

    /// <summary>
    /// Formats an event through the matching named child formatter.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="logEvent">The event to evaluate, format, or write.</param>
    /// <returns>The normalized or formatted text.</returns>
    public string Dispatch(string name, LogEvent logEvent)
    {
        LogFormatter formatter = FindFormatter(name);
        return formatter.Format(logEvent);
    }

    /// <summary>
    /// Formats an event through the matching named child formatter.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="logEvent">The event to evaluate, format, or write.</param>
    /// <returns>The normalized or formatted text.</returns>
    public string DispatchLine(string name, LogEvent logEvent)
    {
        LogFormatter formatter = FindFormatter(name);
        return formatter.FormatLine(logEvent);
    }

    /// <summary>
    /// Adds formatter to this formatter.
    /// </summary>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    public void AddFormatter(LogFormatter formatter)
    {
        Formatters = LogFormatter.AppendFormatter(Formatters, formatter);
    }

    /// <summary>
    /// Sets formatters for this formatter.
    /// </summary>
    /// <param name="formatters">The child formatters to copy into this formatter.</param>
    public void SetFormatters(LogFormatter[] formatters)
    {
        Formatters = LogFormatter.CopyFormatters(formatters);
    }

    /// <summary>
    /// Clears formatters and restores the default behavior.
    /// </summary>
    public void ClearFormatters()
    {
        Formatters = new LogFormatter[0];
    }

    /// <summary>
    /// Finds formatter without changing this formatter.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The configured formatter.</returns>
    public LogFormatter FindFormatter(string name)
    {
        string normalizedName = LogFormatter.NormalizeName(name);
        if (normalizedName == Name)
        {
            return this;
        }

        if (Formatters != null)
        {
            foreach (LogFormatter formatter in Formatters)
            {
                if (formatter != null && formatter.Name == normalizedName)
                {
                    return formatter;
                }
            }
        }

        LogFormatter? builtInFormatter = LogFormatter.CreateBuiltInFormatter(normalizedName);
        if (builtInFormatter != null)
        {
            return builtInFormatter;
        }

        return this;
    }

    /// <summary>
    /// Reports whether this formatter can format.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    public bool CanFormat(string name)
    {
        string normalizedName = LogFormatter.NormalizeName(name);
        if (normalizedName == Name)
        {
            return true;
        }

        if (Formatters != null)
        {
            foreach (LogFormatter formatter in Formatters)
            {
                if (formatter != null && formatter.Name == normalizedName)
                {
                    return true;
                }
            }
        }

        if (LogFormatter.CreateBuiltInFormatter(normalizedName) != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates the built-in formatter matching a normalized formatter name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter? CreateBuiltInFormatter(string name)
    {
        string normalizedName = LogFormatter.NormalizeName(name);
        if (normalizedName == TextName)
        {
            return TextLogFormatter.Plain();
        }

        if (normalizedName == CompactJsonName)
        {
            return JsonLogFormatter.Compact();
        }

        if (normalizedName == JsonName)
        {
            return JsonLogFormatter.Full();
        }

        if (normalizedName == ConsoleTextName)
        {
            return ConsoleTextLogFormatter.Plain();
        }

        if (normalizedName == KeyValueName)
        {
            return KeyValueLogFormatter.KeyValue();
        }

        if (normalizedName == TemplateName)
        {
            return TemplateLogFormatter.Template();
        }

        if (normalizedName == ExceptionName)
        {
            return ExceptionLogFormatter.Full();
        }

        return null;
    }

    internal static string FormatDefault(LogEvent? logEvent)
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

    internal static bool IsKnownName(string name)
    {
        string normalizedName = LogFormatter.NormalizeName(name);
        return normalizedName == TextName
            || normalizedName == CompactJsonName
            || normalizedName == JsonName
            || normalizedName == ConsoleTextName
            || normalizedName == KeyValueName
            || normalizedName == TemplateName
            || normalizedName == ExceptionName;
    }

    internal static string NormalizeName(string? name)
    {
        string normalizedName = LogResult.NormalizeText(name).Trim().ToLowerInvariant();
        if (normalizedName.Length == 0)
        {
            return TextName;
        }

        if (normalizedName == "ndjson")
        {
            return CompactJsonName;
        }

        if (normalizedName == "compactjson")
        {
            return CompactJsonName;
        }

        if (normalizedName == "console")
        {
            return ConsoleTextName;
        }

        if (normalizedName == "kv")
        {
            return KeyValueName;
        }

        return normalizedName;
    }

    internal static string NormalizeNewLine(string newLine)
    {
        string normalizedNewLine = LogResult.NormalizeText(newLine);
        if (normalizedNewLine.Length == 0)
        {
            return Environment.NewLine;
        }

        return normalizedNewLine;
    }

    internal static string FormatTimestamp(DateTimeOffset timestamp, LogTimestampMode mode)
    {
        if (mode == LogTimestampMode.Local)
        {
            return timestamp.ToLocalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }

        return timestamp.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static LogField[] OrderFields(LogField[] fields, LogFieldOrder order)
    {
        if (fields == null)
        {
            return new LogField[0];
        }

        LogField[] copy = new LogField[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            copy[index] = fields[index];
            index = index + 1;
        }

        if (order != LogFieldOrder.Name)
        {
            return copy;
        }

        int outerIndex = 1;
        while (outerIndex < copy.Length)
        {
            LogField current = copy[outerIndex];
            int innerIndex = outerIndex - 1;
            while (innerIndex >= 0 && LogFormatter.CompareFields(copy[innerIndex], current) > 0)
            {
                copy[innerIndex + 1] = copy[innerIndex];
                innerIndex = innerIndex - 1;
            }

            copy[innerIndex + 1] = current;
            outerIndex = outerIndex + 1;
        }

        return copy;
    }

    internal static int CompareFields(LogField left, LogField right)
    {
        string leftName = "";
        string rightName = "";
        if (left != null)
        {
            leftName = left.Name;
        }

        if (right != null)
        {
            rightName = right.Name;
        }

        return string.CompareOrdinal(leftName, rightName);
    }

    internal static LogFormatter[] AppendFormatter(LogFormatter[] formatters, LogFormatter formatter)
    {
        if (formatter == null)
        {
            return LogFormatter.CopyFormatters(formatters);
        }

        LogFormatter[] source = formatters;
        if (source == null)
        {
            source = new LogFormatter[0];
        }

        LogFormatter[] copy = new LogFormatter[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        copy[source.Length] = formatter;
        return copy;
    }

    internal static LogFormatter[] CopyFormatters(LogFormatter[] formatters)
    {
        if (formatters == null)
        {
            return new LogFormatter[0];
        }

        LogFormatter[] copy = new LogFormatter[formatters.Length];
        int index = 0;
        while (index < formatters.Length)
        {
            copy[index] = formatters[index];
            index = index + 1;
        }

        return copy;
    }
}
