using System;

namespace log;

/// <summary>
/// Flushes, drains, and closes sinks, including nested and buffered sinks.
/// </summary>
/// <remarks>
/// Drain empties asynchronous and batch buffers recursively. Timeout overloads stop waiting when the configured duration expires.
/// </remarks>
internal class LogSinkLifecycle
{
    internal const int DefaultShutdownTimeoutMilliseconds = 30000;

    /// <summary>
    /// Sets shutdown timeout for this log sink lifecycle.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="timeout">The maximum time allowed for draining or closing.</param>
    internal static void SetShutdownTimeout(LogSink sink, TimeSpan timeout)
    {
        if (sink == null)
        {
            return;
        }

        sink.ShutdownTimeoutMilliseconds = LogSinkLifecycle.NormalizeTimeoutMilliseconds(timeout);
        if (sink.AsyncInnerSink != null)
        {
            LogSinkLifecycle.SetShutdownTimeout(sink.AsyncInnerSink, timeout);
        }

        if (sink.BatchInnerSink != null)
        {
            LogSinkLifecycle.SetShutdownTimeout(sink.BatchInnerSink, timeout);
        }

        if (sink.CircuitBreakerInnerSink != null)
        {
            LogSinkLifecycle.SetShutdownTimeout(sink.CircuitBreakerInnerSink, timeout);
        }

        if (sink.SubPipeline != null)
        {
            sink.SubPipeline.SetShutdownTimeout(timeout);
        }
    }

