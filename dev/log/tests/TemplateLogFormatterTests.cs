using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class TemplateLogFormatterTests
{
    [Test]
    public void DefaultTemplate_FormatsReadableTextLine()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 3, 1, 0, 0, TimeSpan.Zero);
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        LogEvent logEvent = LogEvent.CreateAt(
            timestamp,
            LogLevel.Error,
            "Application.Worker",
            0,
            "",
            "Failed {Operation}",
            "Failed sync",
            new LogField[] { LogField.String("Operation", "sync") },
            exceptionValue);

        string output = TemplateLogFormatter.FormatDefaultTemplate(logEvent);

        Assert.That(output, Is.EqualTo("2026-08-03T01:00:00.0000000Z [Error] Application.Worker: Failed sync {Operation=sync} exception=InvalidOperationException: failed"));
    }

    [Test]
    public void FormatTemplate_UsesKnownPlaceholdersAndFormatHints()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 3, 1, 30, 0, TimeSpan.Zero);
        LogEvent logEvent = LogEvent.CreateAt(
            timestamp,
            LogLevel.Info,
            "Application",
            7,
            "Processed",
            "message",
            "rendered",
            new LogField[0],
            null);

        string output = TemplateLogFormatter.FormatTemplate(logEvent, "{Timestamp:yyyyMMdd}|{Level}|{Logger}|{EventId:000}|{EventName}|{Message}");

        Assert.That(output, Is.EqualTo("20260803|Info|Application|007|Processed|rendered"));
    }

    [Test]
    public void FormatTemplate_ResolvesStructuredFieldPlaceholdersCaseInsensitively()
    {
        LogEvent logEvent = LogEvent.Create(
            LogLevel.Info,
            "Application",
            "message",
            "rendered",
            new LogField[]
            {
                LogField.String("TenantId", "tenant-1"),
                LogField.Array("Items", new LogValue[] { LogValue.Integer(1), LogValue.String("two") })
            });

        string output = TemplateLogFormatter.FormatTemplate(logEvent, "{tenantid}:{Items}:{Missing}");

        Assert.That(output, Is.EqualTo("tenant-1:[1, two]:"));
    }

    [Test]
    public void FormatTemplate_HonorsEscapedBracesAndNewLinePlaceholder()
    {
        LogEvent logEvent = LogEvent.Create(LogLevel.Warn, "Application", "message", "rendered", new LogField[0]);

        string output = TemplateLogFormatter.FormatTemplate(logEvent, "{{prefix}}{NewLine}{Level}");

        Assert.That(output, Is.EqualTo("{prefix}" + Environment.NewLine + "Warn"));
    }

    [Test]
    public void WithTemplate_FormatLine_AppendsConfiguredNewLine()
    {
        LogFormatter formatter = TemplateLogFormatter.WithTemplate("{Level}:{Message}");
        LogEvent logEvent = LogEvent.Create(LogLevel.Debug, "Application", "message", "rendered", new LogField[0]);

        formatter.NewLine = "\n";
        string output = formatter.FormatLine(logEvent);

        Assert.That(output, Is.EqualTo("Debug:rendered\n"));
    }

    [Test]
    public void FormatterDispatch_UsesBuiltInTemplateFormatter()
    {
        LogFormatter formatter = new LogFormatter();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        string output = formatter.Dispatch("template", logEvent);

        Assert.That(formatter.CanFormat("template"), Is.True);
        Assert.That(output, Does.EndWith("[Info] Application: rendered"));
    }
}
