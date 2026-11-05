using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class CircuitBreakerLogSinkTests
{
    [Test]
    public void Create_ReturnsCircuitBreakerSinkWithDefaults()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = CircuitBreakerLogSink.Create(inner);

        Assert.That(sink.Name, Is.EqualTo(CircuitBreakerLogSink.DefaultName));
        Assert.That(sink.IsCircuitBreakerSink, Is.True);
        Assert.That(CircuitBreakerLogSink.IsCircuitBreaker(sink), Is.True);
        Assert.That(sink.CircuitBreakerInnerSink, Is.SameAs(inner));
        Assert.That(sink.CircuitBreakerFailureThreshold, Is.EqualTo(CircuitBreakerLogSink.DefaultFailureThreshold));
        Assert.That(sink.CircuitBreakerIsOpen, Is.False);
    }

    [Test]
    public void Write_ForwardsSuccessfulEventsToInnerSink()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = CircuitBreakerLogSink.WithThreshold(inner, 2);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(inner)[0], Is.SameAs(logEvent));
        Assert.That(sink.CircuitBreakerConsecutiveFailureCount, Is.EqualTo(0));
    }

    [Test]
    public void Write_OpensCircuitAfterConsecutiveFailures()
    {
        int attempts = 0;
        LogSink inner = DelegateLogSink.Create(logEvent =>
        {
            attempts = attempts + 1;
            return LogResult.FailureCode("inner.failed", "failed");
        });
        LogSink sink = CircuitBreakerLogSink.WithThreshold(inner, 2);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);
        LogEvent third = LogEvent.Create(LogLevel.Info, "Application", "third", "third", new LogField[0]);

        LogResult firstResult = sink.Write(first);
        LogResult secondResult = sink.Write(second);
        LogResult thirdResult = sink.Write(third);

        Assert.That(firstResult.IsFailure, Is.True);
        Assert.That(secondResult.IsFailure, Is.True);
        Assert.That(thirdResult.IsDropped, Is.True);
        Assert.That(thirdResult.Message, Is.EqualTo("circuit breaker open"));
        Assert.That(attempts, Is.EqualTo(2));
        Assert.That(sink.CircuitBreakerIsOpen, Is.True);
        Assert.That(sink.CircuitBreakerConsecutiveFailureCount, Is.EqualTo(2));
        Assert.That(sink.FailureCount, Is.EqualTo(2));
        Assert.That(sink.DroppedCount, Is.EqualTo(1));
    }

    [Test]
    public void ResetCircuit_AllowsWritesAfterOpenCircuit()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = CircuitBreakerLogSink.WithThreshold(inner, 1);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        CircuitBreakerLogSink.OpenCircuit(sink);
        LogResult openResult = sink.Write(logEvent);
        CircuitBreakerLogSink.ResetCircuit(sink);
        LogResult resetResult = sink.Write(logEvent);

        Assert.That(openResult.IsDropped, Is.True);
        Assert.That(resetResult.IsSuccess, Is.True);
        Assert.That(sink.CircuitBreakerIsOpen, Is.False);
        Assert.That(sink.CircuitBreakerConsecutiveFailureCount, Is.EqualTo(0));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
    }

    [Test]
    public void Write_SuccessResetsConsecutiveFailureCount()
    {
        int attempts = 0;
        LogSink inner = DelegateLogSink.Create(logEvent =>
        {
            attempts = attempts + 1;
            if (attempts == 2)
            {
                return LogResult.Success();
            }

            return LogResult.FailureCode("inner.failed", "failed");
        });
        LogSink sink = CircuitBreakerLogSink.WithThreshold(inner, 2);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        sink.Write(logEvent);
        sink.Write(logEvent);
        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(sink.CircuitBreakerIsOpen, Is.False);
        Assert.That(sink.CircuitBreakerConsecutiveFailureCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_HonorsWrapperAndInnerRulesBeforeForwarding()
    {
        int attempts = 0;
        LogSink inner = DelegateLogSink.Create(logEvent =>
        {
            attempts = attempts + 1;
            return LogResult.Success();
        });
        LogSink sink = CircuitBreakerLogSink.WithThreshold(inner, 2);
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent missingEvent = LogEvent.Create(LogLevel.Error, "Application", "missing", "missing", new LogField[0]);
        LogEvent presentEvent = LogEvent.Create(LogLevel.Error, "Application", "present", "present", new LogField[] { LogField.String("TenantId", "tenant-1") });

        sink.SetMinimumLevel(LogLevel.Error);
        inner.AddFilter(LogFilter.FieldExists("TenantId"));
        LogResult infoResult = sink.Write(infoEvent);
        LogResult missingResult = sink.Write(missingEvent);
        LogResult presentResult = sink.Write(presentEvent);

        Assert.That(infoResult.IsDropped, Is.True);
        Assert.That(missingResult.IsDropped, Is.True);
        Assert.That(presentResult.IsSuccess, Is.True);
        Assert.That(attempts, Is.EqualTo(1));
    }

    [Test]
    public void Logger_DoesNotWriteToOpenCircuitBreaker()
    {
        int attempts = 0;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = DelegateLogSink.Create(logEvent =>
        {
            attempts = attempts + 1;
            return LogResult.FailureCode("inner.failed", "failed");
        });
        LogSink sink = CircuitBreakerLogSink.WithThreshold(inner, 1);

        logger.AddSink(sink);
        LogResult firstResult = logger.Info("first");
        LogResult secondResult = logger.Info("second");

        Assert.That(firstResult.IsFailure, Is.True);
        Assert.That(secondResult.IsDropped, Is.True);
        Assert.That(attempts, Is.EqualTo(1));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Health_ReportsOpenCircuitBreakerState()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink inner = LogSink.Named("failure");
        LogSink sink = CircuitBreakerLogSink.WithThreshold(inner, 1);

        inner.SetResult(LogResult.FailureCode("inner.failed", "failed"));
        pipeline.AddSink(sink);
        pipeline.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));
        LogPipelineHealth health = pipeline.Health();

        Assert.That(health.Sinks[0].IsCircuitBreakerOpen, Is.True);
        Assert.That(health.Sinks[0].CircuitBreakerFailureThreshold, Is.EqualTo(1));
        Assert.That(health.Sinks[0].CircuitBreakerConsecutiveFailureCount, Is.EqualTo(1));
    }
}
