using System;

namespace log;

/// <summary>
/// Creates sinks that forward each event to an application callback.
/// </summary>
internal class DelegateLogSink
{
    internal const string DefaultName = "delegate";

    /// <summary>
    /// Creates a delegate sink using default settings.
    /// </summary>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create(Func<LogEvent, LogResult?>? writeAction)
    {
        return DelegateLogSink.Named(DefaultName, writeAction);
    }

    /// <summary>
    /// Creates a delegate sink with the supplied name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Named(string name, Func<LogEvent, LogResult?>? writeAction)
    {
        LogSink sink = LogSink.Named(name);
        sink.IsDelegateSink = true;
        sink.DelegateWriteAction = writeAction;
        return sink;
    }

    /// <summary>
    /// Sets write action for this delegate sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    internal static void SetWriteAction(LogSink sink, Func<LogEvent, LogResult?>? writeAction)
    {
        if (sink == null)
        {
            return;
        }

        sink.DelegateWriteAction = writeAction;
    }

    /// <summary>
    /// Reports whether the supplied sink forwards events to a delegate.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsDelegate(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsDelegateSink;
    }

    internal static LogResult WriteToDelegate(LogSink sink, LogEvent logEvent)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.delegate.null", "delegate sink cannot be null");
        }

        if (sink.DelegateWriteAction == null)
        {
            return LogResult.FailureCode("sink.delegate.missing", "delegate write action cannot be null");
        }

        return LogSink.NormalizeResult(sink.DelegateWriteAction(logEvent));
    }
}
