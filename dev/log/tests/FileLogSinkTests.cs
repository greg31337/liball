using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class FileLogSinkTests
{
    [Test]
    public void Append_CreatesDirectoryAndWritesFormattedEvent()
    {
        string filePath = NewTempFilePath();
        LogSink sink = FileLogSink.Append(filePath);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        LogResult result = sink.Write(logEvent);
        FileLogSink.Close(sink);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(File.Exists(filePath), Is.True);
        Assert.That(File.ReadAllText(filePath), Does.Contain("[Info] Application: rendered"));
        Assert.That(sink.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
        Assert.That(sink.FileIsOpen, Is.False);
    }

    [Test]
    public void Append_PreservesExistingFileContent()
    {
        string filePath = NewTempFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, "existing\n");
        LogSink sink = FileLogSink.Append(filePath);

        LogResult result = sink.Write(LogEvent.Create(LogLevel.Warn, "Application", "message", "rendered", new LogField[0]));
        FileLogSink.Close(sink);
        string output = File.ReadAllText(filePath);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(output, Does.StartWith("existing\n"));
        Assert.That(output, Does.Contain("[Warn] Application: rendered"));
    }

    [Test]
    public void Overwrite_ReplacesExistingContentOnFirstOpenOnly()
    {
        string filePath = NewTempFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, "old\n");
        LogSink sink = FileLogSink.Overwrite(filePath);

        sink.Write(LogEvent.Create(LogLevel.Info, "Application", "first", "first", new LogField[0]));
        FileLogSink.Close(sink);
        sink.Write(LogEvent.Create(LogLevel.Info, "Application", "second", "second", new LogField[0]));
        FileLogSink.Close(sink);
        string output = File.ReadAllText(filePath);

        Assert.That(output, Does.Not.Contain("old"));
        Assert.That(output, Does.Contain("[Info] Application: first"));
        Assert.That(output, Does.Contain("[Info] Application: second"));
    }

    [Test]
    public void FlushOnWrite_MakesContentVisibleBeforeClose()
    {
        string filePath = NewTempFilePath();
        LogSink sink = FileLogSink.Append(filePath);

        FileLogSink.SetFlushOnWrite(sink, true);
        LogResult result = sink.Write(LogEvent.Create(LogLevel.Error, "Application", "message", "rendered", new LogField[0]));
        string output = File.ReadAllText(filePath);
        FileLogSink.Close(sink);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(output, Does.Contain("[Error] Application: rendered"));
    }

    [Test]
    public void Write_ReturnsFailureWhenPathIsMissing()
    {
        LogSink sink = FileLogSink.Append("");

        LogResult result = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.file.path.missing"));
    }

    [Test]
    public void Write_HonorsThresholdsAndFiltersBeforeOpeningFile()
    {
        string filePath = NewTempFilePath();
        LogSink sink = FileLogSink.Append(filePath);
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent missingEvent = LogEvent.Create(LogLevel.Error, "Application", "missing", "missing", new LogField[0]);
        LogEvent presentEvent = LogEvent.Create(LogLevel.Error, "Application", "present", "present", new LogField[] { LogField.String("TenantId", "tenant-1") });

        sink.SetMinimumLevel(LogLevel.Error);
        sink.AddFilter(LogFilter.FieldExists("TenantId"));
        LogResult infoResult = sink.Write(infoEvent);
        LogResult missingResult = sink.Write(missingEvent);

        Assert.That(infoResult.IsDropped, Is.True);
        Assert.That(missingResult.IsDropped, Is.True);
        Assert.That(File.Exists(filePath), Is.False);

        LogResult presentResult = sink.Write(presentEvent);
        FileLogSink.Close(sink);

        Assert.That(presentResult.IsSuccess, Is.True);
        Assert.That(File.ReadAllText(filePath), Does.Contain("present"));
    }

    [Test]
    public void Write_UsesConfiguredFormatter()
    {
        string filePath = NewTempFilePath();
        LogFormatter formatter = LogFormatter.Custom("text", logEvent => "file:" + logEvent.RenderedMessage);
        LogSink sink = FileLogSink.WithFormatter(filePath, formatter, true);

        formatter.AppendNewLine = true;
        formatter.NewLine = "\n";
        LogResult result = sink.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]));
        FileLogSink.Close(sink);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(File.ReadAllText(filePath), Is.EqualTo("file:rendered\n"));
    }

    [Test]
    public void Logger_CanWriteThroughFileSink()
    {
        string filePath = NewTempFilePath();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = FileLogSink.Append(filePath);

        logger.AddSink(sink);
        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });
        FileLogSink.Close(sink);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(File.ReadAllText(filePath), Does.Contain("Processed 3"));
        Assert.That(File.ReadAllText(filePath), Does.Contain("Count=3"));
    }

    static string NewTempFilePath()
    {
        string directory = Path.Combine(Path.GetTempPath(), "log-tests", Guid.NewGuid().ToString("N"), "nested");
        return Path.Combine(directory, "events.log");
    }
}
