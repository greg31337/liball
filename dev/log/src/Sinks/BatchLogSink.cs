namespace log;

/// <summary>
/// Creates sinks that buffer events until a configured batch size is reached or the sink is flushed.
/// </summary>
/// <remarks>
/// A full batch is forwarded in order. Flush or Close sends a partial batch before completing the inner sink lifecycle.
/// </remarks>
internal class BatchLogSink
{
    internal const string DefaultName = "batch";
    internal const int DefaultBatchSize = 50;

    /// <summary>
    /// Creates a batching sink using default settings.
    /// </summary>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create(LogSink? innerSink)
    {
        return BatchLogSink.WithSize(innerSink, DefaultBatchSize);
    }

    /// <summary>
    /// Creates a batching sink configured with the supplied size.
    /// </summary>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <param name="batchSize">The number of events that triggers a batch write.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithSize(LogSink? innerSink, int batchSize)
    {
        return BatchLogSink.Named(DefaultName, innerSink, batchSize);
    }

    /// <summary>
    /// Creates a batching sink with the supplied name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <param name="batchSize">The number of events that triggers a batch write.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Named(string name, LogSink? innerSink, int batchSize)
    {
        LogSink sink = LogSink.Named(name);
        sink.IsBatchSink = true;
        sink.BatchInnerSink = innerSink;
        sink.BatchEvents = new LogEvent[0];
        sink.BatchSize = BatchLogSink.NormalizeBatchSize(batchSize);
        return sink;
    }

    /// <summary>
    /// Sets inner sink for this batching sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    internal static void SetInnerSink(LogSink sink, LogSink innerSink)
    {
        if (sink == null)
        {
            return;
        }

        sink.BatchInnerSink = innerSink;
    }

    /// <summary>
    /// Sets batch size for this batching sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="batchSize">The number of events that triggers a batch write.</param>
    internal static void SetBatchSize(LogSink sink, int batchSize)
    {
        if (sink == null)
        {
            return;
        }

        sink.BatchSize = BatchLogSink.NormalizeBatchSize(batchSize);
    }

    /// <summary>
    /// Reports whether the supplied sink buffers events in batches.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsBatch(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsBatchSink;
    }

    /// <summary>
    /// Returns the number of buffered or retained events.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>The requested count.</returns>
    internal static int Count(LogSink sink)
    {
        if (sink == null || sink.BatchEvents == null)
        {
            return 0;
        }

        return sink.BatchEvents.Length;
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

        return MemoryLogSink.CopyEvents(sink.BatchEvents);
    }

    /// <summary>
    /// Removes buffered events and resets the sink counters.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    internal static void Clear(LogSink sink)
    {
        if (sink == null)
        {
            return;
        }

        sink.BatchEvents = new LogEvent[0];
        sink.LastEvent = null;
        sink.LastResult = LogResult.Success();
        sink.WrittenCount = 0;
        sink.DroppedCount = 0;
        sink.FailureCount = 0;
        sink.DiagnosticCount = 0;
    }

    internal static LogResult WriteToBatch(LogSink sink, LogEvent logEvent)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.batch.null", "batch sink cannot be null");
        }

        if (sink.BatchInnerSink == null)
        {
            return LogResult.FailureCode("sink.batch.inner.missing", "batch inner sink cannot be null");
        }

        if (!sink.BatchInnerSink.IsEnabledForLevel(logEvent.Level))
        {
            return LogResult.Dropped("batch inner sink level disabled");
        }

        if (sink.BatchInnerSink.FilterPipeline.Decide(logEvent) == LogDecision.Exclude)
        {
            return LogResult.Dropped("batch inner sink event filtered");
        }

        sink.BatchEvents = AsyncLogSink.AppendEvent(sink.BatchEvents, logEvent);
        if (BatchLogSink.Count(sink) < sink.BatchSize)
        {
            return LogSink.NormalizeResult(sink.Result);
        }

        return BatchLogSink.WriteBatch(sink);
    }

    /// <summary>
    /// Writes the currently buffered batch to the inner sink and clears the buffer.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult WriteBatch(LogSink sink)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.batch.null", "batch sink cannot be null");
        }

        if (sink.BatchInnerSink == null)
        {
            return LogResult.FailureCode("sink.batch.inner.missing", "batch inner sink cannot be null");
        }

        int count = BatchLogSink.Count(sink);
        LogEvent[] events = BatchLogSink.Events(sink);
        sink.BatchEvents = new LogEvent[0];
        LogResult[] results = new LogResult[count];
        int index = 0;
        while (index < events.Length)
        {
            results[index] = sink.BatchInnerSink.Write(events[index]);
            index = index + 1;
        }

        return LogResult.Combine(results);
    }

    internal static int NormalizeBatchSize(int batchSize)
    {
        if (batchSize < 1)
        {
            return 1;
        }

        return batchSize;
    }
}
