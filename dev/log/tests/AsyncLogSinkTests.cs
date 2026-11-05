using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class AsyncLogSinkTests
{
    [Test]
    public void Create_ReturnsAsyncSinkWithDefaultCapacity()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.Create(inner);

        Assert.That(sink.Name, Is.EqualTo(AsyncLogSink.DefaultName));
        Assert.That(sink.IsEnabled, Is.True);
        Assert.That(sink.IsAsyncSink, Is.True);
        Assert.That(AsyncLogSink.IsAsync(sink), Is.True);
        Assert.That(sink.AsyncInnerSink, Is.SameAs(inner));
        Assert.That(sink.AsyncQueueCapacity, Is.EqualTo(AsyncLogSink.DefaultCapacity));
        Assert.That(sink.AsyncBackpressureMode, Is.EqualTo(LogBackpressureMode.DropNewest));
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(0));
    }

    [Test]
    public void Write_EnqueuesWithoutWritingInnerSink()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(sink.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(AsyncLogSink.Events(sink)[0], Is.SameAs(logEvent));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(0));
    }

    [Test]
    public void DrainOne_WritesNextQueuedEventToInnerSink()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);

        sink.Write(first);
        sink.Write(second);
        LogResult result = AsyncLogSink.DrainOne(sink);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(AsyncLogSink.Events(sink)[0], Is.SameAs(second));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(inner)[0], Is.SameAs(first));
    }

    [Test]
    public void Drain_WritesQueuedEventsInOrder()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Warn, "Application", "second", "second", new LogField[0]);

        sink.Write(first);
        sink.Write(second);
        LogResult result = AsyncLogSink.Drain(sink);
        LogEvent[] events = MemoryLogSink.Events(inner);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(0));
        Assert.That(events.Length, Is.EqualTo(2));
        Assert.That(events[0], Is.SameAs(first));
        Assert.That(events[1], Is.SameAs(second));
    }

    [Test]
    public void Write_DropsNewestWhenQueueIsFull()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithBackpressure(inner, 1, LogBackpressureMode.DropNewest);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);

        LogResult firstResult = sink.Write(first);
        LogResult secondResult = sink.Write(second);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsDropped, Is.True);
        Assert.That(secondResult.Message, Is.EqualTo("async queue full"));
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(AsyncLogSink.Events(sink)[0], Is.SameAs(first));
    }

    [Test]
    public void Write_DropsOldestWhenConfigured()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithBackpressure(inner, 1, LogBackpressureMode.DropOldest);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);

        LogResult firstResult = sink.Write(first);
        LogResult secondResult = sink.Write(second);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(secondResult.DroppedCount, Is.EqualTo(1));
        Assert.That(secondResult.Message, Is.EqualTo("async queue dropped oldest"));
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(AsyncLogSink.Events(sink)[0], Is.SameAs(second));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(0));
    }

    [Test]
    public void Write_BlocksByDrainingOneQueuedEventWhenConfigured()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithBackpressure(inner, 1, LogBackpressureMode.Block);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);

        LogResult firstResult = sink.Write(first);
        LogResult secondResult = sink.Write(second);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(AsyncLogSink.Events(sink)[0], Is.SameAs(second));
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(inner)[0], Is.SameAs(first));
    }

    [Test]
    public void Write_BlockModeReturnsFailureWhenDrainingFails()
    {
        LogSink inner = LogSink.Named("failure");
        LogSink sink = AsyncLogSink.WithBackpressure(inner, 1, LogBackpressureMode.Block);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);

        inner.SetResult(LogResult.FailureCode("inner.failed", "failed"));
        LogResult firstResult = sink.Write(first);
        LogResult secondResult = sink.Write(second);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsFailure, Is.True);
        Assert.That(secondResult.Code, Is.EqualTo("inner.failed"));
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(0));
        Assert.That(inner.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void SetBackpressureMode_ChangesFullQueueBehavior()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithBackpressure(inner, 1, LogBackpressureMode.DropNewest);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);

        AsyncLogSink.SetBackpressureMode(sink, LogBackpressureMode.DropOldest);
        sink.Write(first);
        LogResult result = sink.Write(second);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DroppedCount, Is.EqualTo(1));
        Assert.That(sink.AsyncBackpressureMode, Is.EqualTo(LogBackpressureMode.DropOldest));
        Assert.That(AsyncLogSink.Events(sink)[0], Is.SameAs(second));
    }

    [Test]
    public void Write_HonorsWrapperThresholdsAndFiltersBeforeEnqueue()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);
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
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(AsyncLogSink.Events(sink)[0], Is.SameAs(presentEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_HonorsInnerThresholdsAndFiltersBeforeEnqueue()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);
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
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(AsyncLogSink.Events(sink)[0], Is.SameAs(presentEvent));
    }

    [Test]
    public void SetCapacity_TrimsOldestQueuedEvents()
    {
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);
        LogEvent third = LogEvent.Create(LogLevel.Info, "Application", "third", "third", new LogField[0]);

        sink.Write(first);
        sink.Write(second);
        sink.Write(third);
        AsyncLogSink.SetCapacity(sink, 2);
        LogEvent[] events = AsyncLogSink.Events(sink);

        Assert.That(events.Length, Is.EqualTo(2));
        Assert.That(events[0], Is.SameAs(second));
        Assert.That(events[1], Is.SameAs(third));
    }

    [Test]
    public void Logger_CanWriteThroughAsyncSinkAndDrainLater()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink inner = MemoryLogSink.Create();
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);

        logger.AddSink(sink);
        LogResult writeResult = logger.InfoWithValues("Processed {Count}", new object[] { 3 });
        LogResult drainResult = AsyncLogSink.Drain(sink);

        Assert.That(writeResult.IsSuccess, Is.True);
        Assert.That(drainResult.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(MemoryLogSink.Count(inner), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(inner)[0], Is.SameAs(logger.LastEvent));
    }

    [Test]
    public void Drain_ReturnsFailureWhenInnerSinkFails()
    {
        LogSink inner = LogSink.Named("failure");
        LogSink sink = AsyncLogSink.WithCapacity(inner, 4);

        inner.SetResult(LogResult.FailureCode("inner.failed", "failed"));
        sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));
        LogResult result = AsyncLogSink.Drain(sink);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("inner.failed"));
        Assert.That(AsyncLogSink.Count(sink), Is.EqualTo(0));
        Assert.That(inner.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_ReturnsFailureWhenInnerSinkIsMissing()
    {
        LogSink sink = AsyncLogSink.Create(null);

        LogResult result = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.async.inner.missing"));
    }
}
