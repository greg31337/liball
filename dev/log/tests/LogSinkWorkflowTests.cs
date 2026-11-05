using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogSinkWorkflowTests
{
    [Test]
    public void Logger_FansOutToConsoleTextWriterMemoryFileAndDelegateSinks()
    {
        StringWriter consoleOutput = new StringWriter();
        StringWriter consoleError = new StringWriter();
        StringWriter textWriter = new StringWriter();
        string filePath = NewTempFilePath("fanout");
        List<LogEvent> delegateEvents = new List<LogEvent>();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink consoleSink = ConsoleLogSink.WithWriters(consoleOutput, consoleError);
        LogSink textWriterSink = TextWriterLogSink.Create(textWriter);
        LogSink memorySink = MemoryLogSink.Create();
        LogSink fileSink = FileLogSink.Append(filePath);
        LogSink delegateSink = DelegateLogSink.Create(logEvent =>
        {
            delegateEvents.Add(logEvent);
            return LogResult.Success();
        });

        logger.AddSink(consoleSink);
        logger.AddSink(textWriterSink);
        logger.AddSink(memorySink);
        logger.AddSink(fileSink);
        logger.AddSink(delegateSink);

        LogResult infoResult = logger.InfoWithValues("Processed {Count}", new object[] { 3 });
        LogResult errorResult = logger.ErrorWithValues("Failed {Operation}", new object[] { "sync" });
        FileLogSink.Close(fileSink);
        string fileOutput = File.ReadAllText(filePath);

        Assert.That(infoResult.IsSuccess, Is.True);
        Assert.That(errorResult.IsSuccess, Is.True);
        Assert.That(consoleOutput.ToString(), Does.Contain("[Info]: Processed 3"));
        Assert.That(consoleOutput.ToString(), Does.Not.Contain("[Error]"));
        Assert.That(consoleError.ToString(), Does.Contain("[Error]: Failed sync"));
        Assert.That(textWriter.ToString(), Does.Contain("Processed 3"));
        Assert.That(textWriter.ToString(), Does.Contain("Failed sync"));
        Assert.That(fileOutput, Does.Contain("Processed 3"));
        Assert.That(fileOutput, Does.Contain("Failed sync"));
        Assert.That(MemoryLogSink.Count(memorySink), Is.EqualTo(2));
        Assert.That(MemoryLogSink.Events(memorySink)[0].Fields[0].Name, Is.EqualTo("Count"));
        Assert.That(MemoryLogSink.Events(memorySink)[1].Fields[0].Value.TextValue, Is.EqualTo("sync"));
        Assert.That(delegateEvents.Count, Is.EqualTo(2));
        Assert.That(delegateEvents[0].RenderedMessage, Is.EqualTo("Processed 3"));
        Assert.That(delegateEvents[1].RenderedMessage, Is.EqualTo("Failed sync"));
    }

    [Test]
    public void Logger_RollsFileSinkAcrossTimestampPeriods()
    {
        DateTimeOffset firstTimestamp = new DateTimeOffset(2026, 8, 2, 23, 59, 0, TimeSpan.Zero);
        DateTimeOffset secondTimestamp = new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero);
        LogClock clock = LogClock.Fixed(firstTimestamp);
        Logger logger = new Logger("", LogLevelSwitch.Create(LogLevel.Trace), false, clock);
        string filePath = NewTempFilePath("rolling");
        LogSink sink = FileLogSink.WithRollingPolicy(filePath, MessageOnlyFormatter(), true, LogRollingPolicy.ByInterval(LogRollingInterval.Day));

        logger.AddSink(sink);
        LogResult firstResult = logger.InfoWithValues("first {Count}", new object[] { 1 });
        clock.SetFixedUtcNow(secondTimestamp);
        LogResult secondResult = logger.InfoWithValues("second {Count}", new object[] { 2 });
        FileLogSink.Close(sink);
        string firstPath = LogRollingPolicy.BuildPath(filePath, "20260802", 0);
        string secondPath = LogRollingPolicy.BuildPath(filePath, "20260803", 0);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(File.ReadAllText(firstPath), Is.EqualTo("first 1\n"));
        Assert.That(File.ReadAllText(secondPath), Is.EqualTo("second 2\n"));
        Assert.That(sink.ActiveFilePath, Is.EqualTo(secondPath));
    }

    [Test]
    public void Logger_RollsFileSinkBySizeBeforeNextAcceptedWrite()
    {
        string filePath = NewTempFilePath("rolling-size");
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = FileLogSink.WithRollingPolicy(filePath, MessageOnlyFormatter(), true, LogRollingPolicy.BySize(6));

        FileLogSink.SetFlushOnWrite(sink, true);
        logger.AddSink(sink);
        LogResult firstResult = logger.Info("first");
        LogResult secondResult = logger.Info("second");
        FileLogSink.Close(sink);
        string secondPath = LogRollingPolicy.BuildPath(filePath, "", 1);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(File.ReadAllText(filePath), Is.EqualTo("first\n"));
        Assert.That(File.ReadAllText(secondPath), Is.EqualTo("second\n"));
        Assert.That(sink.RollingSequence, Is.EqualTo(1));
    }

    static LogFormatter MessageOnlyFormatter()
    {
        LogFormatter formatter = LogFormatter.Custom("message", logEvent => logEvent.RenderedMessage);
        formatter.AppendNewLine = true;
        formatter.NewLine = "\n";
        return formatter;
    }

    static string NewTempFilePath(string name)
    {
        string directory = Path.Combine(Path.GetTempPath(), "log-tests", Guid.NewGuid().ToString("N"), name);
        return Path.Combine(directory, "events.log");
    }
}
