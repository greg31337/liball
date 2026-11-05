using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogDiagnosticsHealthWorkflowTests
{
    [SetUp]
    public void SetUp()
    {
        LogSelfLog.Disable();
        LogSelfLog.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        LogSelfLog.Disable();
        LogSelfLog.Clear();
    }

    [Test]
    public void OpenCircuitBreaker_ReportsSelfLogEntriesAndUnhealthySnapshots()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = LogSink.Named("inner");
        LogSink breaker = CircuitBreakerLogSink.WithThreshold(inner, 1);

        LogSelfLog.EnableMemory();
        inner.SetResult(LogResult.FailureCode("inner.failed", "failed"));
        logger.AddSink(breaker);
        LogResult firstResult = logger.Error("first");
        LogResult secondResult = logger.Error("second");
        LogPipelineHealth health = logger.Health();
        string[] entries = LogSelfLog.Entries();

        Assert.That(firstResult.IsFailure, Is.True);
        Assert.That(secondResult.IsDropped, Is.True);
        Assert.That(entries.Length, Is.EqualTo(4));
        Assert.That(entries[0], Does.Contain("sink inner"));
        Assert.That(entries[1], Does.Contain("sink circuit-breaker"));
        Assert.That(entries[2], Does.Contain("pipeline"));
        Assert.That(entries[3], Does.Contain("logger"));
        Assert.That(health.IsHealthy, Is.False);
        Assert.That(health.FailureCount, Is.EqualTo(1));
        Assert.That(health.DroppedCount, Is.EqualTo(1));
        Assert.That(health.LastIsDropped, Is.True);
        Assert.That(health.Sinks[0].IsHealthy, Is.False);
        Assert.That(health.Sinks[0].IsCircuitBreakerOpen, Is.True);
        Assert.That(health.Sinks[0].CircuitBreakerConsecutiveFailureCount, Is.EqualTo(1));
        Assert.That(health.Sinks[0].DroppedCount, Is.EqualTo(0));
        Assert.That(health.Sinks[0].LastIsFailure, Is.True);
    }

    [Test]
    public void DiagnosticResults_AreSelfLoggedAndDoNotMakeHealthUnhealthy()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = DelegateLogSink.Named("diagnostic-inner", logEvent => LogResult.DiagnosticCode("inner.diagnostic", "check"));
        LogSink breaker = CircuitBreakerLogSink.WithThreshold(inner, 2);

        LogSelfLog.EnableMemory();
        logger.AddSink(breaker);
        LogResult result = logger.Info("diagnostic");
        LogPipelineHealth health = logger.Health();
        string[] entries = LogSelfLog.Entries();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IsDiagnostic, Is.True);
        Assert.That(entries.Length, Is.EqualTo(4));
        Assert.That(entries[0], Does.Contain("sink diagnostic-inner"));
        Assert.That(entries[1], Does.Contain("sink circuit-breaker"));
        Assert.That(entries[2], Does.Contain("pipeline"));
        Assert.That(entries[3], Does.Contain("logger"));
        Assert.That(health.IsHealthy, Is.True);
        Assert.That(health.DiagnosticCount, Is.EqualTo(1));
        Assert.That(health.LastIsDiagnostic, Is.True);
        Assert.That(health.Sinks[0].IsHealthy, Is.True);
        Assert.That(health.Sinks[0].DiagnosticCount, Is.EqualTo(1));
        Assert.That(health.Sinks[0].IsCircuitBreakerOpen, Is.False);
    }

    [Test]
    public void HealthSnapshot_CountsPendingWorkInsideSubPipelineWrappers()
    {
        LogPipeline root = new LogPipeline();
        LogPipeline branch = new LogPipeline();
        LogSink asyncInner = MemoryLogSink.Create();
        LogSink batchInner = MemoryLogSink.Create();
        LogSink asyncSink = AsyncLogSink.WithCapacity(asyncInner, 4);
        LogSink batchSink = BatchLogSink.WithSize(batchInner, 4);

        branch.SetSinks(new LogSink[] { asyncSink, batchSink });
        root.AddPipeline("branch", branch);
        root.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));
        LogPipelineHealth health = root.Health();

        Assert.That(health.IsHealthy, Is.True);
        Assert.That(health.SinkCount, Is.EqualTo(1));
        Assert.That(health.PendingEventCount, Is.EqualTo(2));
        Assert.That(health.Sinks[0].Name, Is.EqualTo("branch"));
        Assert.That(health.Sinks[0].PendingEventCount, Is.EqualTo(2));
        Assert.That(MemoryLogSink.Count(asyncInner), Is.EqualTo(0));
        Assert.That(MemoryLogSink.Count(batchInner), Is.EqualTo(0));
    }

    [Test]
    public void CircuitBreakerResetAndCounterClear_AllowsHealthySnapshotAfterLaterSuccess()
    {
        int attempts = 0;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = DelegateLogSink.Create(logEvent =>
        {
            attempts = attempts + 1;
            if (attempts == 1)
            {
                return LogResult.FailureCode("inner.failed", "failed");
            }

            return LogResult.Success();
        });
        LogSink breaker = CircuitBreakerLogSink.WithThreshold(inner, 1);

        logger.AddSink(breaker);
        LogResult firstResult = logger.Info("first");
        LogPipelineHealth failedHealth = logger.Health();
        CircuitBreakerLogSink.ResetCircuit(breaker);
        logger.ClearCounters();
        logger.Pipeline.ClearCounters();
        breaker.ClearCounters();
        LogResult secondResult = logger.Info("second");
        LogPipelineHealth recoveredHealth = logger.Health();

        Assert.That(firstResult.IsFailure, Is.True);
        Assert.That(failedHealth.IsHealthy, Is.False);
        Assert.That(failedHealth.Sinks[0].IsCircuitBreakerOpen, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(recoveredHealth.IsHealthy, Is.True);
        Assert.That(recoveredHealth.FailureCount, Is.EqualTo(0));
        Assert.That(recoveredHealth.Sinks[0].IsCircuitBreakerOpen, Is.False);
        Assert.That(recoveredHealth.Sinks[0].FailureCount, Is.EqualTo(0));
        Assert.That(recoveredHealth.Sinks[0].CircuitBreakerConsecutiveFailureCount, Is.EqualTo(0));
    }
}
