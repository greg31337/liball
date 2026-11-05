using System;

namespace log;

/// <summary>
/// Fans each accepted event out to a configurable collection of sinks.
/// </summary>
internal class LogPipeline
{
    internal LogSink[] Sinks = new LogSink[0];
    /// <summary>
    /// The most recent event accepted by this object, or null when none has been accepted.
    /// </summary>
    internal LogEvent? LastEvent;
    /// <summary>
    /// The combined result returned by the most recent pipeline operation.
    /// </summary>
    internal LogResult LastResult = LogResult.Success();
    /// <summary>
    /// The cumulative number of accepted write operations.
    /// </summary>
    internal int WrittenCount;
    /// <summary>
    /// The cumulative number of dropped events.
    /// </summary>
    internal int DroppedCount;
    /// <summary>
    /// The number of failed operations observed by this object.
    /// </summary>
    internal int FailureCount;
    /// <summary>
    /// The cumulative number of diagnostic outcomes.
    /// </summary>
    internal int DiagnosticCount;
    internal object SyncRoot = new object();

    /// <summary>
    /// Initializes a new pipeline.
    /// </summary>
    internal LogPipeline()
    {
        Sinks = new LogSink[0];
        LastEvent = null;
        LastResult = LogResult.Success();
        WrittenCount = 0;
        DroppedCount = 0;
        FailureCount = 0;
        DiagnosticCount = 0;
        SyncRoot = new object();
    }

    /// <summary>
    /// Adds sink to this pipeline.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    internal void AddSink(LogSink sink)
    {
        Sinks = LogPipeline.AppendSink(Sinks, sink);
    }

    /// <summary>
    /// Adds an interface-based sink to this pipeline.
    /// </summary>
    /// <param name="sink">The destination to add.</param>
    internal void AddInterfaceSink(ILogSink sink)
    {
        AddSink(LogSink.Wrap(sink));
    }

    /// <summary>
    /// Adds pipeline to this pipeline.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="pipeline">The child pipeline that receives routed events.</param>
    internal void AddPipeline(string name, LogPipeline pipeline)
    {
        AddSink(LogSink.PipelineSink(name, pipeline));
    }

    /// <summary>
    /// Sets sinks for this pipeline.
    /// </summary>
    /// <param name="sinks">The sinks to copy into the destination.</param>
    internal void SetSinks(LogSink[] sinks)
    {
        Sinks = LogPipeline.CopySinks(sinks);
    }

    /// <summary>
    /// Replaces the pipeline sinks with interface-based destinations.
    /// </summary>
    /// <param name="sinks">The destinations to copy into this pipeline.</param>
    internal void SetInterfaceSinks(ILogSink[] sinks)
    {
        ClearSinks();
        if (sinks == null)
        {
            return;
        }

        foreach (ILogSink sink in sinks)
        {
            AddInterfaceSink(sink);
        }
    }

    /// <summary>
    /// Clears sinks and restores the default behavior.
    /// </summary>
    internal void ClearSinks()
    {
        Sinks = new LogSink[0];
    }

    /// <summary>
    /// Flushes every sink in this pipeline.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal LogResult Flush()
    {
        lock (SyncRoot)
        {
            return LogPipeline.FlushSinks(Sinks);
        }
    }

    /// <summary>
    /// Forwards pending asynchronous and batch events in every sink.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal LogResult Drain()
    {
        lock (SyncRoot)
        {
            return LogPipeline.DrainSinks(Sinks);
        }
    }

    /// <summary>
    /// Drains and closes every sink in this pipeline.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal LogResult Close()
    {
        lock (SyncRoot)
        {
            return LogPipeline.CloseSinks(Sinks);
        }
    }

    /// <summary>
    /// Returns a point-in-time health snapshot for this pipeline.
    /// </summary>
    /// <returns>A point-in-time pipeline health snapshot.</returns>
    internal LogPipelineHealth Health()
    {
        return LogPipelineHealth.FromPipeline(this);
    }

    /// <summary>
    /// Sets shutdown timeout for this pipeline.
    /// </summary>
    /// <param name="timeout">The maximum time allowed for draining or closing.</param>
    internal void SetShutdownTimeout(TimeSpan timeout)
    {
        foreach (LogSink sink in Sinks)
        {
            LogSinkLifecycle.SetShutdownTimeout(sink, timeout);
        }
    }

    /// <summary>
    /// Fans an event out to every eligible sink and combines their results.
    /// </summary>
    /// <param name="logEvent">The event to evaluate, format, or write.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal LogResult Write(LogEvent logEvent)
    {
        lock (SyncRoot)
        {
            return WriteCore(logEvent);
        }
    }

    internal LogResult WriteCore(LogEvent logEvent)
    {
        if (logEvent == null)
        {
            return CompleteWrite(LogResult.FailureCode("pipeline.event.null", "log event cannot be null"));
        }

        LastEvent = logEvent;
        WrittenCount = LogResult.IncrementCount(WrittenCount);

        if (Sinks == null || Sinks.Length == 0)
        {
            return CompleteWrite(LogResult.Success());
        }

        int sinkCount = LogPipeline.CountSinks(Sinks);
        LogResult[] results = new LogResult[sinkCount];
        int index = 0;
        foreach (LogSink sink in Sinks)
        {
            if (sink == null)
            {
                continue;
            }

            results[index] = sink.Write(logEvent);
            index = index + 1;
        }

        return CompleteWrite(LogPipeline.CombineFanoutResults(results));
    }

