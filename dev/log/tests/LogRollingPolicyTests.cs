using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogRollingPolicyTests
{
    [Test]
    public void BuildPath_AddsPeriodAndSequenceBeforeExtension()
    {
        string filePath = Path.Combine("logs", "events.log");

        string periodPath = LogRollingPolicy.BuildPath(filePath, "20260803", 0);
        string sequencePath = LogRollingPolicy.BuildPath(filePath, "20260803", 2);

        Assert.That(periodPath, Is.EqualTo(Path.Combine("logs", "events-20260803.log")));
        Assert.That(sequencePath, Is.EqualTo(Path.Combine("logs", "events-20260803-2.log")));
    }

    [Test]
    public void PeriodKey_UsesUtcTimestampForIntervals()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 3, 1, 30, 0, TimeSpan.FromHours(1));

        Assert.That(LogRollingPolicy.PeriodKey(LogRollingInterval.Hour, timestamp), Is.EqualTo("2026080300"));
        Assert.That(LogRollingPolicy.PeriodKey(LogRollingInterval.Day, timestamp), Is.EqualTo("20260803"));
        Assert.That(LogRollingPolicy.PeriodKey(LogRollingInterval.Month, timestamp), Is.EqualTo("202608"));
        Assert.That(LogRollingPolicy.PeriodKey(LogRollingInterval.Year, timestamp), Is.EqualTo("2026"));
        Assert.That(LogRollingPolicy.PeriodKey(LogRollingInterval.None, timestamp), Is.EqualTo(""));
    }

    [Test]
    public void NormalizeMaximumFileSize_TreatsNegativeAsDisabled()
    {
        LogRollingPolicy policy = LogRollingPolicy.BySize(-10);

        Assert.That(policy.MaximumFileSizeBytes, Is.EqualTo(0));
    }

    [Test]
    public void FileSink_RollsByDatePeriod()
    {
        string filePath = NewTempFilePath();
        LogFormatter formatter = MessageOnlyFormatter();
        LogSink sink = FileLogSink.WithRollingPolicy(filePath, formatter, true, LogRollingPolicy.ByInterval(LogRollingInterval.Day));
        LogEvent first = LogEvent.CreateAt(new DateTimeOffset(2026, 8, 3, 23, 0, 0, TimeSpan.Zero), LogLevel.Info, "Application", 0, "", "first", "first", new LogField[0], null);
        LogEvent second = LogEvent.CreateAt(new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero), LogLevel.Info, "Application", 0, "", "second", "second", new LogField[0], null);

        LogResult firstResult = sink.Write(first);
        LogResult secondResult = sink.Write(second);
        FileLogSink.Close(sink);
        string firstPath = LogRollingPolicy.BuildPath(filePath, "20260803", 0);
        string secondPath = LogRollingPolicy.BuildPath(filePath, "20260804", 0);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(File.ReadAllText(firstPath), Is.EqualTo("first\n"));
        Assert.That(File.ReadAllText(secondPath), Is.EqualTo("second\n"));
        Assert.That(sink.ActiveFilePath, Is.EqualTo(secondPath));
    }

    [Test]
    public void FileSink_RollsBySizeBeforeNextWrite()
    {
        string filePath = NewTempFilePath();
        LogFormatter formatter = MessageOnlyFormatter();
        LogSink sink = FileLogSink.WithRollingPolicy(filePath, formatter, true, LogRollingPolicy.BySize(6));
        LogEvent first = LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]);
        LogEvent second = LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]);

        FileLogSink.SetFlushOnWrite(sink, true);
        LogResult firstResult = sink.Write(first);
        LogResult secondResult = sink.Write(second);
        FileLogSink.Close(sink);
        string secondPath = LogRollingPolicy.BuildPath(filePath, "", 1);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(File.ReadAllText(filePath), Is.EqualTo("first\n"));
        Assert.That(File.ReadAllText(secondPath), Is.EqualTo("second\n"));
        Assert.That(sink.RollingSequence, Is.EqualTo(1));
    }

    [Test]
    public void FileSink_RollsByDateAndSizeTogether()
    {
        string filePath = NewTempFilePath();
        LogFormatter formatter = MessageOnlyFormatter();
        LogSink sink = FileLogSink.WithRollingPolicy(filePath, formatter, true, LogRollingPolicy.ByIntervalAndSize(LogRollingInterval.Day, 6));
        LogEvent first = LogEvent.CreateAt(new DateTimeOffset(2026, 8, 3, 1, 0, 0, TimeSpan.Zero), LogLevel.Info, "Application", 0, "", "first", "first", new LogField[0], null);
        LogEvent second = LogEvent.CreateAt(new DateTimeOffset(2026, 8, 3, 2, 0, 0, TimeSpan.Zero), LogLevel.Info, "Application", 0, "", "second", "second", new LogField[0], null);

        FileLogSink.SetFlushOnWrite(sink, true);
        sink.Write(first);
        sink.Write(second);
        FileLogSink.Close(sink);
        string firstPath = LogRollingPolicy.BuildPath(filePath, "20260803", 0);
        string secondPath = LogRollingPolicy.BuildPath(filePath, "20260803", 1);

        Assert.That(File.ReadAllText(firstPath), Is.EqualTo("first\n"));
        Assert.That(File.ReadAllText(secondPath), Is.EqualTo("second\n"));
    }

    [Test]
    public void Logger_CanWriteThroughRollingFileSink()
    {
        string filePath = NewTempFilePath();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = FileLogSink.WithRollingPolicy(filePath, MessageOnlyFormatter(), true, LogRollingPolicy.BySize(6));

        FileLogSink.SetFlushOnWrite(sink, true);
        logger.AddSink(sink);
        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });
        FileLogSink.Close(sink);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(File.ReadAllText(filePath), Is.EqualTo("Processed 3\n"));
    }

    static LogFormatter MessageOnlyFormatter()
    {
        LogFormatter formatter = LogFormatter.Custom("text", logEvent => logEvent.RenderedMessage);
        formatter.AppendNewLine = true;
        formatter.NewLine = "\n";
        return formatter;
    }

    static string NewTempFilePath()
    {
        string directory = Path.Combine(Path.GetTempPath(), "log-tests", Guid.NewGuid().ToString("N"), "rolling");
        return Path.Combine(directory, "events.log");
    }
}
