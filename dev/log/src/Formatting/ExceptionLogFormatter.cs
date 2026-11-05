using System;

namespace log;

/// <summary>
/// Creates formatters that render only an event's exception information.
/// </summary>
internal class ExceptionLogFormatter
{
    internal const int MaximumDepth = 16;

    /// <summary>
    /// Creates a formatter that renders exception type, message, stack trace, and inner exceptions.
    /// </summary>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter Full()
    {
        LogFormatter formatter = LogFormatter.Custom(LogFormatter.ExceptionName, ExceptionLogFormatter.FormatEventException);
        formatter.AppendNewLine = false;
        formatter.NewLine = Environment.NewLine;
        return formatter;
    }

    /// <summary>
    /// Creates a formatter that renders only exception type and message.
    /// </summary>
    /// <returns>The configured formatter.</returns>
    internal static LogFormatter Summary()
    {
        LogFormatter formatter = LogFormatter.Custom(LogFormatter.ExceptionName, ExceptionLogFormatter.FormatEventExceptionSummary);
        formatter.AppendNewLine = false;
        formatter.NewLine = Environment.NewLine;
        return formatter;
    }

    internal static string FormatEventException(LogEvent logEvent)
    {
        if (logEvent == null)
        {
            return "";
        }

        return ExceptionLogFormatter.FormatException(logEvent.ExceptionValue);
    }

    internal static string FormatEventExceptionSummary(LogEvent logEvent)
    {
        if (logEvent == null)
        {
            return "";
        }

        return ExceptionLogFormatter.FormatExceptionSummary(logEvent.ExceptionValue);
    }

    internal static string FormatException(Exception? exceptionValue)
    {
        if (exceptionValue == null)
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        ExceptionLogFormatter.AppendException(builder, exceptionValue, 0);
        return builder.ToString();
    }

    internal static string FormatExceptionSummary(Exception? exceptionValue)
    {
        if (exceptionValue == null)
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        ExceptionLogFormatter.AppendExceptionHeader(builder, exceptionValue);
        return builder.ToString();
    }

    internal static void AppendException(System.Text.StringBuilder builder, Exception? exceptionValue, int depth)
    {
        if (exceptionValue == null)
        {
            return;
        }

        if (depth >= MaximumDepth)
        {
            builder.Append(Environment.NewLine);
            builder.Append("Caused by: <maximum exception depth reached>");
            return;
        }

        if (depth > 0)
        {
            builder.Append(Environment.NewLine);
            builder.Append("Caused by: ");
        }

        ExceptionLogFormatter.AppendExceptionHeader(builder, exceptionValue);

        string stackTrace = "";
        string? nullableStackTrace = exceptionValue.StackTrace;
        if (nullableStackTrace != null)
        {
            stackTrace = nullableStackTrace;
        }

        if (stackTrace.Length > 0)
        {
            builder.Append(Environment.NewLine);
            builder.Append(stackTrace);
        }

        ExceptionLogFormatter.AppendException(builder, exceptionValue.InnerException, depth + 1);
    }

    internal static void AppendExceptionHeader(System.Text.StringBuilder builder, Exception exceptionValue)
    {
        builder.Append(ExceptionLogFormatter.ExceptionTypeName(exceptionValue));
        if (exceptionValue.Message.Length > 0)
        {
            builder.Append(": ");
            builder.Append(exceptionValue.Message);
        }
    }

    internal static string ExceptionTypeName(Exception? exceptionValue)
    {
        if (exceptionValue == null)
        {
            return "";
        }

        string name = "";
        string? fullName = exceptionValue.GetType().FullName;
        if (fullName != null)
        {
            name = fullName;
        }

        if (name.Length == 0)
        {
            name = exceptionValue.GetType().Name;
        }

        return name;
    }
}
