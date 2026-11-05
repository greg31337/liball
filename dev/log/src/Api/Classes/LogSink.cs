using System;

namespace log;

/// <summary>
/// Represents a concrete logging destination or a wrapper around another destination.
/// </summary>
/// <remarks>
/// Write applies the sink level and filters before dispatch. Counters and LastResult describe the most recent observable outcomes.
/// </remarks>
public class LogSink : ILogSink, ILogSinkState, ILogSinkConfiguration
{
    /// <summary>
    /// The normalized name used to identify this object.
    /// </summary>
    internal string Name = "";
    /// <summary>
    /// Whether this logger or sink currently accepts eligible events.
    /// </summary>
    internal bool IsEnabled;
    /// <summary>
    /// The most recent event accepted by this object, or null when none has been accepted.
    /// </summary>
    internal LogEvent? LastEvent;
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
    internal LogResult Result = LogResult.Success();
    /// <summary>
    /// The result returned by the most recent sink operation.
    /// </summary>
    internal LogResult LastResult = LogResult.Success();
    internal bool HasMinimumLevel;
    internal LogLevel MinimumLevel;
    internal LogFilterPipeline FilterPipeline = new LogFilterPipeline();
    internal bool HasSubPipeline;
    internal LogPipeline? SubPipeline;
    internal LogSinkPipeline? PublicSubPipeline;
    internal bool IsNullSink;
    internal bool IsMemorySink;
    internal LogEvent[] MemoryEvents = new LogEvent[0];
    internal int MaximumMemoryEvents;
    internal int DroppedMemoryEventCount;
    internal bool IsTextWriterSink;
    internal System.IO.TextWriter? TextWriter;
    internal LogFormatter SinkFormatter = TextLogFormatter.Plain();
    internal bool FlushOnWrite;
    internal bool IsConsoleSink;
    internal System.IO.TextWriter? ConsoleOutWriter;
    internal System.IO.TextWriter? ConsoleErrorWriter;
    internal LogLevel ConsoleErrorMinimumLevel;
    internal bool IsDebugSink;
    internal bool IsTraceSink;
    internal Action<string>? DiagnosticWriteAction;
    internal bool IsFileSink;
    internal string FilePath = "";
    internal System.IO.TextWriter? FileWriter;
    internal bool FileAppend;
    internal bool FileOpenedOnce;
    internal bool FileIsOpen;
    internal bool HasRollingPolicy;
    internal LogRollingPolicy RollingPolicy = new LogRollingPolicy();
    internal string ActiveFilePath = "";
    internal string RollingPeriodKey = "";
    internal int RollingSequence;
    internal bool HasRetentionPolicy;
    internal LogRetentionPolicy RetentionPolicy = new LogRetentionPolicy();
    internal bool IsEventSourceSink;
    internal string EventSourceName = "";
    internal string EventSourceEventName = "";
    internal System.Diagnostics.Tracing.EventSource? EventSourceWriter;
    internal Action<LogEventSourceEntry>? EventSourceWriteAction;
    internal bool IsDelegateSink;
    internal Func<LogEvent, LogResult?>? DelegateWriteAction;
    internal bool IsAsyncSink;
    internal LogSink? AsyncInnerSink;
    internal LogEvent[] AsyncQueue = new LogEvent[0];
    internal int AsyncQueueCapacity;
    internal LogBackpressureMode AsyncBackpressureMode;
    internal bool IsBatchSink;
    internal LogSink? BatchInnerSink;
    internal LogEvent[] BatchEvents = new LogEvent[0];
    internal int BatchSize;
    /// <summary>
    /// Whether the sink lifecycle has completed.
    /// </summary>
    internal bool IsClosed;
    internal int ShutdownTimeoutMilliseconds;
    internal bool IsCircuitBreakerSink;
    internal LogSink? CircuitBreakerInnerSink;
    internal int CircuitBreakerFailureThreshold;
    internal int CircuitBreakerConsecutiveFailureCount;
    internal bool CircuitBreakerIsOpen;
    internal bool IsInterfaceSink;
    internal ILogSink? InterfaceSink;
    internal object SyncRoot = new object();

