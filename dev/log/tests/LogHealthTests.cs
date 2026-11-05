using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogHealthTests
{
    [Test]
    public void Sink_CountsFailuresAndDiagnostics()
    {
        LogSink sink = LogSink.Named("counter");
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        sink.SetResult(LogResult.FailureCode("sink.failed", "failed"));
        sink.Write(logEvent);
        sink.SetResult(LogResult.DiagnosticCode("sink.diagnostic", "diagnostic"));
        sink.Write(logEvent);

        Assert.That(sink.FailureCount, Is.EqualTo(1));
        Assert.That(sink.DiagnosticCount, Is.EqualTo(1));
        Assert.That(sink.LastResult.IsDiagnostic, Is.True);
    }

    [Test]
    public void ClearCounters_ResetsSinkPipelineAndLoggerCounters()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = LogSink.Named("failure");

        sink.SetResult(LogResult.FailureCode("sink.failed", "failed"));
        logger.AddSink(sink);
        logger.Info("message");

        logger.ClearCounters();
        logger.Pipeline.ClearCounters();
        sink.ClearCounters();

        Assert.That(logger.FailureCount, Is.EqualTo(0));
        Assert.That(logger.DroppedCount, Is.EqualTo(0));
        Assert.That(logger.DiagnosticCount, Is.EqualTo(0));
        Assert.That(logger.Pipeline.FailureCount, Is.EqualTo(0));
        Assert.That(sink.FailureCount, Is.EqualTo(0));
    }

    [Test]
    public void PipelineHealth_ReportsSinkFailuresAndLastResult()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink success = LogSink.Named("success");
        LogSink failure = LogSink.Named("failure");
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        failure.SetResult(LogResult.FailureCode("sink.failed", "failed"));
        pipeline.SetSinks(new LogSink[] { success, failure });
        pipeline.Write(logEvent);
        LogPipelineHealth health = pipeline.Health();

        Assert.That(health.IsHealthy, Is.False);
        Assert.That(health.SinkCount, Is.EqualTo(2));
        Assert.That(health.WrittenCount, Is.EqualTo(1));
        Assert.That(health.FailureCount, Is.EqualTo(1));
        Assert.That(health.LastCode, Is.EqualTo("sink.failed"));
        Assert.That(health.Sinks[0].IsHealthy, Is.True);
        Assert.That(health.Sinks[1].IsHealthy, Is.False);
        Assert.That(health.Sinks[1].FailureCount, Is.EqualTo(1));
        Assert.That(health.Sinks[1].LastMessage, Is.EqualTo("failed"));
    }

    [Test]
    public void PipelineHealth_ReportsPendingEventsFromAsyncAndBatchSinks()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink asyncInner = MemoryLogSink.Create();
        LogSink batchInner = MemoryLogSink.Create();
        LogSink asyncSink = AsyncLogSink.WithCapacity(asyncInner, 4);
        LogSink batchSink = BatchLogSink.WithSize(batchInner, 4);

        pipeline.SetSinks(new LogSink[] { asyncSink, batchSink });
        pipeline.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));
        LogPipelineHealth health = pipeline.Health();

        Assert.That(health.PendingEventCount, Is.EqualTo(2));
        Assert.That(health.Sinks[0].PendingEventCount, Is.EqualTo(1));
        Assert.That(health.Sinks[1].PendingEventCount, Is.EqualTo(1));
        Assert.That(health.IsHealthy, Is.True);
    }

    [Test]
    public void LoggerHealth_UsesLoggerCountersAndPipelineSinks()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace).For("Application.Worker");
        LogSink failure = LogSink.Named("failure");

        failure.SetResult(LogResult.FailureCode("sink.failed", "failed"));
        logger.AddSink(failure);
        logger.Info("message");
        LogPipelineHealth health = logger.Health();

        Assert.That(health.Name, Is.EqualTo("Application.Worker"));
        Assert.That(health.IsHealthy, Is.False);
        Assert.That(health.FailureCount, Is.EqualTo(1));
        Assert.That(health.LastIsFailure, Is.True);
        Assert.That(health.Sinks.Length, Is.EqualTo(1));
        Assert.That(health.Sinks[0].Name, Is.EqualTo("failure"));
    }
}