    /// <summary>
    /// Flushes a sink and its nested destinations.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Flush(LogSink sink)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.lifecycle.null", "sink cannot be null");
        }

        lock (sink.SyncRoot)
        {
            LogResult drainResult = LogSinkLifecycle.DrainWithTimeoutMilliseconds(sink, sink.ShutdownTimeoutMilliseconds);
            if (!drainResult.ShouldContinue())
            {
                sink.LastResult = drainResult;
                return drainResult;
            }

            LogResult flushResult = LogSinkLifecycle.FlushConcrete(sink);
            sink.LastResult = flushResult;
            return flushResult;
        }
    }

    /// <summary>
    /// Forwards all events buffered by a sink and its nested destinations.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Drain(LogSink sink)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.lifecycle.null", "sink cannot be null");
        }

        lock (sink.SyncRoot)
        {
            LogResult result = LogSinkLifecycle.DrainWithTimeoutMilliseconds(sink, sink.ShutdownTimeoutMilliseconds);
            sink.LastResult = result;
            return result;
        }
    }

    /// <summary>
    /// Drains buffered events, waiting no longer than the supplied timeout.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="timeout">The maximum time allowed for draining or closing.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult DrainWithTimeout(LogSink sink, TimeSpan timeout)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.lifecycle.null", "sink cannot be null");
        }

        lock (sink.SyncRoot)
        {
            LogResult result = LogSinkLifecycle.DrainWithTimeoutMilliseconds(sink, LogSinkLifecycle.NormalizeTimeoutMilliseconds(timeout));
            sink.LastResult = result;
            return result;
        }
    }

    /// <summary>
    /// Drains and closes a sink and its nested destinations.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Close(LogSink sink)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.lifecycle.null", "sink cannot be null");
        }

        return LogSinkLifecycle.CloseWithTimeoutMilliseconds(sink, sink.ShutdownTimeoutMilliseconds);
    }

    /// <summary>
    /// Drains and closes the sink, waiting no longer than the supplied timeout.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="timeout">The maximum time allowed for draining or closing.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult CloseWithTimeout(LogSink sink, TimeSpan timeout)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.lifecycle.null", "sink cannot be null");
        }

        return LogSinkLifecycle.CloseWithTimeoutMilliseconds(sink, LogSinkLifecycle.NormalizeTimeoutMilliseconds(timeout));
    }

    internal static LogResult DrainWithTimeoutMilliseconds(LogSink sink, int timeoutMilliseconds)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.lifecycle.null", "sink cannot be null");
        }

        lock (sink.SyncRoot)
        {
            return LogSinkLifecycle.DrainWithTimeoutMillisecondsCore(sink, timeoutMilliseconds);
        }
    }

    internal static LogResult DrainWithTimeoutMillisecondsCore(LogSink sink, int timeoutMilliseconds)
    {
        if (timeoutMilliseconds == 0 && LogSinkLifecycle.HasPendingWork(sink))
        {
            return LogResult.FailureCode("sink.shutdown.timeout", "sink shutdown timed out");
        }

        if (sink.IsInterfaceSink && sink.InterfaceSink != null)
        {
            return LogSink.NormalizeResult(sink.InterfaceSink.Drain());
        }

        if (sink.IsAsyncSink)
        {
            LogResult asyncResult = AsyncLogSink.Drain(sink);
            if (!asyncResult.ShouldContinue())
            {
                return asyncResult;
            }

            if (sink.AsyncInnerSink == null)
            {
                return asyncResult;
            }

            LogResult asyncInnerResult = LogSinkLifecycle.DrainWithTimeoutMilliseconds(sink.AsyncInnerSink, timeoutMilliseconds);
            return LogResult.Combine(new LogResult[] { asyncResult, asyncInnerResult });
        }

        if (sink.IsBatchSink)
        {
            LogResult batchResult = BatchLogSink.WriteBatch(sink);
            if (!batchResult.ShouldContinue())
            {
                return batchResult;
            }

            if (sink.BatchInnerSink == null)
            {
                return batchResult;
            }

            LogResult batchInnerResult = LogSinkLifecycle.DrainWithTimeoutMilliseconds(sink.BatchInnerSink, timeoutMilliseconds);
            return LogResult.Combine(new LogResult[] { batchResult, batchInnerResult });
        }

        if (sink.IsCircuitBreakerSink && sink.CircuitBreakerInnerSink != null)
        {
            return LogSinkLifecycle.DrainWithTimeoutMilliseconds(sink.CircuitBreakerInnerSink, timeoutMilliseconds);
        }

        if (sink.HasSubPipeline && sink.SubPipeline != null)
        {
            return sink.SubPipeline.Drain();
        }

        return LogResult.Success();
    }

    internal static LogResult CloseWithTimeoutMilliseconds(LogSink sink, int timeoutMilliseconds)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.lifecycle.null", "sink cannot be null");
        }

        lock (sink.SyncRoot)
        {
            return LogSinkLifecycle.CloseWithTimeoutMillisecondsCore(sink, timeoutMilliseconds);
        }
    }

    internal static LogResult CloseWithTimeoutMillisecondsCore(LogSink sink, int timeoutMilliseconds)
    {
        if (sink.IsClosed)
        {
            return LogResult.Success();
        }

        LogResult drainResult = LogSinkLifecycle.DrainWithTimeoutMilliseconds(sink, timeoutMilliseconds);
        if (!drainResult.ShouldContinue())
        {
            sink.LastResult = drainResult;
            return drainResult;
        }

        LogResult closeResult = LogSinkLifecycle.CloseConcrete(sink);
        if (closeResult.ShouldContinue())
        {
            sink.IsClosed = true;
            sink.IsEnabled = false;
        }

        sink.LastResult = closeResult;
        return closeResult;
    }

    internal static LogResult FlushConcrete(LogSink sink)
    {
        if (sink.IsInterfaceSink && sink.InterfaceSink != null)
        {
            return LogSink.NormalizeResult(sink.InterfaceSink.Flush());
        }

        if (sink.IsAsyncSink && sink.AsyncInnerSink != null)
        {
            return LogSinkLifecycle.Flush(sink.AsyncInnerSink);
        }

        if (sink.IsBatchSink && sink.BatchInnerSink != null)
        {
            return LogSinkLifecycle.Flush(sink.BatchInnerSink);
        }

        if (sink.IsCircuitBreakerSink && sink.CircuitBreakerInnerSink != null)
        {
            return LogSinkLifecycle.Flush(sink.CircuitBreakerInnerSink);
        }

        if (sink.HasSubPipeline && sink.SubPipeline != null)
        {
            return sink.SubPipeline.Flush();
        }

        if (sink.IsFileSink)
        {
            return FileLogSink.Flush(sink);
        }

        if (sink.IsTextWriterSink && sink.TextWriter != null)
        {
            sink.TextWriter.Flush();
            return LogResult.Success();
        }

        if (sink.IsConsoleSink)
        {
            if (sink.ConsoleOutWriter != null)
            {
                sink.ConsoleOutWriter.Flush();
            }

            if (sink.ConsoleErrorWriter != null)
            {
                sink.ConsoleErrorWriter.Flush();
            }
        }

        return LogResult.Success();
    }

    internal static LogResult CloseConcrete(LogSink sink)
    {
        if (sink.IsInterfaceSink && sink.InterfaceSink != null)
        {
            return LogSink.NormalizeResult(sink.InterfaceSink.Close());
        }

        if (sink.IsAsyncSink && sink.AsyncInnerSink != null)
        {
            return LogSinkLifecycle.Close(sink.AsyncInnerSink);
        }

        if (sink.IsBatchSink && sink.BatchInnerSink != null)
        {
            return LogSinkLifecycle.Close(sink.BatchInnerSink);
        }

        if (sink.IsCircuitBreakerSink && sink.CircuitBreakerInnerSink != null)
        {
            return LogSinkLifecycle.Close(sink.CircuitBreakerInnerSink);
        }

        if (sink.HasSubPipeline && sink.SubPipeline != null)
        {
            return sink.SubPipeline.Close();
        }

        if (sink.IsFileSink)
        {
            return FileLogSink.Close(sink);
        }

        if (sink.IsEventSourceSink)
        {
            return EventSourceLogSink.Close(sink);
        }

        return LogSinkLifecycle.FlushConcrete(sink);
    }

    internal static bool HasPendingWork(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        if (sink.IsAsyncSink && AsyncLogSink.Count(sink) > 0)
        {
            return true;
        }

        if (sink.IsBatchSink && BatchLogSink.Count(sink) > 0)
        {
            return true;
        }

        if (sink.HasSubPipeline && sink.SubPipeline != null)
        {
            foreach (LogSink childSink in sink.SubPipeline.Sinks)
            {
                if (LogSinkLifecycle.HasPendingWork(childSink))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static int NormalizeTimeoutMilliseconds(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
        {
            return DefaultShutdownTimeoutMilliseconds;
        }

        if (timeout.TotalMilliseconds > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)timeout.TotalMilliseconds;
    }
}