    internal LogSink()
    {
        Name = "";
        IsEnabled = true;
        LastEvent = null;
        WrittenCount = 0;
        DroppedCount = 0;
        FailureCount = 0;
        DiagnosticCount = 0;
        Result = LogResult.Success();
        LastResult = LogResult.Success();
        HasMinimumLevel = false;
        MinimumLevel = LogLevel.Trace;
        FilterPipeline = new LogFilterPipeline();
        HasSubPipeline = false;
        SubPipeline = null;
        PublicSubPipeline = null;
        IsNullSink = false;
        IsMemorySink = false;
        MemoryEvents = new LogEvent[0];
        MaximumMemoryEvents = 0;
        DroppedMemoryEventCount = 0;
        IsTextWriterSink = false;
        TextWriter = null;
        SinkFormatter = TextLogFormatter.Plain();
        FlushOnWrite = false;
        IsConsoleSink = false;
        ConsoleOutWriter = null;
        ConsoleErrorWriter = null;
        ConsoleErrorMinimumLevel = LogLevel.Error;
        IsDebugSink = false;
        IsTraceSink = false;
        DiagnosticWriteAction = null;
        IsFileSink = false;
        FilePath = "";
        FileWriter = null;
        FileAppend = true;
        FileOpenedOnce = false;
        FileIsOpen = false;
        HasRollingPolicy = false;
        RollingPolicy = new LogRollingPolicy();
        ActiveFilePath = "";
        RollingPeriodKey = "";
        RollingSequence = 0;
        HasRetentionPolicy = false;
        RetentionPolicy = new LogRetentionPolicy();
        IsEventSourceSink = false;
        EventSourceName = "";
        EventSourceEventName = "";
        EventSourceWriter = null;
        EventSourceWriteAction = null;
        IsDelegateSink = false;
        DelegateWriteAction = null;
        IsAsyncSink = false;
        AsyncInnerSink = null;
        AsyncQueue = new LogEvent[0];
        AsyncQueueCapacity = 0;
        AsyncBackpressureMode = LogBackpressureMode.DropNewest;
        IsBatchSink = false;
        BatchInnerSink = null;
        BatchEvents = new LogEvent[0];
        BatchSize = 0;
        IsClosed = false;
        ShutdownTimeoutMilliseconds = LogSinkLifecycle.DefaultShutdownTimeoutMilliseconds;
        IsCircuitBreakerSink = false;
        CircuitBreakerInnerSink = null;
        CircuitBreakerFailureThreshold = 0;
        CircuitBreakerConsecutiveFailureCount = 0;
        CircuitBreakerIsOpen = false;
        IsInterfaceSink = false;
        InterfaceSink = null;
        SyncRoot = new object();
    }

    /// <summary>
    /// Creates a log sink with the supplied name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink Named(string name)
    {
        LogSink sink = new LogSink();
        sink.Name = LogResult.NormalizeText(name);
        return sink;
    }

    /// <summary>
    /// Creates a named sink that routes accepted events into a child pipeline.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="pipeline">The child pipeline that receives routed events.</param>
    /// <returns>The configured sink.</returns>
    internal static LogSink PipelineSink(string name, LogPipeline pipeline)
    {
        LogSink sink = LogSink.Named(name);
        sink.SetSubPipeline(pipeline);
        return sink;
    }

    /// <summary>
    /// Wraps an interface-based sink so it can be used by concrete sink decorators and legacy APIs.
    /// </summary>
    /// <param name="sink">The interface-based destination to wrap.</param>
    /// <returns>A concrete adapter that delegates writes and lifecycle operations to the supplied sink.</returns>
    internal static LogSink Wrap(ILogSink sink)
    {
        LogSink adapter = LogSink.Named("");
        adapter.IsInterfaceSink = true;
        adapter.InterfaceSink = sink;
        if (sink == null)
        {
            adapter.IsEnabled = false;
            adapter.SetResult(LogResult.FailureCode("sink.interface.null", "interface sink cannot be null"));
            return adapter;
        }

        adapter.Name = LogResult.NormalizeText(sink.GetName());
        return adapter;
    }

    /// <summary>
    /// Returns the stable name used to identify this sink.
    /// </summary>
    /// <returns>The normalized sink name.</returns>
    public string GetName()
    {
        return Name;
    }

    /// <summary>Reports whether the sink is enabled.</summary>
    public bool GetIsEnabled()
    {
        return IsEnabled;
    }

    /// <summary>Returns the most recently accepted event.</summary>
    public LogEvent? GetLastEvent()
    {
        return LastEvent;
    }

    /// <summary>Returns the accepted-write count.</summary>
    public int GetWrittenCount()
    {
        return WrittenCount;
    }

    /// <summary>Returns the dropped-event count.</summary>
    public int GetDroppedCount()
    {
        return DroppedCount;
    }

    /// <summary>Returns the failure count.</summary>
    public int GetFailureCount()
    {
        return FailureCount;
    }

    /// <summary>Returns the diagnostic count.</summary>
    public int GetDiagnosticCount()
    {
        return DiagnosticCount;
    }

    /// <summary>Returns the most recent sink result.</summary>
    public LogResult GetLastResult()
    {
        return LastResult;
    }

