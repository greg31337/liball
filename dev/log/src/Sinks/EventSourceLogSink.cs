using System;

namespace log;

/// <summary>
/// Creates sinks that translate log events into EventSource payloads.
/// </summary>
internal class EventSourceLogSink
{
    internal const string DefaultSourceName = "log";
    internal const string DefaultEventName = "Log";

    /// <summary>
    /// Creates a EventSource sink using default settings.
    /// </summary>
    /// <param name="sourceName">The EventSource name.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create(string sourceName)
    {
        return EventSourceLogSink.WithFormatter(sourceName, JsonLogFormatter.Compact());
    }

    /// <summary>
    /// Creates a EventSource sink configured with the supplied formatter.
    /// </summary>
    /// <param name="sourceName">The EventSource name.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(string sourceName, LogFormatter formatter)
    {
        LogSink sink = LogSink.Named("event-source");
        sink.IsEventSourceSink = true;
        sink.EventSourceName = EventSourceLogSink.NormalizeSourceName(sourceName);
        sink.EventSourceEventName = DefaultEventName;
        sink.SinkFormatter = TextWriterLogSink.NormalizeFormatter(formatter);
        sink.EventSourceWriter = null;
        sink.EventSourceWriteAction = null;
        return sink;
    }

    /// <summary>
    /// Creates an EventSource sink with an interface-based formatter.
    /// </summary>
    /// <param name="sourceName">The EventSource name.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(string sourceName, ILogFormatter formatter)
    {
        return EventSourceLogSink.WithFormatter(sourceName, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Creates a EventSource sink configured with the supplied action.
    /// </summary>
    /// <param name="sourceName">The EventSource name.</param>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithAction(string sourceName, Action<LogEventSourceEntry> writeAction)
    {
        LogSink sink = EventSourceLogSink.WithFormatter(sourceName, JsonLogFormatter.Compact());
        sink.EventSourceWriteAction = writeAction;
        return sink;
    }

    /// <summary>
    /// Sets event name for this EventSource sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="eventName">The application-defined event name or EventSource event name.</param>
    internal static void SetEventName(LogSink sink, string eventName)
    {
        if (sink == null)
        {
            return;
        }

        sink.EventSourceEventName = EventSourceLogSink.NormalizeEventName(eventName);
    }

    /// <summary>
    /// Sets formatter for this EventSource sink.
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
    /// Sets an interface-based formatter for this EventSource sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    internal static void SetFormatter(LogSink sink, ILogFormatter formatter)
    {
        EventSourceLogSink.SetFormatter(sink, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Sets write action for this EventSource sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="writeAction">The callback that receives each formatted message or event.</param>
    internal static void SetWriteAction(LogSink sink, Action<LogEventSourceEntry> writeAction)
    {
        if (sink == null)
        {
            return;
        }

        sink.EventSourceWriteAction = writeAction;
    }

    /// <summary>
    /// Reports whether the supplied sink emits EventSource events.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsEventSource(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsEventSourceSink;
    }

    internal static LogResult WriteToEventSource(LogSink sink, LogEvent logEvent)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.eventsource.null", "event source sink cannot be null");
        }

        LogFormatter formatter = TextWriterLogSink.NormalizeFormatter(sink.SinkFormatter);
        LogEventSourceEntry entry = EventSourceLogSink.BuildEntry(logEvent, formatter);
        if (sink.EventSourceWriteAction != null)
        {
            sink.EventSourceWriteAction(entry);
            return LogSink.NormalizeResult(sink.Result);
        }

        System.Diagnostics.Tracing.EventSource eventSource = EventSourceLogSink.EnsureEventSource(sink);
        System.Diagnostics.Tracing.EventSourceOptions options = new System.Diagnostics.Tracing.EventSourceOptions();
        options.Level = EventSourceLogSink.EventLevelForLogLevel(logEvent.Level);
        eventSource.Write(sink.EventSourceEventName, options, entry);
        return LogSink.NormalizeResult(sink.Result);
    }

    internal static LogEventSourceEntry BuildEntry(LogEvent logEvent, LogFormatter formatter)
    {
        if (logEvent == null)
        {
            return new LogEventSourceEntry("", "", "", 0, "", "", "", "");
        }

        LogFormatter normalizedFormatter = TextWriterLogSink.NormalizeFormatter(formatter);
        return new LogEventSourceEntry(
            logEvent.Timestamp.UtcDateTime.ToString("O"),
            logEvent.Level.ToString(),
            logEvent.LoggerName,
            logEvent.EventId,
            logEvent.EventName,
            LogFormatter.FormatDefault(logEvent),
            normalizedFormatter.Format(logEvent),
            ExceptionLogFormatter.FormatException(logEvent.ExceptionValue));
    }

    internal static System.Diagnostics.Tracing.EventSource EnsureEventSource(LogSink sink)
    {
        if (sink.EventSourceWriter == null)
        {
            sink.EventSourceWriter = new System.Diagnostics.Tracing.EventSource(sink.EventSourceName);
        }

        return sink.EventSourceWriter;
    }

    internal static LogResult Close(LogSink sink)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.eventsource.null", "event source sink cannot be null");
        }

        if (sink.EventSourceWriter != null)
        {
            sink.EventSourceWriter.Dispose();
            sink.EventSourceWriter = null;
        }

        return LogResult.Success();
    }

    internal static System.Diagnostics.Tracing.EventLevel EventLevelForLogLevel(LogLevel level)
    {
        if (level == LogLevel.Fatal)
        {
            return System.Diagnostics.Tracing.EventLevel.Critical;
        }

        if (level == LogLevel.Error)
        {
            return System.Diagnostics.Tracing.EventLevel.Error;
        }

        if (level == LogLevel.Warn)
        {
            return System.Diagnostics.Tracing.EventLevel.Warning;
        }

        if (level == LogLevel.Info)
        {
            return System.Diagnostics.Tracing.EventLevel.Informational;
        }

        return System.Diagnostics.Tracing.EventLevel.Verbose;
    }

    internal static string NormalizeSourceName(string sourceName)
    {
        string normalizedName = LogResult.NormalizeText(sourceName).Trim();
        if (normalizedName.Length == 0)
        {
            return DefaultSourceName;
        }

        return normalizedName;
    }

    internal static string NormalizeEventName(string eventName)
    {
        string normalizedName = LogResult.NormalizeText(eventName).Trim();
        if (normalizedName.Length == 0)
        {
            return DefaultEventName;
        }

        return normalizedName;
    }
}
