namespace log;

/// <summary>
/// Creates bounded queue sinks whose events are written to an inner sink when the queue is drained.
/// </summary>
/// <remarks>
/// This implementation does not create a background thread. Writes enqueue events; Drain, Flush, or Close forwards queued events to the inner sink in order.
/// </remarks>
internal class AsyncLogSink
{
    internal const string DefaultName = "async";
    internal const int DefaultCapacity = 1024;

    /// <summary>
    /// Creates an asynchronous queue sink using the default capacity and drop-newest behavior.
    /// </summary>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create(LogSink? innerSink)
    {
        return AsyncLogSink.WithCapacity(innerSink, DefaultCapacity);
    }

    /// <summary>
    /// Creates an asynchronous queue sink with the supplied capacity and drop-newest behavior.
    /// </summary>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <param name="capacity">The maximum number of queued events.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithCapacity(LogSink? innerSink, int capacity)
    {
        return AsyncLogSink.Named(DefaultName, innerSink, capacity);
    }

    /// <summary>
    /// Creates an asynchronous queue sink with the supplied capacity and full-queue behavior.
    /// </summary>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <param name="capacity">The maximum number of queued events.</param>
    /// <param name="mode">The action to take when the queue is full.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithBackpressure(LogSink? innerSink, int capacity, LogBackpressureMode mode)
    {
        return AsyncLogSink.NamedWithBackpressure(DefaultName, innerSink, capacity, mode);
    }

    /// <summary>
    /// Creates a named asynchronous queue sink with drop-newest behavior.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <param name="capacity">The maximum number of queued events.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Named(string name, LogSink? innerSink, int capacity)
    {
        return AsyncLogSink.NamedWithBackpressure(name, innerSink, capacity, LogBackpressureMode.DropNewest);
    }

    /// <summary>
    /// Creates a named asynchronous queue sink with explicit capacity and full-queue behavior.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <param name="capacity">The maximum number of queued events.</param>
    /// <param name="mode">The action to take when the queue is full.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink NamedWithBackpressure(string name, LogSink? innerSink, int capacity, LogBackpressureMode mode)
    {
        LogSink sink = LogSink.Named(name);
        sink.IsAsyncSink = true;
        sink.AsyncInnerSink = innerSink;
        sink.AsyncQueue = new LogEvent[0];
        sink.AsyncQueueCapacity = AsyncLogSink.NormalizeCapacity(capacity);
        sink.AsyncBackpressureMode = AsyncLogSink.NormalizeBackpressureMode(mode);
        return sink;
    }

    /// <summary>
    /// Sets inner sink for this asynchronous queue sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    internal static void SetInnerSink(LogSink sink, LogSink innerSink)
    {
        if (sink == null)
        {
            return;
        }

        sink.AsyncInnerSink = innerSink;
    }

    /// <summary>
    /// Sets capacity for this asynchronous queue sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="capacity">The maximum number of queued events.</param>
    internal static void SetCapacity(LogSink sink, int capacity)
    {
        if (sink == null)
        {
            return;
        }

        sink.AsyncQueueCapacity = AsyncLogSink.NormalizeCapacity(capacity);
        sink.AsyncQueue = AsyncLogSink.TrimToCapacity(sink.AsyncQueue, sink.AsyncQueueCapacity);
    }

    /// <summary>
    /// Sets backpressure mode for this asynchronous queue sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="mode">The action to take when the queue is full.</param>
    internal static void SetBackpressureMode(LogSink sink, LogBackpressureMode mode)
    {
        if (sink == null)
        {
            return;
        }

        sink.AsyncBackpressureMode = AsyncLogSink.NormalizeBackpressureMode(mode);
    }

