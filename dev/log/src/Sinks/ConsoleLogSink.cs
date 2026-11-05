using System;

namespace log;

/// <summary>
/// Creates sinks that route formatted events to standard output or standard error.
/// </summary>
internal class ConsoleLogSink
{
    internal const string DefaultName = "console";

    /// <summary>
    /// Creates a console sink using default settings.
    /// </summary>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create()
    {
        return ConsoleLogSink.WithWriters(Console.Out, Console.Error);
    }

    /// <summary>
    /// Creates a console sink configured with the supplied writers.
    /// </summary>
    /// <param name="outputWriter">The writer used for levels below the error threshold.</param>
    /// <param name="errorWriter">The writer used for levels at or above the error threshold.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithWriters(System.IO.TextWriter? outputWriter, System.IO.TextWriter? errorWriter)
    {
        return ConsoleLogSink.WithFormatter(outputWriter, errorWriter, ConsoleTextLogFormatter.Plain());
    }

    /// <summary>
    /// Creates a console sink configured with the supplied formatter.
    /// </summary>
    /// <param name="outputWriter">The writer used for levels below the error threshold.</param>
    /// <param name="errorWriter">The writer used for levels at or above the error threshold.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(System.IO.TextWriter? outputWriter, System.IO.TextWriter? errorWriter, LogFormatter formatter)
    {
        LogSink sink = LogSink.Named(DefaultName);
        sink.IsConsoleSink = true;
        sink.ConsoleOutWriter = outputWriter;
        sink.ConsoleErrorWriter = errorWriter;
        sink.SinkFormatter = TextWriterLogSink.NormalizeFormatter(formatter);
        sink.FlushOnWrite = false;
        sink.ConsoleErrorMinimumLevel = LogLevel.Error;
        return sink;
    }

    /// <summary>
    /// Creates a console sink with an interface-based formatter.
    /// </summary>
    /// <param name="outputWriter">The writer used below the error threshold.</param>
    /// <param name="errorWriter">The writer used at or above the error threshold.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(System.IO.TextWriter? outputWriter, System.IO.TextWriter? errorWriter, ILogFormatter formatter)
    {
        return ConsoleLogSink.WithFormatter(outputWriter, errorWriter, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Creates a console sink configured with the supplied ansi color.
    /// </summary>
    /// <param name="useAnsiColor">Whether severity colors should be emitted as ANSI escape sequences.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithAnsiColor(bool useAnsiColor)
    {
        return ConsoleLogSink.WithFormatter(Console.Out, Console.Error, ConsoleTextLogFormatter.WithAnsiColor(useAnsiColor));
    }

    /// <summary>
    /// Sets writers for this console sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="outputWriter">The writer used for levels below the error threshold.</param>
    /// <param name="errorWriter">The writer used for levels at or above the error threshold.</param>
    internal static void SetWriters(LogSink sink, System.IO.TextWriter outputWriter, System.IO.TextWriter errorWriter)
    {
        if (sink == null)
        {
            return;
        }

        sink.ConsoleOutWriter = outputWriter;
        sink.ConsoleErrorWriter = errorWriter;
    }

    /// <summary>
    /// Sets formatter for this console sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    internal static void SetFormatter(LogSink sink, LogFormatter formatter)
    {
        if (sink == null)
        {
            return;
        }

        sink.SinkFormatter = TextWriterLogSink.NormalizeFormatter(formatter);
    }

    /// <summary>
    /// Sets an interface-based formatter for this console sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    internal static void SetFormatter(LogSink sink, ILogFormatter formatter)
    {
        ConsoleLogSink.SetFormatter(sink, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Sets error minimum level for this console sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="level">The minimum or event level.</param>
    internal static void SetErrorMinimumLevel(LogSink sink, LogLevel level)
    {
        if (sink == null)
        {
            return;
        }

        sink.ConsoleErrorMinimumLevel = LogLevelSwitch.NormalizeLevel(level);
    }

    /// <summary>
    /// Sets flush on write for this console sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="flushOnWrite">Whether the writer is flushed after every event.</param>
    internal static void SetFlushOnWrite(LogSink sink, bool flushOnWrite)
    {
        if (sink == null)
        {
            return;
        }

        sink.FlushOnWrite = flushOnWrite;
    }

    /// <summary>
    /// Reports whether the supplied sink routes events to console writers.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsConsole(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsConsoleSink;
    }

    internal static LogResult WriteToConsole(LogSink sink, LogEvent logEvent)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.console.null", "console sink cannot be null");
        }

        System.IO.TextWriter? writer = ConsoleLogSink.SelectWriter(sink, logEvent.Level);
        if (writer == null)
        {
            return LogResult.FailureCode("sink.console.writer.missing", "console writer cannot be null");
        }

        LogFormatter formatter = TextWriterLogSink.NormalizeFormatter(sink.SinkFormatter);
        writer.Write(formatter.FormatLine(logEvent));
        if (sink.FlushOnWrite)
        {
            writer.Flush();
        }

        return LogSink.NormalizeResult(sink.Result);
    }

    internal static System.IO.TextWriter? SelectWriter(LogSink? sink, LogLevel level)
    {
        if (sink == null)
        {
            return null;
        }

        if (ConsoleLogSink.ShouldRouteToError(sink, level))
        {
            return sink.ConsoleErrorWriter;
        }

        return sink.ConsoleOutWriter;
    }

    internal static bool ShouldRouteToError(LogSink sink, LogLevel level)
    {
        if (sink == null)
        {
            return false;
        }

        return LogSinkThresholds.IsLevelEnabled(level, sink.ConsoleErrorMinimumLevel);
    }
}
