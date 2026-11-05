using System;

namespace log;

/// <summary>
/// Creates sinks that write formatted events through System.Diagnostics.Debug or a supplied action.
/// </summary>
internal class DebugLogSink
{
    internal const string DefaultName = "debug";

    /// <summary>
    /// Creates a debug sink using default settings.
    /// </summary>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create()
    {
        return DebugLogSink.WithFormatter(null, TextLogFormatter.Plain());
    }

    /// <summary>
    /// Creates a debug sink configured with the supplied action.
    /// </summary>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithAction(Action<string> writeAction)
    {
        return DebugLogSink.WithFormatter(writeAction, TextLogFormatter.Plain());
    }

    /// <summary>
    /// Creates a debug sink configured with the supplied formatter.
    /// </summary>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(Action<string>? writeAction, LogFormatter formatter)
    {
        LogSink sink = LogSink.Named(DefaultName);
        sink.IsDebugSink = true;
        sink.DiagnosticWriteAction = writeAction;
        sink.SinkFormatter = TextWriterLogSink.NormalizeFormatter(formatter);
        sink.FlushOnWrite = false;
        return sink;
    }

    /// <summary>
    /// Creates a debug sink with an interface-based formatter.
    /// </summary>
    /// <param name="writeAction">The callback that receives formatted text.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(Action<string>? writeAction, ILogFormatter formatter)
    {
        return DebugLogSink.WithFormatter(writeAction, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Sets write action for this debug sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    internal static void SetWriteAction(LogSink sink, Action<string> writeAction)
    {
        if (sink == null)
        {
            return;
        }

        sink.DiagnosticWriteAction = writeAction;
    }

    /// <summary>
    /// Sets formatter for this debug sink.
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
    /// Sets an interface-based formatter for this debug sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    internal static void SetFormatter(LogSink sink, ILogFormatter formatter)
    {
        DebugLogSink.SetFormatter(sink, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Sets flush on write for this debug sink.
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
    /// Reports whether the supplied sink writes through System.Diagnostics.Debug.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsDebug(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsDebugSink;
    }

    internal static LogResult WriteToDebug(LogSink sink, LogEvent logEvent)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.debug.null", "debug sink cannot be null");
        }

        LogFormatter formatter = TextWriterLogSink.NormalizeFormatter(sink.SinkFormatter);
        string output = formatter.FormatLine(logEvent);
        if (sink.DiagnosticWriteAction == null)
        {
            System.Diagnostics.Debug.Write(output);
            if (sink.FlushOnWrite)
            {
                System.Diagnostics.Debug.Flush();
            }
        }
        else
        {
            sink.DiagnosticWriteAction(output);
        }

        return LogSink.NormalizeResult(sink.Result);
    }
}
