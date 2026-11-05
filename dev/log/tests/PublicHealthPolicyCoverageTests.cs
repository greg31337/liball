using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicHealthPolicyCoverageTests
{
    [Test]
    public void PolicyConstructorsAndFactoryMethods_CoverDefaultsIntervalsAndBoundaries()
    {
        ILogFactory factory = new LogFactory();
        LogRollingPolicy defaultRolling = new LogRollingPolicy();
        LogRetentionPolicy defaultRetention = new LogRetentionPolicy();

        Assert.That(defaultRolling.Interval, Is.EqualTo(LogRollingInterval.None));
        Assert.That(defaultRolling.MaximumFileSizeBytes, Is.Zero);
        Assert.That(defaultRetention.MaximumFileCount, Is.Zero);
        Assert.That(defaultRetention.MaximumFileAge, Is.EqualTo(TimeSpan.Zero));

        foreach (LogRollingInterval interval in Enum.GetValues<LogRollingInterval>())
        {
            LogRollingPolicy policy = factory.CreateRollingPolicy(interval, 4096);
            Assert.That(policy.Interval, Is.EqualTo(interval));
            Assert.That(policy.MaximumFileSizeBytes, Is.EqualTo(4096));
        }

        LogRollingPolicy maximumRolling = factory.CreateRollingPolicy(LogRollingInterval.Year, long.MaxValue);
        LogRollingPolicy normalizedRolling = factory.CreateRollingPolicy((LogRollingInterval)int.MaxValue, -1);
        Assert.That(maximumRolling.MaximumFileSizeBytes, Is.EqualTo(long.MaxValue));
        Assert.That(normalizedRolling.Interval, Is.EqualTo(LogRollingInterval.None));
        Assert.That(normalizedRolling.MaximumFileSizeBytes, Is.Zero);

        LogRetentionPolicy maximumRetention = factory.CreateRetentionPolicy(int.MaxValue, TimeSpan.MaxValue);
        LogRetentionPolicy normalizedRetention = factory.CreateRetentionPolicy(-1, TimeSpan.FromTicks(-1));
        Assert.That(maximumRetention.MaximumFileCount, Is.EqualTo(int.MaxValue));
        Assert.That(maximumRetention.MaximumFileAge, Is.EqualTo(TimeSpan.MaxValue));
        Assert.That(normalizedRetention.MaximumFileCount, Is.Zero);
        Assert.That(normalizedRetention.MaximumFileAge, Is.EqualTo(TimeSpan.Zero));

        defaultRolling.Interval = LogRollingInterval.Month;
        defaultRolling.MaximumFileSizeBytes = 1;
        defaultRetention.MaximumFileCount = 2;
        defaultRetention.MaximumFileAge = TimeSpan.FromDays(3);
        Assert.That(defaultRolling.Interval, Is.EqualTo(LogRollingInterval.Month));
        Assert.That(defaultRolling.MaximumFileSizeBytes, Is.EqualTo(1));
        Assert.That(defaultRetention.MaximumFileCount, Is.EqualTo(2));
        Assert.That(defaultRetention.MaximumFileAge, Is.EqualTo(TimeSpan.FromDays(3)));
    }

    [Test]
    public void HealthySnapshot_ExposesEveryPipelineAndSinkFieldAndIsDetached()
    {
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;
        Logger logger = ((ILoggerFactory)factory).CreateNamedLoggerWithLevel("Application.Worker", LogLevel.Trace);
        LogSink sink = componentFactory.CreateMemorySink("memory", 0);
        ((ILoggerPipeline)logger).AddSink(sink);
        logger.WriteWithValues(LogLevel.Info, "first", null);

        LogPipelineHealth first = ((ILoggerPipeline)logger).Health();

        Assert.That(first.Name, Is.EqualTo("Application.Worker"));
        Assert.That(first.IsHealthy, Is.True);
        Assert.That(first.SinkCount, Is.EqualTo(1));
        Assert.That(first.WrittenCount, Is.EqualTo(1));
        Assert.That(first.DroppedCount, Is.Zero);
        Assert.That(first.FailureCount, Is.Zero);
        Assert.That(first.DiagnosticCount, Is.Zero);
        Assert.That(first.PendingEventCount, Is.Zero);
        Assert.That(first.LastCode, Is.Empty);
        Assert.That(first.LastMessage, Is.Empty);
        Assert.That(first.LastIsFailure, Is.False);
        Assert.That(first.LastIsDropped, Is.False);
        Assert.That(first.LastIsDiagnostic, Is.False);
        Assert.That(first.Sinks, Has.Length.EqualTo(1));

        LogSinkHealth sinkHealth = first.Sinks[0];
        Assert.That(sinkHealth.Name, Is.EqualTo("memory"));
        Assert.That(sinkHealth.IsHealthy, Is.True);
        Assert.That(sinkHealth.IsEnabled, Is.True);
        Assert.That(sinkHealth.IsClosed, Is.False);
        Assert.That(sinkHealth.WrittenCount, Is.EqualTo(1));
        Assert.That(sinkHealth.DroppedCount, Is.Zero);
        Assert.That(sinkHealth.FailureCount, Is.Zero);
        Assert.That(sinkHealth.DiagnosticCount, Is.Zero);
        Assert.That(sinkHealth.PendingEventCount, Is.Zero);
        Assert.That(sinkHealth.LastCode, Is.Empty);
        Assert.That(sinkHealth.LastMessage, Is.Empty);
        Assert.That(sinkHealth.LastIsFailure, Is.False);
        Assert.That(sinkHealth.LastIsDropped, Is.False);
        Assert.That(sinkHealth.LastIsDiagnostic, Is.False);
        Assert.That(sinkHealth.IsCircuitBreakerOpen, Is.False);
        Assert.That(sinkHealth.CircuitBreakerFailureThreshold, Is.Zero);
        Assert.That(sinkHealth.CircuitBreakerConsecutiveFailureCount, Is.Zero);

        logger.WriteWithValues(LogLevel.Info, "second", null);
        ((ILoggerPipeline)logger).Close();
        LogPipelineHealth second = ((ILoggerPipeline)logger).Health();

        Assert.That(first.WrittenCount, Is.EqualTo(1));
        Assert.That(first.Sinks[0].WrittenCount, Is.EqualTo(1));
        Assert.That(first.Sinks[0].IsClosed, Is.False);
        Assert.That(second.WrittenCount, Is.EqualTo(2));
        Assert.That(second.Sinks[0].WrittenCount, Is.EqualTo(2));
        Assert.That(second.Sinks[0].IsClosed, Is.True);
        Assert.That(second.Sinks[0].IsEnabled, Is.False);
    }

    [Test]
    public void HealthSnapshots_ReportDiagnosticsPendingWorkAndOpenCircuits()
    {
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;

        Logger diagnosticLogger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        ResultSink diagnosticDestination = new ResultSink("diagnostic", new LogResult(true, false, false, true, "sink.diagnostic", "check", 0, 0, 1));
        ((ILoggerPipeline)diagnosticLogger).AddInterfaceSink(diagnosticDestination);
        diagnosticLogger.WriteWithValues(LogLevel.Info, "diagnostic", null);
        LogPipelineHealth diagnosticHealth = ((ILoggerPipeline)diagnosticLogger).Health();

        Assert.That(diagnosticHealth.IsHealthy, Is.True);
        Assert.That(diagnosticHealth.DiagnosticCount, Is.EqualTo(1));
        Assert.That(diagnosticHealth.LastCode, Is.EqualTo("sink.diagnostic"));
        Assert.That(diagnosticHealth.LastMessage, Is.EqualTo("check"));
        Assert.That(diagnosticHealth.LastIsDiagnostic, Is.True);
        Assert.That(diagnosticHealth.Sinks[0].DiagnosticCount, Is.EqualTo(1));
        Assert.That(diagnosticHealth.Sinks[0].LastCode, Is.EqualTo("sink.diagnostic"));
        Assert.That(diagnosticHealth.Sinks[0].LastMessage, Is.EqualTo("check"));
        Assert.That(diagnosticHealth.Sinks[0].LastIsDiagnostic, Is.True);

        Logger queuedLogger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        LogSink queueInner = componentFactory.CreateMemorySink("queue-inner", 0);
        LogSink asyncSink = componentFactory.CreateAsyncSink("async", queueInner, 4, LogBackpressureMode.Block);
        ((ILoggerPipeline)queuedLogger).AddSink(asyncSink);
        queuedLogger.WriteWithValues(LogLevel.Info, "queued", null);
        LogPipelineHealth queuedHealth = ((ILoggerPipeline)queuedLogger).Health();

        Assert.That(queuedHealth.PendingEventCount, Is.EqualTo(1));
        Assert.That(queuedHealth.Sinks[0].PendingEventCount, Is.EqualTo(1));
        ((ILoggerPipeline)queuedLogger).Drain();

        Logger circuitLogger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        ResultSink failingDestination = new ResultSink("inner", new LogResult(false, true, false, false, "sink.failed", "failed", 0, 1, 0));
        LogSink failingInner = componentFactory.WrapSink(failingDestination);
        LogSink breaker = componentFactory.CreateCircuitBreakerSink("breaker", failingInner, 1);
        ((ILoggerPipeline)circuitLogger).AddSink(breaker);
        circuitLogger.WriteWithValues(LogLevel.Error, "failed", null);
        LogPipelineHealth circuitHealth = ((ILoggerPipeline)circuitLogger).Health();

        Assert.That(circuitHealth.IsHealthy, Is.False);
        Assert.That(circuitHealth.FailureCount, Is.EqualTo(1));
        Assert.That(circuitHealth.LastCode, Is.EqualTo("sink.failed"));
        Assert.That(circuitHealth.LastMessage, Is.EqualTo("failed"));
        Assert.That(circuitHealth.LastIsFailure, Is.True);
        Assert.That(circuitHealth.Sinks[0].IsHealthy, Is.False);
        Assert.That(circuitHealth.Sinks[0].FailureCount, Is.EqualTo(1));
        Assert.That(circuitHealth.Sinks[0].LastIsFailure, Is.True);
        Assert.That(circuitHealth.Sinks[0].IsCircuitBreakerOpen, Is.True);
        Assert.That(circuitHealth.Sinks[0].CircuitBreakerFailureThreshold, Is.EqualTo(1));
        Assert.That(circuitHealth.Sinks[0].CircuitBreakerConsecutiveFailureCount, Is.EqualTo(1));
    }

    class ResultSink : ILogSink
    {
        readonly string name;
        readonly LogResult result;

        public ResultSink(string sinkName, LogResult writeResult)
        {
            name = sinkName;
            result = writeResult;
        }

        public string GetName()
        {
            return name;
        }

        public bool IsEnabledForLevel(LogLevel level)
        {
            return true;
        }

        public LogResult Write(LogEvent? logEvent)
        {
            return result;
        }

        public LogResult Flush()
        {
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Drain()
        {
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Close()
        {
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }
    }
}