    /// <summary>
    /// Reports whether the supplied sink is an asynchronous queue sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsAsync(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsAsyncSink;
    }

    /// <summary>
    /// Returns the number of buffered or retained events.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>The requested count.</returns>
    internal static int Count(LogSink sink)
    {
        if (sink == null || sink.AsyncQueue == null)
        {
            return 0;
        }

        return sink.AsyncQueue.Length;
    }

    /// <summary>
    /// Returns a defensive copy of buffered or retained events.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>A defensive copy of the requested items.</returns>
    internal static LogEvent[] Events(LogSink sink)
    {
        if (sink == null)
        {
            return new LogEvent[0];
        }

        return MemoryLogSink.CopyEvents(sink.AsyncQueue);
    }

    /// <summary>
    /// Removes queued events and resets the sink counters.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    internal static void Clear(LogSink sink)
    {
        if (sink == null)
        {
            return;
        }

        sink.AsyncQueue = new LogEvent[0];
        sink.LastEvent = null;
        sink.LastResult = LogResult.Success();
        sink.WrittenCount = 0;
        sink.DroppedCount = 0;
        sink.FailureCount = 0;
        sink.DiagnosticCount = 0;
    }

    internal static LogResult WriteToAsyncQueue(LogSink sink, LogEvent logEvent)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.async.null", "async sink cannot be null");
        }

        if (sink.AsyncInnerSink == null)
        {
            return LogResult.FailureCode("sink.async.inner.missing", "async inner sink cannot be null");
        }

        if (!sink.AsyncInnerSink.IsEnabledForLevel(logEvent.Level))
        {
            return LogResult.Dropped("async inner sink level disabled");
        }

        if (sink.AsyncInnerSink.FilterPipeline.Decide(logEvent) == LogDecision.Exclude)
        {
            return LogResult.Dropped("async inner sink event filtered");
        }

        if (AsyncLogSink.Count(sink) >= sink.AsyncQueueCapacity)
        {
            return AsyncLogSink.ApplyBackpressure(sink, logEvent);
        }

        sink.AsyncQueue = AsyncLogSink.AppendEvent(sink.AsyncQueue, logEvent);
        return LogSink.NormalizeResult(sink.Result);
    }

    internal static LogResult ApplyBackpressure(LogSink sink, LogEvent logEvent)
    {
        LogBackpressureMode mode = AsyncLogSink.NormalizeBackpressureMode(sink.AsyncBackpressureMode);
        if (mode == LogBackpressureMode.DropOldest)
        {
            sink.AsyncQueue = AsyncLogSink.RemoveFirst(sink.AsyncQueue);
            sink.AsyncQueue = AsyncLogSink.AppendEvent(sink.AsyncQueue, logEvent);
            return new LogResult(true, false, false, false, "", "async queue dropped oldest", 1, 0, 0);
        }

        if (mode == LogBackpressureMode.Block)
        {
            LogResult drainResult = AsyncLogSink.DrainOne(sink);
            if (!drainResult.ShouldContinue())
            {
                return drainResult;
            }

            sink.AsyncQueue = AsyncLogSink.AppendEvent(sink.AsyncQueue, logEvent);
            return drainResult;
        }

        return LogResult.Dropped("async queue full");
    }

    internal static LogResult DrainOne(LogSink sink)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.async.null", "async sink cannot be null");
        }

        if (sink.AsyncInnerSink == null)
        {
            return LogResult.FailureCode("sink.async.inner.missing", "async inner sink cannot be null");
        }

        if (AsyncLogSink.Count(sink) == 0)
        {
            return LogResult.Success();
        }

        LogEvent logEvent = sink.AsyncQueue[0];
        sink.AsyncQueue = AsyncLogSink.RemoveFirst(sink.AsyncQueue);
        return sink.AsyncInnerSink.Write(logEvent);
    }

    /// <summary>
    /// Dequeues all pending events in order and writes them to the inner sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Drain(LogSink sink)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.async.null", "async sink cannot be null");
        }

        if (sink.AsyncInnerSink == null)
        {
            return LogResult.FailureCode("sink.async.inner.missing", "async inner sink cannot be null");
        }

        int count = AsyncLogSink.Count(sink);
        LogResult[] results = new LogResult[count];
        int index = 0;
        while (AsyncLogSink.Count(sink) > 0)
        {
            results[index] = AsyncLogSink.DrainOne(sink);
            index = index + 1;
        }

        return LogResult.Combine(results);
    }

    internal static LogEvent[] AppendEvent(LogEvent[] events, LogEvent logEvent)
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

        LogEvent[] copy = new LogEvent[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        copy[source.Length] = logEvent;
        return copy;
    }

    internal static LogEvent[] RemoveFirst(LogEvent[] events)
    {
        if (events == null || events.Length == 0)
        {
            return new LogEvent[0];
        }

        LogEvent[] copy = new LogEvent[events.Length - 1];
        int sourceIndex = 1;
        int targetIndex = 0;
        while (sourceIndex < events.Length)
        {
            copy[targetIndex] = events[sourceIndex];
            sourceIndex = sourceIndex + 1;
            targetIndex = targetIndex + 1;
        }

        return copy;
    }

    internal static LogEvent[] TrimToCapacity(LogEvent[] events, int capacity)
    {
        LogEvent[] source = events;
        if (source == null)
        {
            source = new LogEvent[0];
        }

        int normalizedCapacity = AsyncLogSink.NormalizeCapacity(capacity);
        if (source.Length <= normalizedCapacity)
        {
            return MemoryLogSink.CopyEvents(source);
        }

        LogEvent[] copy = new LogEvent[normalizedCapacity];
        int sourceIndex = source.Length - normalizedCapacity;
        int targetIndex = 0;
        while (targetIndex < normalizedCapacity)
        {
            copy[targetIndex] = source[sourceIndex];
            targetIndex = targetIndex + 1;
            sourceIndex = sourceIndex + 1;
        }

        return copy;
    }

    internal static int NormalizeCapacity(int capacity)
    {
        if (capacity < 1)
        {
            return 1;
        }

        return capacity;
    }

    internal static LogBackpressureMode NormalizeBackpressureMode(LogBackpressureMode mode)
    {
        if (mode == LogBackpressureMode.Block)
        {
            return LogBackpressureMode.Block;
        }

        if (mode == LogBackpressureMode.DropOldest)
        {
            return LogBackpressureMode.DropOldest;
        }

        return LogBackpressureMode.DropNewest;
    }
}
