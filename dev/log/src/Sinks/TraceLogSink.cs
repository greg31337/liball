using System;

namespace log;

/// <summary>
/// Creates sinks that write formatted events through System.Diagnostics.Trace or a supplied action.
/// </summary>
internal class TraceLogSink
{
    internal const string DefaultName = "trace";

    /// <summary>
    /// Creates a trace sink using default settings.
    /// </summary>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create()
    {
        return TraceLogSink.WithFormatter(null, TextLogFormatter.Plain());
    }

    /// <summary>
    /// Creates a trace sink configured with the supplied action.
    /// </summary>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithAction(Action<string> writeAction)
    {
        return TraceLogSink.WithFormatter(writeAction, TextLogFormatter.Plain());
    }

    /// <summary>
    /// Creates a trace sink configured with the supplied formatter.
    /// </summary>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(Action<string>? writeAction, LogFormatter formatter)
    {
        LogSink sink = LogSink.Named(DefaultName);
        sink.IsTraceSink = true;
        sink.DiagnosticWriteAction = writeAction;
        sink.SinkFormatter = TextWriterLogSink.NormalizeFormatter(formatter);
        sink.FlushOnWrite = false;
        return sink;
    }

    /// <summary>
    /// Creates a trace sink with an interface-based formatter.
    /// </summary>
    /// <param name="writeAction">The callback that receives formatted text.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(Action<string>? writeAction, ILogFormatter formatter)
    {
        return TraceLogSink.WithFormatter(writeAction, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Sets write action for this trace sink.
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
    /// Sets formatter for this trace sink.
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
    /// Sets an interface-based formatter for this trace sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    internal static void SetFormatter(LogSink sink, ILogFormatter formatter)
    {
        TraceLogSink.SetFormatter(sink, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Sets flush on write for this trace sink.
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
    /// Reports whether the supplied sink writes through System.Diagnostics.Trace.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsTrace(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsTraceSink;
    }

    internal static LogResult WriteToTrace(LogSink sink, LogEvent logEvent)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.trace.null", "trace sink cannot be null");
        }

        LogFormatter formatter = TextWriterLogSink.NormalizeFormatter(sink.SinkFormatter);
        string output = formatter.FormatLine(logEvent);
        if (sink.DiagnosticWriteAction == null)
        {
            System.Diagnostics.Trace.Write(output);
            if (sink.FlushOnWrite)
            {
                System.Diagnostics.Trace.Flush();
            }
        }
        else
        {
            sink.DiagnosticWriteAction(output);
        }

        return LogSink.NormalizeResult(sink.Result);
    }
}