    /// <summary>Reports whether the sink is closed.</summary>
    public bool GetIsClosed()
    {
        return IsClosed;
    }

    internal void SetResult(LogResult? result)
    {
        if (result == null)
        {
            Result = LogResult.Success();
        }
        else
        {
            Result = result;
        }
    }

    /// <summary>
    /// Applies this sink's level and filters, dispatches the event, and records the result.
    /// </summary>
    /// <param name="logEvent">The event to evaluate, format, or write.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Write(LogEvent? logEvent)
    {
        lock (SyncRoot)
        {
            return WriteCore(logEvent);
        }
    }

    internal LogResult WriteCore(LogEvent? logEvent)
    {
        if (logEvent == null)
        {
            return CompleteWrite(LogResult.FailureCode("sink.event.null", "log event cannot be null"));
        }

        if (IsClosed)
        {
            return CompleteWrite(LogResult.FailureCode("sink.closed", "sink is closed"));
        }

        if (IsCircuitBreakerSink && CircuitBreakerIsOpen)
        {
            return CompleteWrite(LogResult.Dropped("circuit breaker open"));
        }

        if (!IsEnabled)
        {
            return CompleteWrite(LogResult.Dropped("sink disabled"));
        }

        if (!IsEnabledForLevel(logEvent.Level))
        {
            return CompleteWrite(LogResult.Dropped("sink level disabled"));
        }

        if (FilterPipeline.Decide(logEvent) == LogDecision.Exclude)
        {
            return CompleteWrite(LogResult.Dropped("sink event filtered"));
        }

        if (IsNullSink)
        {
            WrittenCount = LogResult.IncrementCount(WrittenCount);
            return CompleteWrite(LogResult.Success());
        }

        LastEvent = logEvent;
        WrittenCount = LogResult.IncrementCount(WrittenCount);

        if (IsInterfaceSink && InterfaceSink != null)
        {
            return CompleteWrite(InterfaceSink.Write(logEvent));
        }

        if (IsMemorySink)
        {
            MemoryEvents = MemoryLogSink.AppendEvent(MemoryEvents, logEvent, MaximumMemoryEvents);
            if (MaximumMemoryEvents > 0 && MemoryEvents.Length == MaximumMemoryEvents && WrittenCount > MaximumMemoryEvents)
            {
                DroppedMemoryEventCount = WrittenCount - MaximumMemoryEvents;
            }

            return CompleteWrite(Result);
        }

        if (IsTextWriterSink)
        {
            return CompleteWrite(TextWriterLogSink.WriteToTextWriter(this, logEvent));
        }

        if (IsConsoleSink)
        {
            return CompleteWrite(ConsoleLogSink.WriteToConsole(this, logEvent));
        }

        if (IsDebugSink)
        {
            return CompleteWrite(DebugLogSink.WriteToDebug(this, logEvent));
        }

        if (IsTraceSink)
        {
            return CompleteWrite(TraceLogSink.WriteToTrace(this, logEvent));
        }

        if (IsFileSink)
        {
            return CompleteWrite(FileLogSink.WriteToFile(this, logEvent));
        }

        if (IsEventSourceSink)
        {
            return CompleteWrite(EventSourceLogSink.WriteToEventSource(this, logEvent));
        }

        if (IsDelegateSink)
        {
            return CompleteWrite(DelegateLogSink.WriteToDelegate(this, logEvent));
        }

        if (IsAsyncSink)
        {
            return CompleteWrite(AsyncLogSink.WriteToAsyncQueue(this, logEvent));
        }

        if (IsBatchSink)
        {
            return CompleteWrite(BatchLogSink.WriteToBatch(this, logEvent));
        }

        if (IsCircuitBreakerSink)
        {
            return CompleteWrite(CircuitBreakerLogSink.WriteToCircuitBreaker(this, logEvent));
        }

        if (HasSubPipeline && SubPipeline != null)
        {
            return CompleteWrite(SubPipeline.Write(logEvent));
        }

        return CompleteWrite(Result);
    }

    internal LogResult CompleteWrite(LogResult result)
    {
        LastResult = LogSink.NormalizeResult(result);
        DroppedCount = LogResult.AddCounts(DroppedCount, LastResult.DroppedCount);
        FailureCount = LogResult.AddCounts(FailureCount, LastResult.FailureCount);
        DiagnosticCount = LogResult.AddCounts(DiagnosticCount, LastResult.DiagnosticCount);
        LogSelfLog.WriteResult("sink " + Name, LastResult);
        return LastResult;
    }

    /// <summary>
    /// Clears dropped count and restores the default behavior.
    /// </summary>
    public void ClearDroppedCount()
    {
        DroppedCount = 0;
    }

