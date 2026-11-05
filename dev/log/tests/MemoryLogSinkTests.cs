using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class MemoryLogSinkTests
{
    [Test]
    public void Create_ReturnsEnabledMemorySink()
    {
        LogSink sink = MemoryLogSink.Create();

        Assert.That(sink.Name, Is.EqualTo(MemoryLogSink.DefaultName));
        Assert.That(sink.IsEnabled, Is.True);
        Assert.That(sink.IsMemorySink, Is.True);
        Assert.That(MemoryLogSink.IsMemory(sink), Is.True);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(0));
    }

    [Test]
    public void Write_StoresAcceptedEventsInOrder()
    {
        LogSink sink = MemoryLogSink.Create();
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Warn, "Application", "second", "second", new LogField[0]);

        LogResult firstResult = sink.Write(first);
        LogResult secondResult = sink.Write(second);
        LogEvent[] events = MemoryLogSink.Events(sink);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(sink.LastEvent, Is.SameAs(second));
        Assert.That(sink.WrittenCount, Is.EqualTo(2));
        Assert.That(events.Length, Is.EqualTo(2));
        Assert.That(events[0], Is.SameAs(first));
        Assert.That(events[1], Is.SameAs(second));
    }

    [Test]
    public void Events_ReturnsDefensiveCopy()
    {
        LogSink sink = MemoryLogSink.Create();
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent replacement = LogEvent.Create(LogLevel.Info, "Application", "replacement", "replacement", new LogField[0]);

        sink.Write(first);
        LogEvent[] events = MemoryLogSink.Events(sink);
        events[0] = replacement;

        Assert.That(MemoryLogSink.Events(sink)[0], Is.SameAs(first));
    }

    [Test]
    public void WithLimit_KeepsMostRecentEventsAndCountsDroppedStoredEvents()
    {
        LogSink sink = MemoryLogSink.WithLimit(2);
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);
        LogEvent third = LogEvent.Create(LogLevel.Info, "Application", "third", "third", new LogField[0]);

        sink.Write(first);
        sink.Write(second);
        sink.Write(third);
        LogEvent[] events = MemoryLogSink.Events(sink);

        Assert.That(events.Length, Is.EqualTo(2));
        Assert.That(events[0], Is.SameAs(second));
        Assert.That(events[1], Is.SameAs(third));
        Assert.That(sink.WrittenCount, Is.EqualTo(3));
        Assert.That(sink.DroppedMemoryEventCount, Is.EqualTo(1));
    }

    [Test]
    public void Clear_RemovesStoredEventsAndResetsCounters()
    {
        LogSink sink = MemoryLogSink.Create();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        sink.Write(logEvent);
        MemoryLogSink.Clear(sink);

        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(0));
        Assert.That(sink.LastEvent, Is.Null);
        Assert.That(sink.WrittenCount, Is.EqualTo(0));
        Assert.That(sink.DroppedMemoryEventCount, Is.EqualTo(0));
    }

    [Test]
    public void Write_HonorsThresholdsAndFiltersBeforeStoring()
    {
        LogSink sink = MemoryLogSink.Create();
        LogEvent missingEvent = LogEvent.Create(LogLevel.Error, "Application", "missing", "missing", new LogField[0]);
        LogEvent presentEvent = LogEvent.Create(LogLevel.Error, "Application", "present", "present", new LogField[] { LogField.String("TenantId", "tenant-1") });
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[] { LogField.String("TenantId", "tenant-1") });

        sink.SetMinimumLevel(LogLevel.Error);
        sink.AddFilter(LogFilter.FieldExists("TenantId"));
        LogResult infoResult = sink.Write(infoEvent);
        LogResult missingResult = sink.Write(missingEvent);
        LogResult presentResult = sink.Write(presentEvent);

        Assert.That(infoResult.IsDropped, Is.True);
        Assert.That(missingResult.IsDropped, Is.True);
        Assert.That(presentResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(sink)[0], Is.SameAs(presentEvent));
    }

    [Test]
    public void Logger_CanWriteThroughMemorySink()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = MemoryLogSink.Create();

        logger.AddSink(sink);
        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(sink)[0], Is.SameAs(logger.LastEvent));
        Assert.That(MemoryLogSink.Events(sink)[0].Fields[0].Name, Is.EqualTo("Count"));
    }
}
