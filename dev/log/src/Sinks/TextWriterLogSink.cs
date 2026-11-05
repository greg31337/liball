namespace log;

/// <summary>
/// Creates sinks that write formatted events to a TextWriter.
/// </summary>
internal class TextWriterLogSink
{
    internal const string DefaultName = "text-writer";

    /// <summary>
    /// Creates a text-writer sink using default settings.
    /// </summary>
    /// <param name="writer">The destination text writer.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create(System.IO.TextWriter? writer)
    {
        return TextWriterLogSink.WithFormatter(writer, TextLogFormatter.Plain());
    }

    /// <summary>
    /// Creates a text-writer sink configured with the supplied formatter.
    /// </summary>
    /// <param name="writer">The destination text writer.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(System.IO.TextWriter? writer, LogFormatter formatter)
    {
        LogSink sink = LogSink.Named(DefaultName);
        sink.IsTextWriterSink = true;
        sink.TextWriter = writer;
        sink.SinkFormatter = TextWriterLogSink.NormalizeFormatter(formatter);
        sink.FlushOnWrite = false;
        return sink;
    }

    /// <summary>
    /// Creates a text-writer sink with an interface-based formatter.
    /// </summary>
    /// <param name="writer">The destination text writer.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatter(System.IO.TextWriter? writer, ILogFormatter formatter)
    {
        return TextWriterLogSink.WithFormatter(writer, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Creates a text-writer sink configured with the supplied formatter and flush.
    /// </summary>
    /// <param name="writer">The destination text writer.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatterAndFlush(System.IO.TextWriter? writer, LogFormatter formatter)
    {
        LogSink sink = TextWriterLogSink.WithFormatter(writer, formatter);
        sink.FlushOnWrite = true;
        return sink;
    }

    /// <summary>
    /// Creates a flushed text-writer sink with an interface-based formatter.
    /// </summary>
    /// <param name="writer">The destination text writer.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithFormatterAndFlush(System.IO.TextWriter? writer, ILogFormatter formatter)
    {
        return TextWriterLogSink.WithFormatterAndFlush(writer, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Sets writer for this text-writer sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="writer">The destination text writer.</param>
    internal static void SetWriter(LogSink sink, System.IO.TextWriter writer)
    {
        if (sink == null)
        {
            return;
        }

        sink.TextWriter = writer;
    }

    /// <summary>
    /// Sets formatter for this text-writer sink.
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
    /// Sets an interface-based formatter for this text-writer sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="formatter">The formatter used to produce sink output.</param>
    internal static void SetFormatter(LogSink sink, ILogFormatter formatter)
    {
        TextWriterLogSink.SetFormatter(sink, LogFormatter.Wrap(formatter));
    }

    /// <summary>
    /// Sets flush on write for this text-writer sink.
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
    /// Reports whether the supplied sink writes to a TextWriter.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsTextWriter(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsTextWriterSink;
    }

    internal static LogResult WriteToTextWriter(LogSink sink, LogEvent logEvent)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.textwriter.null", "text writer sink cannot be null");
        }

        if (sink.TextWriter == null)
        {
            return LogResult.FailureCode("sink.textwriter.missing", "text writer cannot be null");
        }

        LogFormatter formatter = TextWriterLogSink.NormalizeFormatter(sink.SinkFormatter);
        sink.TextWriter.Write(formatter.FormatLine(logEvent));
        if (sink.FlushOnWrite)
        {
            sink.TextWriter.Flush();
        }

        return LogSink.NormalizeResult(sink.Result);
    }

    internal static LogFormatter NormalizeFormatter(LogFormatter formatter)
    {
        if (formatter == null)
        {
            return TextLogFormatter.Plain();
        }

        return formatter;
    }
}
