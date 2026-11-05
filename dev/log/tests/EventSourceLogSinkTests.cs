using System.Text.Json;
using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class EventSourceLogSinkTests
{
    [Test]
    public void Create_ReturnsEventSourceSinkWithDefaults()
    {
        LogSink sink = EventSourceLogSink.Create("");

        Assert.That(sink.Name, Is.EqualTo("event-source"));
        Assert.That(sink.IsEventSourceSink, Is.True);
        Assert.That(EventSourceLogSink.IsEventSource(sink), Is.True);
        Assert.That(sink.EventSourceName, Is.EqualTo(EventSourceLogSink.DefaultSourceName));
        Assert.That(sink.EventSourceEventName, Is.EqualTo(EventSourceLogSink.DefaultEventName));
    }

    [Test]
    public void Write_EmitsEntryToConfiguredAction()
    {
        LogEventSourceEntry? captured = null;
        LogSink sink = EventSourceLogSink.WithAction("Sample.Source", entry => captured = entry);
        LogEvent logEvent = LogEvent.CreateAt(
            new DateTimeOffset(2026, 8, 3, 2, 0, 0, TimeSpan.Zero),
            LogLevel.Warn,
            "Application.Worker",
            17,
            "Processed",
            "Processed {Count}",
            "Processed 3",
            new LogField[] { LogField.Int("Count", 3) },
            null);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Timestamp, Is.EqualTo("2026-08-03T02:00:00.0000000Z"));
        Assert.That(captured.Level, Is.EqualTo("Warn"));
        Assert.That(captured.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(captured.EventId, Is.EqualTo(17));
        Assert.That(captured.EventName, Is.EqualTo("Processed"));
        Assert.That(captured.Message, Is.EqualTo("Processed 3"));
        Assert.That(JsonDocument.Parse(captured.Payload).RootElement.GetProperty("fields").GetProperty("Count").GetInt64(), Is.EqualTo(3));
        Assert.That(sink.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_UsesConfiguredFormatterForPayload()
    {
        LogEventSourceEntry? captured = null;
        LogFormatter formatter = LogFormatter.Custom("text", logEvent => "payload:" + logEvent.RenderedMessage);
        LogSink sink = EventSourceLogSink.WithFormatter("Sample.Source", formatter);

        EventSourceLogSink.SetWriteAction(sink, entry => captured = entry);
        LogResult result = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Payload, Is.EqualTo("payload:rendered"));
    }

    [Test]
    public void Write_IncludesExceptionText()
    {
        LogEventSourceEntry? captured = null;
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        LogSink sink = EventSourceLogSink.WithAction("Sample.Source", entry => captured = entry);
        LogEvent logEvent = LogEvent.CreateAt(DateTimeOffset.UnixEpoch, LogLevel.Error, "Application", 0, "", "failed", "failed", new LogField[0], exceptionValue);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Exception, Does.Contain("System.InvalidOperationException: failed"));
    }

    [Test]
    public void EventLevelForLogLevel_MapsLogLevelsToEventSourceLevels()
    {
        Assert.That(EventSourceLogSink.EventLevelForLogLevel(LogLevel.Fatal), Is.EqualTo(System.Diagnostics.Tracing.EventLevel.Critical));
        Assert.That(EventSourceLogSink.EventLevelForLogLevel(LogLevel.Error), Is.EqualTo(System.Diagnostics.Tracing.EventLevel.Error));
        Assert.That(EventSourceLogSink.EventLevelForLogLevel(LogLevel.Warn), Is.EqualTo(System.Diagnostics.Tracing.EventLevel.Warning));
        Assert.That(EventSourceLogSink.EventLevelForLogLevel(LogLevel.Info), Is.EqualTo(System.Diagnostics.Tracing.EventLevel.Informational));
        Assert.That(EventSourceLogSink.EventLevelForLogLevel(LogLevel.Debug), Is.EqualTo(System.Diagnostics.Tracing.EventLevel.Verbose));
        Assert.That(EventSourceLogSink.EventLevelForLogLevel(LogLevel.Trace), Is.EqualTo(System.Diagnostics.Tracing.EventLevel.Verbose));
    }

    [Test]
    public void Write_HonorsThresholdsAndFiltersBeforeEmitting()
    {
        int emittedCount = 0;
        LogSink sink = EventSourceLogSink.WithAction("Sample.Source", entry => emittedCount = emittedCount + 1);
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
        Assert.That(emittedCount, Is.EqualTo(1));
    }

    [Test]
    public void Logger_CanWriteThroughEventSourceSink()
    {
        LogEventSourceEntry? captured = null;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = EventSourceLogSink.WithAction("Sample.Source", entry => captured = entry);

        logger.AddSink(sink);
        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Message, Is.EqualTo("Processed 3"));
        Assert.That(JsonDocument.Parse(captured.Payload).RootElement.GetProperty("fields").GetProperty("Count").GetInt64(), Is.EqualTo(3));
    }

    [Test]
    public void Write_DefaultEventSourcePathSucceedsWithoutListener()
    {
        LogSink sink = EventSourceLogSink.Create("Sample.Source." + Guid.NewGuid().ToString("N"));

        LogResult result = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]));
        EventSourceLogSink.Close(sink);

        Assert.That(result.IsSuccess, Is.True);
    }
}
