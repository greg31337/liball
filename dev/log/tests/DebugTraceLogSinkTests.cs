using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class DebugTraceLogSinkTests
{
    [Test]
    public void DebugCreate_ReturnsDebugSink()
    {
        LogSink sink = DebugLogSink.Create();

        Assert.That(sink.Name, Is.EqualTo(DebugLogSink.DefaultName));
        Assert.That(sink.IsDebugSink, Is.True);
        Assert.That(DebugLogSink.IsDebug(sink), Is.True);
        Assert.That(sink.SinkFormatter.Name, Is.EqualTo(LogFormatter.TextName));
    }

    [Test]
    public void TraceCreate_ReturnsTraceSink()
    {
        LogSink sink = TraceLogSink.Create();

        Assert.That(sink.Name, Is.EqualTo(TraceLogSink.DefaultName));
        Assert.That(sink.IsTraceSink, Is.True);
        Assert.That(TraceLogSink.IsTrace(sink), Is.True);
        Assert.That(sink.SinkFormatter.Name, Is.EqualTo(LogFormatter.TextName));
    }

    [Test]
    public void DebugWrite_FormatsEventToConfiguredAction()
    {
        System.Text.StringBuilder output = new System.Text.StringBuilder();
        LogSink sink = DebugLogSink.WithAction(text => output.Append(text));
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(output.ToString(), Does.Contain("[Info] Application: rendered"));
        Assert.That(output.ToString(), Does.EndWith(Environment.NewLine));
        Assert.That(sink.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void TraceWrite_FormatsEventToConfiguredAction()
    {
        System.Text.StringBuilder output = new System.Text.StringBuilder();
        LogSink sink = TraceLogSink.WithAction(text => output.Append(text));
        LogEvent logEvent = LogEvent.Create(LogLevel.Warn, "Application", "message", "rendered", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(output.ToString(), Does.Contain("[Warn] Application: rendered"));
        Assert.That(output.ToString(), Does.EndWith(Environment.NewLine));
    }

    [Test]
    public void DebugWrite_UsesConfiguredFormatter()
    {
        System.Text.StringBuilder output = new System.Text.StringBuilder();
        LogFormatter formatter = LogFormatter.Custom("text", logEvent => "debug:" + logEvent.RenderedMessage);
        LogSink sink = DebugLogSink.WithFormatter(text => output.Append(text), formatter);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        formatter.AppendNewLine = true;
        formatter.NewLine = "\n";
        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(output.ToString(), Is.EqualTo("debug:rendered\n"));
    }

    [Test]
    public void TraceWrite_HonorsThresholdsAndFilters()
    {
        System.Text.StringBuilder output = new System.Text.StringBuilder();
        LogSink sink = TraceLogSink.WithAction(text => output.Append(text));
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
        Assert.That(output.ToString(), Does.Contain("present"));
        Assert.That(output.ToString(), Does.Not.Contain("info"));
        Assert.That(output.ToString(), Does.Not.Contain("missing"));
    }

    [Test]
    public void Logger_CanWriteThroughDebugAndTraceSinks()
    {
        System.Text.StringBuilder debugOutput = new System.Text.StringBuilder();
        System.Text.StringBuilder traceOutput = new System.Text.StringBuilder();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink debugSink = DebugLogSink.WithAction(text => debugOutput.Append(text));
        LogSink traceSink = TraceLogSink.WithAction(text => traceOutput.Append(text));

        logger.AddSink(debugSink);
        logger.AddSink(traceSink);
        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(debugOutput.ToString(), Does.Contain("Processed 3"));
        Assert.That(debugOutput.ToString(), Does.Contain("Count=3"));
        Assert.That(traceOutput.ToString(), Does.Contain("Processed 3"));
        Assert.That(traceOutput.ToString(), Does.Contain("Count=3"));
    }
}
