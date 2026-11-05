using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class ConsoleLogSinkTests
{
    [Test]
    public void WithWriters_ReturnsConsoleSink()
    {
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();

        LogSink sink = ConsoleLogSink.WithWriters(output, error);

        Assert.That(sink.Name, Is.EqualTo(ConsoleLogSink.DefaultName));
        Assert.That(sink.IsConsoleSink, Is.True);
        Assert.That(ConsoleLogSink.IsConsole(sink), Is.True);
        Assert.That(sink.ConsoleOutWriter, Is.SameAs(output));
        Assert.That(sink.ConsoleErrorWriter, Is.SameAs(error));
        Assert.That(sink.ConsoleErrorMinimumLevel, Is.EqualTo(LogLevel.Error));
    }

    [Test]
    public void Write_RoutesInfoAndWarnToOutputAndErrorToErrorWriter()
    {
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();
        LogSink sink = ConsoleLogSink.WithWriters(output, error);

        LogResult infoResult = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]));
        LogResult warnResult = sink.Write(LogEvent.Create(LogLevel.Warn, "Application", "warn", "warn", new LogField[0]));
        LogResult errorResult = sink.Write(LogEvent.Create(LogLevel.Error, "Application", "error", "error", new LogField[0]));

        Assert.That(infoResult.IsSuccess, Is.True);
        Assert.That(warnResult.IsSuccess, Is.True);
        Assert.That(errorResult.IsSuccess, Is.True);
        Assert.That(output.ToString(), Does.Contain("[Info] Application: info"));
        Assert.That(output.ToString(), Does.Contain("[Warn] Application: warn"));
        Assert.That(output.ToString(), Does.Not.Contain("[Error]"));
        Assert.That(error.ToString(), Does.Contain("[Error] Application: error"));
    }

    [Test]
    public void SetErrorMinimumLevel_CanRouteWarningsToErrorWriter()
    {
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();
        LogSink sink = ConsoleLogSink.WithWriters(output, error);

        ConsoleLogSink.SetErrorMinimumLevel(sink, LogLevel.Warn);
        LogResult result = sink.Write(LogEvent.Create(LogLevel.Warn, "Application", "warn", "warn", new LogField[0]));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(output.ToString(), Is.EqualTo(""));
        Assert.That(error.ToString(), Does.Contain("[Warn] Application: warn"));
    }

    [Test]
    public void Write_UsesConfiguredConsoleFormatter()
    {
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();
        LogSink sink = ConsoleLogSink.WithFormatter(output, error, ConsoleTextLogFormatter.AnsiColor());

        LogResult result = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(output.ToString(), Does.StartWith(ConsoleTextLogFormatter.InfoColor));
        Assert.That(output.ToString(), Does.Contain("[Info] Application: rendered"));
        Assert.That(output.ToString(), Does.EndWith(ConsoleTextLogFormatter.Reset + Environment.NewLine));
    }

    [Test]
    public void Write_ReturnsFailureWhenSelectedWriterIsMissing()
    {
        StringWriter error = new StringWriter();
        LogSink sink = ConsoleLogSink.WithWriters(null, error);

        LogResult result = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.console.writer.missing"));
        Assert.That(error.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void Write_FlushesSelectedWriterWhenConfigured()
    {
        FlushTrackingWriter output = new FlushTrackingWriter();
        FlushTrackingWriter error = new FlushTrackingWriter();
        LogSink sink = ConsoleLogSink.WithWriters(output, error);

        ConsoleLogSink.SetFlushOnWrite(sink, true);
        LogResult result = sink.Write(LogEvent.Create(LogLevel.Error, "Application", "error", "error", new LogField[0]));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(output.FlushCount, Is.EqualTo(0));
        Assert.That(error.FlushCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_HonorsThresholdsAndFiltersBeforeRouting()
    {
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();
        LogSink sink = ConsoleLogSink.WithWriters(output, error);
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
        Assert.That(output.ToString(), Is.EqualTo(""));
        Assert.That(error.ToString(), Does.Contain("present"));
        Assert.That(error.ToString(), Does.Not.Contain("missing"));
    }

    [Test]
    public void Logger_CanWriteThroughConsoleSink()
    {
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = ConsoleLogSink.WithWriters(output, error);

        logger.AddSink(sink);
        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(output.ToString(), Does.Contain("Processed 3"));
        Assert.That(output.ToString(), Does.Contain("Count=3"));
        Assert.That(error.ToString(), Is.EqualTo(""));
    }
}
