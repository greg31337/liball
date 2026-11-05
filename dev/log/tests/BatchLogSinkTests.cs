using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class BatchLogSinkTests
{
    [Test]
    public void Create_ReturnsBatchSinkWithDefaultSize()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = BatchLogSink.Create(inner);

        Assert.That(sink.Name, Is.EqualTo(BatchLogSink.DefaultName));
        Assert.That(sink.IsEnabled, Is.True);
        Assert.That(sink.IsBatchSink, Is.True);
        Assert.That(BatchLogSink.IsBatch(sink), Is.True);
        Assert.That(sink.BatchInnerSink, Is.SameAs(inner));
        Assert.That(sink.BatchSize, Is.EqualTo(BatchLogSink.DefaultBatchSize));
        Assert.That(BatchLogSink.Count(sink), Is.EqualTo(0));
    }

    [Test]
    public void Write_BuffersUntilBatchSizeIsReached()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = BatchLogSink.WithSize(inner, 2);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Warn, "Application", "second", "second", new LogField[0]);

        LogResult firstResult = sink.Write(first);
        LogResult secondResult = sink.Write(second);
        LogEvent[] writtenEvents = MemoryLogSink.Events(inner);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(BatchLogSink.Count(sink), Is.EqualTo(0));
        Assert.That(writtenEvents.Length, Is.EqualTo(2));
        Assert.That(writtenEvents[0], Is.SameAs(first));
        Assert.That(writtenEvents[1], Is.SameAs(second));
    }

    [Test]
    public void Write_KeepsPartialBatchBuffered()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = BatchLogSink.WithSize(inner, 3);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(BatchLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(BatchLogSink.Events(sink)[0], Is.SameAs(logEvent));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(0));
        Assert.That(sink.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void WithSize_NormalizesNonPositiveBatchSizeToOne()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = BatchLogSink.WithSize(inner, 0);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(sink.BatchSize, Is.EqualTo(1));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(BatchLogSink.Count(sink), Is.EqualTo(0));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
    }

    [Test]
    public void Events_ReturnsDefensiveCopy()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = BatchLogSink.WithSize(inner, 3);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent replacement = LogEvent.Create(LogLevel.Info, "Application", "replacement", "replacement", new LogField[0]);

        sink.Write(first);
        LogEvent[] events = BatchLogSink.Events(sink);
        events[0] = replacement;

        Assert.That(BatchLogSink.Events(sink)[0], Is.SameAs(first));
    }

    [Test]
    public void Write_HonorsWrapperThresholdsAndFiltersBeforeBuffering()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = BatchLogSink.WithSize(inner, 2);
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent missingEvent = LogEvent.Create(LogLevel.Error, "Application", "missing", "missing", new LogField[0]);
        LogEvent presentEvent = LogEvent.Create(LogLevel.Error, "Application", "present", "present", new LogField[] { LogField.String("TenantId", "tenant-1") });

        sink.SetMinimumLevel(LogLevel.Error);
        sink.AddFilter(LogFilter.FieldExists("TenantId"));
        LogResult infoResult = sink.Write(infoEvent);
        LogResult missingResult = sink.Write(missingEvent);
        LogResult presentResult = sink.Write(presentEvent);

        Assert.That(infoResult.IsDropped, Is.True);
        Assert.That(missingResult.IsDropped, Is.True);
        Assert.That(presentResult.IsSuccess, Is.True);
        Assert.That(BatchLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(BatchLogSink.Events(sink)[0], Is.SameAs(presentEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_HonorsInnerThresholdsAndFiltersBeforeBuffering()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = BatchLogSink.WithSize(inner, 2);
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent missingEvent = LogEvent.Create(LogLevel.Error, "Application", "missing", "missing", new LogField[0]);
        LogEvent presentEvent = LogEvent.Create(LogLevel.Error, "Application", "present", "present", new LogField[] { LogField.String("TenantId", "tenant-1") });

        inner.SetMinimumLevel(LogLevel.Error);
        inner.AddFilter(LogFilter.FieldExists("TenantId"));
        LogResult infoResult = sink.Write(infoEvent);
        LogResult missingResult = sink.Write(missingEvent);
        LogResult presentResult = sink.Write(presentEvent);

        Assert.That(infoResult.IsDropped, Is.True);
        Assert.That(missingResult.IsDropped, Is.True);
        Assert.That(presentResult.IsSuccess, Is.True);
        Assert.That(BatchLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(BatchLogSink.Events(sink)[0], Is.SameAs(presentEvent));
    }

    [Test]
    public void Write_ReturnsFailureWhenInnerSinkIsMissing()
    {
        LogSink sink = BatchLogSink.Create(null);

        LogResult result = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.batch.inner.missing"));
    }

    [Test]
    public void Write_ReturnsFailureWhenBatchFlushFails()
    {
        LogSink inner = LogSink.Named("failure");
        LogSink sink = BatchLogSink.WithSize(inner, 1);

        inner.SetResult(LogResult.FailureCode("inner.failed", "failed"));
        LogResult result = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("inner.failed"));
        Assert.That(BatchLogSink.Count(sink), Is.EqualTo(0));
        Assert.That(inner.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Logger_CanWriteThroughBatchSink()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = BatchLogSink.WithSize(inner, 2);

        logger.AddSink(sink);
        LogResult firstResult = logger.Info("first");
        LogEvent firstEvent = logger.LastEvent!;
        LogResult secondResult = logger.Info("second");

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(2));
        Assert.That(MemoryLogSink.Events(inner)[0], Is.SameAs(firstEvent));
        Assert.That(MemoryLogSink.Events(inner)[1], Is.SameAs(logger.LastEvent));
    }
}
