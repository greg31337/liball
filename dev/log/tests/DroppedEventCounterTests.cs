using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class DroppedEventCounterTests
{
    [Test]
    public void Sink_CountsDroppedWritesFromDisabledLevelAndFilterChecks()
    {
        LogSink sink = LogSink.Named("counter");
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent missingEvent = LogEvent.Create(LogLevel.Error, "Application", "missing", "missing", new LogField[0]);

        sink.IsEnabled = false;
        sink.Write(infoEvent);
        sink.IsEnabled = true;
        sink.SetMinimumLevel(LogLevel.Error);
        sink.Write(infoEvent);
        sink.AddFilter(LogFilter.FieldExists("TenantId"));
        sink.Write(missingEvent);

        Assert.That(sink.DroppedCount, Is.EqualTo(3));
        Assert.That(sink.WrittenCount, Is.EqualTo(0));
    }

    [Test]
    public void Sink_CountsSuccessfulWritesThatDropQueuedEvents()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithBackpressure(inner, 1, LogBackpressureMode.DropOldest);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);

        sink.Write(first);
        LogResult result = sink.Write(second);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DroppedCount, Is.EqualTo(1));
        Assert.That(sink.DroppedCount, Is.EqualTo(1));
        Assert.That(AsyncLogSink.Events(sink)[0], Is.SameAs(second));
    }

    [Test]
    public void Pipeline_CountsDropsReportedByFanoutSinks()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink filtered = LogSink.Named("filtered");
        LogSink accepted = LogSink.Named("accepted");

        filtered.AddFilter(LogFilter.FieldExists("TenantId"));
        pipeline.SetSinks(new LogSink[] { filtered, accepted });
        LogResult firstResult = pipeline.Write(LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]));
        LogResult secondResult = pipeline.Write(LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]));

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(firstResult.DroppedCount, Is.EqualTo(1));
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(pipeline.DroppedCount, Is.EqualTo(2));
        Assert.That(filtered.DroppedCount, Is.EqualTo(2));
        Assert.That(accepted.DroppedCount, Is.EqualTo(0));
    }

    [Test]
    public void Logger_CountsDisabledFilterAndPipelineDrops()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink filtered = LogSink.Named("filtered");
        LogSink accepted = LogSink.Named("accepted");

        logger.SetSinkMinimumLevel(LogLevel.Error);
        logger.Info("disabled");
        logger.ClearSinkMinimumLevel();
        logger.AddFilter(LogFilter.FieldExists("TenantId"));
        logger.Info("filtered");
        logger.ClearFilters();
        filtered.AddFilter(LogFilter.FieldExists("TenantId"));
        logger.SetSinks(new LogSink[] { filtered, accepted });
        LogResult fanoutResult = logger.Info("partial");

        Assert.That(fanoutResult.IsSuccess, Is.True);
        Assert.That(fanoutResult.DroppedCount, Is.EqualTo(1));
        Assert.That(logger.DroppedCount, Is.EqualTo(3));
        Assert.That(logger.Pipeline.DroppedCount, Is.EqualTo(1));
    }

    [Test]
    public void ClearDroppedCount_ResetsCountersWithoutChangingLastResults()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Error);
        LogPipeline pipeline = new LogPipeline();
        LogSink sink = LogSink.Named("counter");

        logger.Info("disabled");
        pipeline.AddSink(sink);
        sink.IsEnabled = false;
        pipeline.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));

        logger.ClearDroppedCount();
        pipeline.ClearDroppedCount();
        sink.ClearDroppedCount();

        Assert.That(logger.DroppedCount, Is.EqualTo(0));
        Assert.That(pipeline.DroppedCount, Is.EqualTo(0));
        Assert.That(sink.DroppedCount, Is.EqualTo(0));
        Assert.That(logger.LastWriteResult.IsDropped, Is.True);
        Assert.That(pipeline.LastResult.IsDropped, Is.True);
        Assert.That(sink.LastResult.IsDropped, Is.True);
    }
}