    internal LogResult CompleteWrite(LogResult result)
    {
        lock (SyncRoot)
        {
            LastResult = LogSink.NormalizeResult(result);
            DroppedCount = LogResult.AddCounts(DroppedCount, LastResult.DroppedCount);
            FailureCount = LogResult.AddCounts(FailureCount, LastResult.FailureCount);
            DiagnosticCount = LogResult.AddCounts(DiagnosticCount, LastResult.DiagnosticCount);
            LogSelfLog.WriteResult("pipeline", LastResult);
            return LastResult;
        }
    }

    /// <summary>
    /// Clears dropped count and restores the default behavior.
    /// </summary>
    internal void ClearDroppedCount()
    {
        DroppedCount = 0;
    }

    /// <summary>
    /// Clears failure count and restores the default behavior.
    /// </summary>
    internal void ClearFailureCount()
    {
        FailureCount = 0;
    }

    /// <summary>
    /// Clears diagnostic count and restores the default behavior.
    /// </summary>
    internal void ClearDiagnosticCount()
    {
        DiagnosticCount = 0;
    }

    /// <summary>
    /// Clears counters and restores the default behavior.
    /// </summary>
    internal void ClearCounters()
    {
        DroppedCount = 0;
        FailureCount = 0;
        DiagnosticCount = 0;
    }

    /// <summary>
    /// Reports whether at least one sink in this pipeline accepts the specified level.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal bool IsEnabled(LogLevel level)
    {
        if (Sinks == null || Sinks.Length == 0)
        {
            return true;
        }

        foreach (LogSink sink in Sinks)
        {
            if (sink != null && sink.IsEnabledForLevel(level))
            {
                return true;
            }
        }

        return false;
    }

    internal static LogResult CombineFanoutResults(LogResult[] results)
    {
        if (results == null)
        {
            return LogResult.Success();
        }

        bool hasAccepted = false;
        bool hasFailure = false;
        bool hasDropped = false;
        bool hasDiagnostic = false;
        int droppedCount = 0;
        int failureCount = 0;
        int diagnosticCount = 0;
        string code = "";
        string message = "";

        foreach (LogResult result in results)
        {
            if (result == null)
            {
                continue;
            }

            if (result.IsSuccess || result.IsDiagnostic)
            {
                hasAccepted = true;
            }

            if (result.IsFailure)
            {
                hasFailure = true;
            }

            if (result.IsDropped)
            {
                hasDropped = true;
            }

            if (result.IsDiagnostic)
            {
                hasDiagnostic = true;
            }

            droppedCount = LogResult.AddCounts(droppedCount, result.DroppedCount);
            failureCount = LogResult.AddCounts(failureCount, result.FailureCount);
            diagnosticCount = LogResult.AddCounts(diagnosticCount, result.DiagnosticCount);

            if (code.Length == 0 && result.Code.Length > 0)
            {
                code = result.Code;
            }

            if (message.Length == 0 && result.Message.Length > 0)
            {
                message = result.Message;
            }
        }

        if (hasFailure)
        {
            return new LogResult(false, true, hasDropped, hasDiagnostic, code, message, droppedCount, failureCount, diagnosticCount);
        }

        if (hasAccepted)
        {
            return new LogResult(true, false, false, hasDiagnostic, code, message, droppedCount, failureCount, diagnosticCount);
        }

        if (hasDropped)
        {
            return new LogResult(false, false, true, hasDiagnostic, code, message, droppedCount, failureCount, diagnosticCount);
        }

        return LogResult.Success();
    }

    internal static int CountSinks(LogSink[] sinks)
    {
        if (sinks == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LogSink sink in sinks)
        {
            if (sink != null)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static LogSink[] AppendSink(LogSink[] sinks, LogSink sink)
    {
        LogSink storedSink = sink;
        if (storedSink == null)
        {
            return LogPipeline.CopySinks(sinks);
        }

        LogSink[] source = sinks;
        if (source == null)
        {
            source = new LogSink[0];
        }

        LogSink[] copy = new LogSink[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        copy[source.Length] = storedSink;
        return copy;
    }

    internal static LogSink[] CopySinks(LogSink[] sinks)
    {
        if (sinks == null)
        {
            return new LogSink[0];
        }

        LogSink[] copy = new LogSink[sinks.Length];
        int index = 0;
        while (index < sinks.Length)
        {
            copy[index] = sinks[index];
            index = index + 1;
        }

        return copy;
    }

    internal static LogResult FlushSinks(LogSink[] sinks)
    {
        if (sinks == null)
        {
            return LogResult.Success();
        }

        int sinkCount = LogPipeline.CountSinks(sinks);
        LogResult[] results = new LogResult[sinkCount];
        int index = 0;
        foreach (LogSink sink in sinks)
        {
            if (sink == null)
            {
                continue;
            }

            results[index] = LogSinkLifecycle.Flush(sink);
            index = index + 1;
        }

        return LogResult.Combine(results);
    }

    internal static LogResult DrainSinks(LogSink[] sinks)
    {
        if (sinks == null)
        {
            return LogResult.Success();
        }

        int sinkCount = LogPipeline.CountSinks(sinks);
        LogResult[] results = new LogResult[sinkCount];
        int index = 0;
        foreach (LogSink sink in sinks)
        {
            if (sink == null)
            {
                continue;
            }

            results[index] = LogSinkLifecycle.Drain(sink);
            index = index + 1;
        }

        return LogResult.Combine(results);
    }

    internal static LogResult CloseSinks(LogSink[] sinks)
    {
        if (sinks == null)
        {
            return LogResult.Success();
        }

        int sinkCount = LogPipeline.CountSinks(sinks);
        LogResult[] results = new LogResult[sinkCount];
        int index = 0;
        foreach (LogSink sink in sinks)
        {
            if (sink == null)
            {
                continue;
            }

            results[index] = LogSinkLifecycle.Close(sink);
            index = index + 1;
        }

        return LogResult.Combine(results);
    }
}
