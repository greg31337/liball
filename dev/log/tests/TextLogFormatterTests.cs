using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class TextLogFormatterTests
{
    [Test]
    public void Plain_FormatsTimestampLevelLoggerMessageAndFields()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 23, 30, 0, TimeSpan.Zero);
        LogEvent logEvent = LogEvent.CreateAt(
            timestamp,
            LogLevel.Info,
            "Application.Worker",
            0,
            "",
            "Processed {Count}",
            "Processed 3",
            new LogField[] { LogField.Int("Count", 3), LogField.String("TenantId", "tenant-1") },
            null);

        string output = TextLogFormatter.FormatPlain(logEvent);

        Assert.That(output, Is.EqualTo("2026-08-02T23:30:00.0000000Z [Info] Application.Worker: Processed 3 {Count=3, TenantId=tenant-1}"));
    }

    [Test]
    public void Plain_UsesTemplateWhenRenderedMessageIsEmpty()
    {
        LogEvent logEvent = LogEvent.Create(LogLevel.Debug, "Application", "Template {Value}", "", new LogField[0]);

        string output = TextLogFormatter.FormatPlain(logEvent);

        Assert.That(output, Does.EndWith("[Debug] Application: Template {Value}"));
    }

    [Test]
    public void Plain_SanitizesEmbeddedWhitespaceInMessageFieldsAndException()
    {
        InvalidOperationException exceptionValue = new InvalidOperationException("first\nsecond");
        LogEvent logEvent = LogEvent.CreateAt(
            DateTimeOffset.UnixEpoch,
            LogLevel.Error,
            "Application",
            0,
            "",
            "message",
            "line one\nline two",
            new LogField[] { LogField.String("Path", "c:\\temp\tfile.txt") },
            exceptionValue);

        string output = TextLogFormatter.FormatPlain(logEvent);

        Assert.That(output, Does.Not.Contain("\n"));
        Assert.That(output, Does.Contain("line one line two"));
        Assert.That(output, Does.Contain("Path=c:\\temp file.txt"));
        Assert.That(output, Does.Contain("exception=InvalidOperationException: first second"));
    }

    [Test]
    public void Plain_RendersStructuredValues()
    {
        Dictionary<string, LogValue> dictionary = new Dictionary<string, LogValue>();
        dictionary["Active"] = LogValue.Bool(true);
        dictionary["Score"] = LogValue.FloatingPoint(2.5d);
        LogEvent logEvent = LogEvent.Create(
            LogLevel.Info,
            "",
            "message",
            "message",
            new LogField[]
            {
                LogField.Array("Items", new LogValue[] { LogValue.Integer(1), LogValue.Null(), LogValue.String("two") }),
                LogField.Dictionary("User", dictionary),
                LogField.Duration("Elapsed", TimeSpan.FromSeconds(3))
            });

        string output = TextLogFormatter.FormatPlain(logEvent);

        Assert.That(output, Does.Contain("{Items=[1, null, two], User={Active=true, Score=2.5}, Elapsed=00:00:03}"));
    }

    [Test]
    public void Plain_FormatLine_AppendsConfiguredNewLine()
    {
        LogFormatter formatter = TextLogFormatter.Plain();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        formatter.NewLine = "\n";
        string output = formatter.FormatLine(logEvent);

        Assert.That(output, Does.EndWith("\n"));
        Assert.That(output.Substring(0, output.Length - 1), Does.EndWith("[Info] Application: rendered"));
    }

    [Test]
    public void FormatterDispatch_UsesBuiltInPlainTextFormatter()
    {
        LogFormatter formatter = LogFormatter.Named("text");
        LogEvent logEvent = LogEvent.Create(LogLevel.Warn, "Application", "message", "rendered", new LogField[0]);

        string output = formatter.Format(logEvent);

        Assert.That(formatter.CanFormat("text"), Is.True);
        Assert.That(output, Does.EndWith("[Warn] Application: rendered"));
    }
}
