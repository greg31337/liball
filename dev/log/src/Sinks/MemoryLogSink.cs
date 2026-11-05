namespace log;

/// <summary>
/// Creates sinks that retain a bounded in-memory copy of written events.
/// </summary>
internal class MemoryLogSink
{
    internal const string DefaultName = "memory";

    /// <summary>
    /// Creates a memory sink using default settings.
    /// </summary>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create()
    {
        return MemoryLogSink.Named(DefaultName);
    }

    /// <summary>
    /// Creates a memory sink with the supplied name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Named(string name)
    {
        LogSink sink = LogSink.Named(name);
        sink.IsMemorySink = true;
        sink.MemoryEvents = new LogEvent[0];
        sink.MaximumMemoryEvents = 0;
        sink.DroppedMemoryEventCount = 0;
        return sink;
    }

    /// <summary>
    /// Creates a memory sink configured with the supplied limit.
    /// </summary>
    /// <param name="maximumEvents">The maximum number of events retained.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithLimit(int maximumEvents)
    {
        LogSink sink = MemoryLogSink.Create();
        sink.MaximumMemoryEvents = MemoryLogSink.NormalizeMaximumEvents(maximumEvents);
        return sink;
    }

    /// <summary>
    /// Returns a defensive copy of the events currently retained by a memory sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>A defensive copy of the requested items.</returns>
    internal static LogEvent[] Events(LogSink sink)
    {
        if (sink == null)
        {
            return new LogEvent[0];
        }

        return MemoryLogSink.CopyEvents(sink.MemoryEvents);
    }

    /// <summary>
    /// Returns the number of events currently retained by a memory sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>The requested count.</returns>
    internal static int Count(LogSink sink)
    {
        if (sink == null || sink.MemoryEvents == null)
        {
            return 0;
        }

        return sink.MemoryEvents.Length;
    }

    /// <summary>
    /// Removes retained events and resets the sink's counters.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    internal static void Clear(LogSink sink)
    {
        if (sink == null)
        {
            return;
        }

        sink.LastEvent = null;
        sink.WrittenCount = 0;
        sink.DroppedCount = 0;
        sink.FailureCount = 0;
        sink.DiagnosticCount = 0;
        sink.LastResult = LogResult.Success();
        sink.MemoryEvents = new LogEvent[0];
        sink.DroppedMemoryEventCount = 0;
    }

    /// <summary>
    /// Reports whether the supplied sink stores events in memory.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsMemory(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsMemorySink;
    }

    internal static LogEvent[] AppendEvent(LogEvent[] events, LogEvent logEvent, int maximumEvents)
    {
        if (logEvent == null)
        {
            return MemoryLogSink.CopyEvents(events);
        }

        LogEvent[] source = events;
        if (source == null)
        {
            source = new LogEvent[0];
        }

        int limit = MemoryLogSink.NormalizeMaximumEvents(maximumEvents);
        if (limit == 0 || source.Length < limit)
        {
            LogEvent[] appended = new LogEvent[source.Length + 1];
            int appendIndex = 0;
            while (appendIndex < source.Length)
            {
                appended[appendIndex] = source[appendIndex];
                appendIndex = appendIndex + 1;
            }

            appended[source.Length] = logEvent;
            return appended;
        }

        LogEvent[] shifted = new LogEvent[limit];
        int sourceIndex = source.Length - limit + 1;
        if (sourceIndex < 0)
        {
            sourceIndex = 0;
        }

        int targetIndex = 0;
        while (targetIndex < limit - 1 && sourceIndex < source.Length)
        {
            shifted[targetIndex] = source[sourceIndex];
            targetIndex = targetIndex + 1;
            sourceIndex = sourceIndex + 1;
        }

        shifted[limit - 1] = logEvent;
        return shifted;
    }

    internal static LogEvent[] CopyEvents(LogEvent[] events)
    {
        if (events == null)
        {
            return new LogEvent[0];
        }

        LogEvent[] copy = new LogEvent[events.Length];
        int index = 0;
        while (index < events.Length)
        {
            copy[index] = events[index];
            index = index + 1;
        }

        return copy;
    }

    internal static int NormalizeMaximumEvents(int maximumEvents)
    {
        if (maximumEvents < 0)
        {
            return 0;
        }

        return maximumEvents;
    }
}
