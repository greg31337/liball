using System;

namespace log;

/// <summary>
/// Creates human-readable console formatters with optional ANSI level colors.
/// </summary>
internal class ConsoleTextLogFormatter
{
    internal const string Reset = "\u001b[0m";
    internal const string TraceColor = "\u001b[90m";
    internal const string DebugColor = "\u001b[36m";
    internal const string InfoColor = "\u001b[32m";
    internal const string WarnColor = "\u001b[33m";
    internal const string ErrorColor = "\u001b[31m";
    internal const string FatalColor = "\u001b[1;31m";

    /// <summary>
    /// Creates a console text formatter without ANSI escape sequences.
    /// </summary>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter Plain()
    {
        LogFormatter formatter = LogFormatter.Custom(LogFormatter.ConsoleTextName, ConsoleTextLogFormatter.FormatPlain);
        formatter.IsConfigurableBuiltIn = true;
        formatter.FormatAction = null;
        formatter.UseAnsiColor = false;
        formatter.AppendNewLine = true;
        formatter.NewLine = Environment.NewLine;
        return formatter;
    }

    /// <summary>
    /// Creates a console text formatter that colors the complete line by severity.
    /// </summary>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter AnsiColor()
    {
        LogFormatter formatter = LogFormatter.Custom(LogFormatter.ConsoleTextName, ConsoleTextLogFormatter.FormatAnsi);
        formatter.IsConfigurableBuiltIn = true;
        formatter.FormatAction = null;
        formatter.UseAnsiColor = true;
        formatter.AppendNewLine = true;
        formatter.NewLine = Environment.NewLine;
        return formatter;
    }

    /// <summary>
    /// Creates a console text log formatter configured with the supplied ansi color.
    /// </summary>
    /// <param name="useAnsiColor">Whether severity colors should be emitted as ANSI escape sequences.</param>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter WithAnsiColor(bool useAnsiColor)
    {
        if (useAnsiColor)
        {
            return ConsoleTextLogFormatter.AnsiColor();
        }

        return ConsoleTextLogFormatter.Plain();
    }

    internal static string FormatPlain(LogEvent logEvent)
    {
        return TextLogFormatter.FormatPlain(logEvent);
    }

    internal static string FormatAnsi(LogEvent logEvent)
    {
        if (logEvent == null)
        {
            return "";
        }

        string line = TextLogFormatter.FormatPlain(logEvent);
        if (line.Length == 0)
        {
            return "";
        }

        return ConsoleTextLogFormatter.ColorForLevel(logEvent.Level) + line + Reset;
    }

    internal static string FormatConfigured(LogEvent logEvent, LogTimestampMode timestampMode, LogFieldOrder fieldOrder, bool useAnsiColor)
    {
        if (logEvent == null)
        {
            return "";
        }

        string line = TextLogFormatter.FormatConfigured(logEvent, timestampMode, fieldOrder);
        if (!useAnsiColor || line.Length == 0)
        {
            return line;
        }

        return ConsoleTextLogFormatter.ColorForLevel(logEvent.Level) + line + Reset;
    }

    internal static string ColorForLevel(LogLevel level)
    {
        if (level == LogLevel.Trace)
        {
            return TraceColor;
        }

        if (level == LogLevel.Debug)
        {
            return DebugColor;
        }

        if (level == LogLevel.Info)
        {
            return InfoColor;
        }

        if (level == LogLevel.Warn)
        {
            return WarnColor;
        }

        if (level == LogLevel.Error)
        {
            return ErrorColor;
        }

        if (level == LogLevel.Fatal)
        {
            return FatalColor;
        }

        return Reset;
    }

    internal static bool HasAnsiColor(string value)
    {
        string text = LogResult.NormalizeText(value);
        return text.Contains("\u001b[");
    }
}
