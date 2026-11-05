namespace log;

/// <summary>
/// Captures a point-in-time health snapshot for a logger pipeline.
/// </summary>
/// <remarks>
/// The snapshot is detached from later counter changes, but the contained values describe only the instant Health was called.
/// </remarks>
public class LogPipelineHealth
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
    /// The number of sinks represented by the snapshot.
    /// </summary>
    public int SinkCount;
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
    /// The per-sink snapshots captured with this pipeline snapshot.
    /// </summary>
    public LogSinkHealth[] Sinks;

    internal LogPipelineHealth()
    {
        Name = "pipeline";
        IsHealthy = true;
        SinkCount = 0;
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
        Sinks = new LogSinkHealth[0];
    }

    internal static LogPipelineHealth FromLogger(Logger logger)
    {
        if (logger == null)
        {
            return LogPipelineHealth.NullSnapshot("logger");
        }

        LogPipelineHealth health = LogPipelineHealth.FromPipeline(logger.Pipeline);
        health.Name = logger.Name;
        health.WrittenCount = logger.WrittenCount;
        health.DroppedCount = logger.DroppedCount;
        health.FailureCount = logger.FailureCount;
        health.DiagnosticCount = logger.DiagnosticCount;
        health.LastCode = logger.LastWriteResult.Code;
        health.LastMessage = logger.LastWriteResult.Message;
        health.LastIsFailure = logger.LastWriteResult.IsFailure;
        health.LastIsDropped = logger.LastWriteResult.IsDropped;
        health.LastIsDiagnostic = logger.LastWriteResult.IsDiagnostic;
        health.IsHealthy = logger.FailureCount == 0 && !logger.LastWriteResult.IsFailure && LogPipelineHealth.SinksAreHealthy(health.Sinks);
        return health;
    }

    internal static LogPipelineHealth FromPipeline(LogPipeline pipeline)
    {
        if (pipeline == null)
        {
            return LogPipelineHealth.NullSnapshot("pipeline");
        }

        LogPipelineHealth health = new LogPipelineHealth();
        health.Name = "pipeline";
        health.SinkCount = LogPipeline.CountSinks(pipeline.Sinks);
        health.WrittenCount = pipeline.WrittenCount;
        health.DroppedCount = pipeline.DroppedCount;
        health.FailureCount = pipeline.FailureCount;
        health.DiagnosticCount = pipeline.DiagnosticCount;
        health.LastCode = pipeline.LastResult.Code;
        health.LastMessage = pipeline.LastResult.Message;
        health.LastIsFailure = pipeline.LastResult.IsFailure;
        health.LastIsDropped = pipeline.LastResult.IsDropped;
        health.LastIsDiagnostic = pipeline.LastResult.IsDiagnostic;
        health.Sinks = LogPipelineHealth.SinkSnapshots(pipeline.Sinks);
        health.PendingEventCount = LogPipelineHealth.CountPendingEvents(health.Sinks);
        health.IsHealthy = pipeline.FailureCount == 0 && !pipeline.LastResult.IsFailure && LogPipelineHealth.SinksAreHealthy(health.Sinks);
        return health;
    }

    internal static LogPipelineHealth NullSnapshot(string name)
    {
        LogPipelineHealth health = new LogPipelineHealth();
        health.Name = LogResult.NormalizeText(name);
        health.IsHealthy = false;
        health.FailureCount = 1;
        health.LastCode = "pipeline.null";
        health.LastMessage = "pipeline cannot be null";
        health.LastIsFailure = true;
        return health;
    }

    internal static LogSinkHealth[] SinkSnapshots(LogSink[] sinks)
    {
        int count = LogPipeline.CountSinks(sinks);
        LogSinkHealth[] health = new LogSinkHealth[count];
        int index = 0;
        if (sinks == null)
        {
            return health;
        }

        foreach (LogSink sink in sinks)
        {
            if (sink == null)
            {
                continue;
            }

            health[index] = LogSinkHealth.FromSink(sink);
            index = index + 1;
        }

        return health;
    }

    internal static bool SinksAreHealthy(LogSinkHealth[] sinks)
    {
        if (sinks == null)
        {
            return true;
        }

        foreach (LogSinkHealth sink in sinks)
        {
            if (sink != null && !sink.IsHealthy)
            {
                return false;
            }
        }

        return true;
    }

    internal static int CountPendingEvents(LogSinkHealth[] sinks)
    {
        if (sinks == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LogSinkHealth sink in sinks)
        {
            if (sink != null)
            {
                count = LogResult.AddCounts(count, sink.PendingEventCount);
            }
        }

        return count;
    }
}