    /// <summary>
    /// Clears failure count and restores the default behavior.
    /// </summary>
    public void ClearFailureCount()
    {
        FailureCount = 0;
    }

    /// <summary>
    /// Clears diagnostic count and restores the default behavior.
    /// </summary>
    public void ClearDiagnosticCount()
    {
        DiagnosticCount = 0;
    }

    /// <summary>
    /// Clears counters and restores the default behavior.
    /// </summary>
    public void ClearCounters()
    {
        DroppedCount = 0;
        FailureCount = 0;
        DiagnosticCount = 0;
    }

    /// <summary>
    /// Reports whether this sink accepts the specified level before filters are evaluated.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    public bool IsEnabledForLevel(LogLevel level)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (IsCircuitBreakerSink && CircuitBreakerIsOpen)
        {
            return false;
        }

        if (!HasMinimumLevel)
        {
            if (!IsSubPipelineEnabled(level))
            {
                return false;
            }

            if (IsInterfaceSink && InterfaceSink != null)
            {
                return InterfaceSink.IsEnabledForLevel(level);
            }

            return true;
        }

        if (!LogSinkThresholds.IsLevelEnabled(level, MinimumLevel))
        {
            return false;
        }

        if (!IsSubPipelineEnabled(level))
        {
            return false;
        }

        if (IsInterfaceSink && InterfaceSink != null)
        {
            return InterfaceSink.IsEnabledForLevel(level);
        }

        return true;
    }

    /// <summary>
    /// Flushes this sink and its nested destinations.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Flush()
    {
        return LogSinkLifecycle.Flush(this);
    }

    /// <summary>
    /// Forwards all buffered events without closing this sink.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Drain()
    {
        return LogSinkLifecycle.Drain(this);
    }

    /// <summary>
    /// Drains and closes this sink.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Close()
    {
        return LogSinkLifecycle.Close(this);
    }

    /// <summary>
    /// Sets minimum level for this log sink.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    public void SetMinimumLevel(LogLevel level)
    {
        HasMinimumLevel = true;
        MinimumLevel = LogLevelSwitch.NormalizeLevel(level);
    }

    /// <summary>
    /// Clears minimum level and restores the default behavior.
    /// </summary>
    public void ClearMinimumLevel()
    {
        HasMinimumLevel = false;
        MinimumLevel = LogLevel.Trace;
    }

    /// <summary>
    /// Adds filter to this log sink.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    public void AddFilter(LogFilter filter)
    {
        FilterPipeline.AddFilter(filter);
    }

    /// <summary>
    /// Adds an interface-based filter to this sink.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    public void AddInterfaceFilter(ILogFilter filter)
    {
        FilterPipeline.AddInterfaceFilter(filter);
    }

    /// <summary>
    /// Sets filters for this log sink.
    /// </summary>
    /// <param name="filters">The filters to copy into the destination.</param>
    public void SetFilters(LogFilter[] filters)
    {
        FilterPipeline.SetFilters(filters);
    }

    /// <summary>
    /// Replaces sink filters with interface-based rules.
    /// </summary>
    /// <param name="filters">The filters to copy into this sink.</param>
    public void SetInterfaceFilters(ILogFilter[] filters)
    {
        FilterPipeline.SetInterfaceFilters(filters);
    }

    /// <summary>
    /// Clears filters and restores the default behavior.
    /// </summary>
    public void ClearFilters()
    {
        FilterPipeline.ClearFilters();
    }

    /// <summary>
    /// Sets sub pipeline for this log sink.
    /// </summary>
    /// <param name="pipeline">The child pipeline that receives routed events.</param>
    internal void SetSubPipeline(LogPipeline pipeline)
    {
        if (pipeline == null)
        {
            ClearSubPipeline();
            return;
        }

        HasSubPipeline = true;
        SubPipeline = pipeline;
    }

    /// <summary>
    /// Clears sub pipeline and restores the default behavior.
    /// </summary>
    public void ClearSubPipeline()
    {
        HasSubPipeline = false;
        SubPipeline = null;
        PublicSubPipeline = null;
    }

    /// <summary>
    /// Reports whether the configured child pipeline accepts the specified level.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    public bool IsSubPipelineEnabled(LogLevel level)
    {
        if (!HasSubPipeline || SubPipeline == null)
        {
            return LogSinkThresholds.IsLevelEnabled(level, LogLevel.Trace);
        }

        return SubPipeline.IsEnabled(level);
    }

    internal static LogResult NormalizeResult(LogResult? result)
    {
        if (result == null)
        {
            return LogResult.Success();
        }

        return result;
    }
}
