namespace log;

/// <summary>
/// Creates sinks that stop forwarding events after consecutive inner-sink failures.
/// </summary>
/// <remarks>
/// Each successful inner write resets the consecutive-failure count. Once the threshold is reached, later writes are dropped until ResetCircuit is called.
/// </remarks>
internal class CircuitBreakerLogSink
{
    internal const string DefaultName = "circuit-breaker";
    internal const int DefaultFailureThreshold = 3;

    /// <summary>
    /// Creates a circuit-breaker sink using default settings.
    /// </summary>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Create(LogSink? innerSink)
    {
        return CircuitBreakerLogSink.WithThreshold(innerSink, DefaultFailureThreshold);
    }

    /// <summary>
    /// Creates a circuit-breaker sink configured with the supplied threshold.
    /// </summary>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <param name="failureThreshold">The consecutive failure count that opens the circuit.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink WithThreshold(LogSink? innerSink, int failureThreshold)
    {
        return CircuitBreakerLogSink.Named(DefaultName, innerSink, failureThreshold);
    }

    /// <summary>
    /// Creates a circuit-breaker sink with the supplied name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    /// <param name="failureThreshold">The consecutive failure count that opens the circuit.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Named(string name, LogSink? innerSink, int failureThreshold)
    {
        LogSink sink = LogSink.Named(name);
        sink.IsCircuitBreakerSink = true;
        sink.CircuitBreakerInnerSink = innerSink;
        sink.CircuitBreakerFailureThreshold = CircuitBreakerLogSink.NormalizeFailureThreshold(failureThreshold);
        sink.CircuitBreakerConsecutiveFailureCount = 0;
        sink.CircuitBreakerIsOpen = false;
        return sink;
    }

    /// <summary>
    /// Sets inner sink for this circuit-breaker sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="innerSink">The destination that receives forwarded events.</param>
    internal static void SetInnerSink(LogSink sink, LogSink innerSink)
    {
        if (sink == null)
        {
            return;
        }

        sink.CircuitBreakerInnerSink = innerSink;
    }

    /// <summary>
    /// Sets failure threshold for this circuit-breaker sink.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <param name="failureThreshold">The consecutive failure count that opens the circuit.</param>
    internal static void SetFailureThreshold(LogSink sink, int failureThreshold)
    {
        if (sink == null)
        {
            return;
        }

        sink.CircuitBreakerFailureThreshold = CircuitBreakerLogSink.NormalizeFailureThreshold(failureThreshold);
    }

    /// <summary>
    /// Reports whether the supplied sink uses circuit-breaker behavior.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsCircuitBreaker(LogSink sink)
    {
        if (sink == null)
        {
            return false;
        }

        return sink.IsCircuitBreakerSink;
    }

    /// <summary>
    /// Opens the circuit immediately so later writes are dropped.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    internal static void OpenCircuit(LogSink sink)
    {
        if (sink == null)
        {
            return;
        }

        sink.CircuitBreakerIsOpen = true;
    }

    /// <summary>
    /// Closes the circuit and clears the consecutive-failure count.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    internal static void ResetCircuit(LogSink sink)
    {
        if (sink == null)
        {
            return;
        }

        sink.CircuitBreakerIsOpen = false;
        sink.CircuitBreakerConsecutiveFailureCount = 0;
    }

    internal static LogResult WriteToCircuitBreaker(LogSink sink, LogEvent logEvent)
    {
        if (sink == null)
        {
            return LogResult.FailureCode("sink.circuit.null", "circuit breaker sink cannot be null");
        }

        if (sink.CircuitBreakerInnerSink == null)
        {
            return CircuitBreakerLogSink.RecordInnerResult(sink, LogResult.FailureCode("sink.circuit.inner.missing", "circuit breaker inner sink cannot be null"));
        }

        if (!sink.CircuitBreakerInnerSink.IsEnabledForLevel(logEvent.Level))
        {
            return LogResult.Dropped("circuit breaker inner sink level disabled");
        }

        if (sink.CircuitBreakerInnerSink.FilterPipeline.Decide(logEvent) == LogDecision.Exclude)
        {
            return LogResult.Dropped("circuit breaker inner sink event filtered");
        }

        return CircuitBreakerLogSink.RecordInnerResult(sink, sink.CircuitBreakerInnerSink.Write(logEvent));
    }

    internal static LogResult RecordInnerResult(LogSink sink, LogResult result)
    {
        LogResult normalizedResult = LogSink.NormalizeResult(result);
        if (normalizedResult.IsFailure)
        {
            sink.CircuitBreakerConsecutiveFailureCount = LogResult.AddCounts(sink.CircuitBreakerConsecutiveFailureCount, normalizedResult.FailureCount);
            if (sink.CircuitBreakerConsecutiveFailureCount >= sink.CircuitBreakerFailureThreshold)
            {
                sink.CircuitBreakerIsOpen = true;
            }

            return normalizedResult;
        }

        if (normalizedResult.ShouldContinue())
        {
            sink.CircuitBreakerConsecutiveFailureCount = 0;
        }

        return normalizedResult;
    }

    internal static int NormalizeFailureThreshold(int failureThreshold)
    {
        if (failureThreshold < 1)
        {
            return 1;
        }

        return failureThreshold;
    }
}
