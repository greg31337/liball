using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class NullLogSinkTests
{
    [Test]
    public void Create_ReturnsEnabledNullSink()
    {
        LogSink sink = NullLogSink.Create();

        Assert.That(sink.Name, Is.EqualTo(NullLogSink.DefaultName));
        Assert.That(sink.IsEnabled, Is.True);
        Assert.That(sink.IsNullSink, Is.True);
        Assert.That(NullLogSink.IsNull(sink), Is.True);
    }

    [Test]
    public void Write_AcceptsAndDiscardsEvent()
    {
        LogSink sink = NullLogSink.Create();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(sink.LastEvent, Is.Null);
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
        Assert.That(sink.LastResult.IsSuccess, Is.True);
    }

    [Test]
    public void Write_HonorsThresholdBeforeDiscarding()
    {
        LogSink sink = NullLogSink.Create();
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent errorEvent = LogEvent.Create(LogLevel.Error, "Application", "error", "error", new LogField[0]);

        sink.SetMinimumLevel(LogLevel.Error);
        LogResult infoResult = sink.Write(infoEvent);
        LogResult errorResult = sink.Write(errorEvent);

        Assert.That(infoResult.IsDropped, Is.True);
        Assert.That(errorResult.IsSuccess, Is.True);
        Assert.That(sink.LastEvent, Is.Null);
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_HonorsFiltersBeforeDiscarding()
    {
        LogSink sink = NullLogSink.Create();
        LogEvent missingEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);
        LogEvent presentEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[] { LogField.String("TenantId", "tenant-1") });

        sink.AddFilter(LogFilter.FieldExists("TenantId"));
        LogResult missingResult = sink.Write(missingEvent);
        LogResult presentResult = sink.Write(presentEvent);

        Assert.That(missingResult.IsDropped, Is.True);
        Assert.That(presentResult.IsSuccess, Is.True);
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
        Assert.That(sink.LastEvent, Is.Null);
    }

    [Test]
    public void Pipeline_TreatsNullSinkWriteAsAccepted()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink sink = NullLogSink.Create();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        pipeline.AddSink(sink);
        LogResult result = pipeline.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(pipeline.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.LastEvent, Is.Null);
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Logger_CanWriteThroughNullSink()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = NullLogSink.Create();

        logger.AddSink(sink);
        LogResult result = logger.Info("message");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.WrittenCount, Is.EqualTo(1));
        Assert.That(sink.LastEvent, Is.Null);
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }
}
