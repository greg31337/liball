namespace log;

/// <summary>
/// Captures a point-in-time health snapshot for one sink.
/// </summary>
/// <remarks>
/// The snapshot includes buffered-event counts and circuit-breaker state when those features apply.
/// </remarks>
public class LogSinkHealth
{
    /// <summary>
    /// The normalized name used to identify this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Whether the snapshot contains no current failure or open circuit.
    /// </summary>
    public bool IsHealthy;
    /// <summary>
    /// Whether this logger or sink currently accepts eligible events.
    /// </summary>
    public bool IsEnabled;
    /// <summary>
    /// Whether the sink lifecycle has completed.
    /// </summary>
    public bool IsClosed;
    /// <summary>
    /// The cumulative number of accepted write operations.
    /// </summary>
    public int WrittenCount;
    /// <summary>
    /// The cumulative number of dropped events.
    /// </summary>
    public int DroppedCount;
    /// <summary>
    /// The number of failed operations observed by this object.
    /// </summary>
    public int FailureCount;
    /// <summary>
    /// The cumulative number of diagnostic outcomes.
    /// </summary>
    public int DiagnosticCount;
    /// <summary>
    /// The number of events buffered by asynchronous or batch sinks.
    /// </summary>
    public int PendingEventCount;
    /// <summary>
    /// The stable code from the latest result.
    /// </summary>
    public string LastCode;
    /// <summary>
    /// The message from the latest result.
    /// </summary>
    public string LastMessage;
    /// <summary>
    /// Whether the latest result was a failure.
    /// </summary>
    public bool LastIsFailure;
    /// <summary>
    /// Whether the latest result reported dropped events.
    /// </summary>
    public bool LastIsDropped;
    /// <summary>
    /// Whether the latest result contained diagnostic information.
    /// </summary>
    public bool LastIsDiagnostic;
    /// <summary>
    /// Whether the sink's circuit breaker is currently preventing writes.
    /// </summary>
    public bool IsCircuitBreakerOpen;
    /// <summary>
    /// The consecutive failure count that opens the circuit.
    /// </summary>
    public int CircuitBreakerFailureThreshold;
    /// <summary>
    /// The current number of consecutive inner-sink failures.
    /// </summary>
    public int CircuitBreakerConsecutiveFailureCount;

    internal LogSinkHealth()
    {
        Name = "";
        IsHealthy = true;
        IsEnabled = true;
        IsClosed = false;
        WrittenCount = 0;
        DroppedCount = 0;
        FailureCount = 0;
        DiagnosticCount = 0;
        PendingEventCount = 0;
        LastCode = "";
        LastMessage = "";
        LastIsFailure = false;
        LastIsDropped = false;
        LastIsDiagnostic = false;
        IsCircuitBreakerOpen = false;
        CircuitBreakerFailureThreshold = 0;
        CircuitBreakerConsecutiveFailureCount = 0;
    }

    internal static LogSinkHealth FromSink(LogSink sink)
    {
        LogSinkHealth health = new LogSinkHealth();
        if (sink == null)
        {
            health.IsHealthy = false;
            health.FailureCount = 1;
            health.LastCode = "sink.null";
            health.LastMessage = "sink cannot be null";
            health.LastIsFailure = true;
            return health;
        }

        health.Name = sink.Name;
        health.IsEnabled = sink.IsEnabled;
        health.IsClosed = sink.IsClosed;
        health.WrittenCount = sink.WrittenCount;
        health.DroppedCount = sink.DroppedCount;
        health.FailureCount = sink.FailureCount;
        health.DiagnosticCount = sink.DiagnosticCount;
        health.PendingEventCount = LogSinkHealth.CountPendingEvents(sink);
        health.LastCode = sink.LastResult.Code;
        health.LastMessage = sink.LastResult.Message;
        health.LastIsFailure = sink.LastResult.IsFailure;
        health.LastIsDropped = sink.LastResult.IsDropped;
        health.LastIsDiagnostic = sink.LastResult.IsDiagnostic;
        health.IsCircuitBreakerOpen = sink.CircuitBreakerIsOpen;
        health.CircuitBreakerFailureThreshold = sink.CircuitBreakerFailureThreshold;
        health.CircuitBreakerConsecutiveFailureCount = sink.CircuitBreakerConsecutiveFailureCount;
        health.IsHealthy = sink.FailureCount == 0 && !sink.LastResult.IsFailure;
        return health;
    }

    internal static int CountPendingEvents(LogSink sink)
    {
        if (sink == null)
        {
            return 0;
        }

        int count = 0;
        if (sink.IsAsyncSink)
        {
            count = LogResult.AddCounts(count, AsyncLogSink.Count(sink));
        }

        if (sink.IsBatchSink)
        {
            count = LogResult.AddCounts(count, BatchLogSink.Count(sink));
        }

        if (sink.HasSubPipeline && sink.SubPipeline != null)
        {
            foreach (LogSink childSink in sink.SubPipeline.Sinks)
            {
                count = LogResult.AddCounts(count, LogSinkHealth.CountPendingEvents(childSink));
            }
        }

        return count;
    }
}
