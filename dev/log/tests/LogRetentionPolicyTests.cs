using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogRetentionPolicyTests
{
    [Test]
    public void PolicyFactories_NormalizeNegativeLimits()
    {
        LogRetentionPolicy countPolicy = LogRetentionPolicy.ByCount(-1);
        LogRetentionPolicy agePolicy = LogRetentionPolicy.ByAge(TimeSpan.FromSeconds(-1));

        Assert.That(countPolicy.MaximumFileCount, Is.EqualTo(0));
        Assert.That(agePolicy.MaximumFileAge, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void IsCandidate_MatchesBaseAndRolledFilesOnly()
    {
        string directory = NewTempDirectory();
        string basePath = Path.Combine(directory, "events.log");

        Assert.That(LogRetentionPolicy.IsCandidate(basePath, Path.Combine(directory, "events.log")), Is.True);
        Assert.That(LogRetentionPolicy.IsCandidate(basePath, Path.Combine(directory, "events-20260803.log")), Is.True);
        Assert.That(LogRetentionPolicy.IsCandidate(basePath, Path.Combine(directory, "events-20260803-1.log")), Is.True);
        Assert.That(LogRetentionPolicy.IsCandidate(basePath, Path.Combine(directory, "other.log")), Is.False);
        Assert.That(LogRetentionPolicy.IsCandidate(basePath, Path.Combine(directory, "events.txt")), Is.False);
    }

    [Test]
    public void FileSink_DeletesOldestFilesBeyondMaximumCount()
    {
        string directory = NewTempDirectory();
        string filePath = Path.Combine(directory, "events.log");
        string oldPath = Path.Combine(directory, "events-20260801.log");
        string newerPath = Path.Combine(directory, "events-20260802.log");

        Directory.CreateDirectory(directory);
        File.WriteAllText(oldPath, "old\n");
        File.WriteAllText(newerPath, "newer\n");
        File.SetLastWriteTimeUtc(oldPath, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newerPath, new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc));

        LogSink sink = FileLogSink.Append(filePath);
        FileLogSink.SetRetentionPolicy(sink, LogRetentionPolicy.ByCount(2));
        sink.Write(LogEvent.CreateAt(new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero), LogLevel.Info, "Application", 0, "", "current", "current", new LogField[0], null));
        FileLogSink.Close(sink);

        Assert.That(File.Exists(oldPath), Is.False);
        Assert.That(File.Exists(newerPath), Is.True);
        Assert.That(File.Exists(filePath), Is.True);
    }

    [Test]
    public void FileSink_DeletesFilesOlderThanMaximumAge()
    {
        string directory = NewTempDirectory();
        string filePath = Path.Combine(directory, "events.log");
        string oldPath = Path.Combine(directory, "events-20260701.log");
        string recentPath = Path.Combine(directory, "events-20260802.log");

        Directory.CreateDirectory(directory);
        File.WriteAllText(oldPath, "old\n");
        File.WriteAllText(recentPath, "recent\n");
        File.SetLastWriteTimeUtc(oldPath, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(recentPath, new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc));

        LogSink sink = FileLogSink.Append(filePath);
        FileLogSink.SetRetentionPolicy(sink, LogRetentionPolicy.ByAge(TimeSpan.FromDays(7)));
        sink.Write(LogEvent.CreateAt(new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero), LogLevel.Info, "Application", 0, "", "current", "current", new LogField[0], null));
        FileLogSink.Close(sink);

        Assert.That(File.Exists(oldPath), Is.False);
        Assert.That(File.Exists(recentPath), Is.True);
        Assert.That(File.Exists(filePath), Is.True);
    }

    [Test]
    public void FileSink_RetentionKeepsActiveFileWhenCountLimitIsOne()
    {
        string directory = NewTempDirectory();
        string filePath = Path.Combine(directory, "events.log");
        string oldPath = Path.Combine(directory, "events-20260801.log");

        Directory.CreateDirectory(directory);
        File.WriteAllText(oldPath, "old\n");
        File.SetLastWriteTimeUtc(oldPath, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

        LogSink sink = FileLogSink.Append(filePath);
        FileLogSink.SetRetentionPolicy(sink, LogRetentionPolicy.ByCount(1));
        sink.Write(LogEvent.CreateAt(new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero), LogLevel.Info, "Application", 0, "", "current", "current", new LogField[0], null));
        FileLogSink.Close(sink);

        Assert.That(File.Exists(oldPath), Is.False);
        Assert.That(File.Exists(filePath), Is.True);
    }

    [Test]
    public void FileSink_AppliesRetentionAfterRollingToNewFile()
    {
        string filePath = NewTempFilePath();
        LogSink sink = FileLogSink.WithRollingPolicy(filePath, MessageOnlyFormatter(), true, LogRollingPolicy.ByInterval(LogRollingInterval.Day));
        LogEvent first = LogEvent.CreateAt(new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero), LogLevel.Info, "Application", 0, "", "first", "first", new LogField[0], null);
        LogEvent second = LogEvent.CreateAt(new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero), LogLevel.Info, "Application", 0, "", "second", "second", new LogField[0], null);

        FileLogSink.SetFlushOnWrite(sink, true);
        FileLogSink.SetRetentionPolicy(sink, LogRetentionPolicy.ByCount(1));
        sink.Write(first);
        sink.Write(second);
        FileLogSink.Close(sink);
        string firstPath = LogRollingPolicy.BuildPath(filePath, "20260803", 0);
        string secondPath = LogRollingPolicy.BuildPath(filePath, "20260804", 0);

        Assert.That(File.Exists(firstPath), Is.False);
        Assert.That(File.ReadAllText(secondPath), Is.EqualTo("second\n"));
    }

    [Test]
    public void ClearRetentionPolicy_DisablesCleanup()
    {
        string directory = NewTempDirectory();
        string filePath = Path.Combine(directory, "events.log");
        string oldPath = Path.Combine(directory, "events-20260801.log");

        Directory.CreateDirectory(directory);
        File.WriteAllText(oldPath, "old\n");
        File.SetLastWriteTimeUtc(oldPath, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

        LogSink sink = FileLogSink.Append(filePath);
        FileLogSink.SetRetentionPolicy(sink, LogRetentionPolicy.ByCount(1));
        FileLogSink.ClearRetentionPolicy(sink);
        sink.Write(LogEvent.CreateAt(new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero), LogLevel.Info, "Application", 0, "", "current", "current", new LogField[0], null));
        FileLogSink.Close(sink);

        Assert.That(File.Exists(oldPath), Is.True);
        Assert.That(File.Exists(filePath), Is.True);
    }

    static LogFormatter MessageOnlyFormatter()
    {
        LogFormatter formatter = LogFormatter.Custom("text", logEvent => logEvent.RenderedMessage);
        formatter.AppendNewLine = true;
        formatter.NewLine = "\n";
        return formatter;
    }

    static string NewTempDirectory()
    {
        return Path.Combine(Path.GetTempPath(), "log-tests", Guid.NewGuid().ToString("N"), "retention");
    }

    static string NewTempFilePath()
    {
        return Path.Combine(NewTempDirectory(), "events.log");
    }
}
