using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class TextWriterLogSinkTests
{
    [Test]
    public void Create_ReturnsTextWriterSink()
    {
        StringWriter writer = new StringWriter();
        LogSink sink = TextWriterLogSink.Create(writer);

        Assert.That(sink.Name, Is.EqualTo(TextWriterLogSink.DefaultName));
        Assert.That(sink.IsTextWriterSink, Is.True);
        Assert.That(TextWriterLogSink.IsTextWriter(sink), Is.True);
        Assert.That(sink.TextWriter, Is.SameAs(writer));
        Assert.That(sink.SinkFormatter.Name, Is.EqualTo(LogFormatter.TextName));
    }

    [Test]
    public void Write_FormatsEventToTextWriter()
    {
        StringWriter writer = new StringWriter();
        LogSink sink = TextWriterLogSink.Create(writer);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        LogResult result = sink.Write(logEvent);
        string output = writer.ToString();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(output, Does.EndWith(Environment.NewLine));
        Assert.That(output, Does.Contain("[Info] Application: rendered"));
        Assert.That(sink.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_UsesConfiguredFormatter()
    {
        StringWriter writer = new StringWriter();
        LogFormatter formatter = LogFormatter.Custom("text", logEvent => "custom:" + logEvent.RenderedMessage);
        LogSink sink = TextWriterLogSink.WithFormatter(writer, formatter);
        LogEvent logEvent = LogEvent.Create(LogLevel.Warn, "Application", "message", "rendered", new LogField[0]);

        formatter.AppendNewLine = true;
        formatter.NewLine = "\n";
        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(writer.ToString(), Is.EqualTo("custom:rendered\n"));
    }

    [Test]
    public void Write_ReturnsFailureWhenWriterIsMissing()
    {
        LogSink sink = TextWriterLogSink.Create(null);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.textwriter.missing"));
    }

    [Test]
    public void Write_HonorsThresholdsAndFiltersBeforeWriting()
    {
        StringWriter writer = new StringWriter();
        LogSink sink = TextWriterLogSink.Create(writer);
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
        Assert.That(writer.ToString(), Does.Contain("present"));
        Assert.That(writer.ToString(), Does.Not.Contain("info"));
        Assert.That(writer.ToString(), Does.Not.Contain("missing"));
    }

    [Test]
    public void Write_FlushesWhenConfigured()
    {
        FlushTrackingWriter writer = new FlushTrackingWriter();
        LogSink sink = TextWriterLogSink.WithFormatterAndFlush(writer, TextLogFormatter.Plain());
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(writer.FlushCount, Is.EqualTo(1));
    }

    [Test]
    public void Logger_CanWriteThroughTextWriterSink()
    {
        StringWriter writer = new StringWriter();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = TextWriterLogSink.Create(writer);

        logger.AddSink(sink);
        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(writer.ToString(), Does.Contain("Processed 3"));
        Assert.That(writer.ToString(), Does.Contain("Count=3"));
    }
}

public class FlushTrackingWriter : StringWriter
{
    public int FlushCount;

    public override void Flush()
    {
        FlushCount = FlushCount + 1;
        base.Flush();
    }
}
