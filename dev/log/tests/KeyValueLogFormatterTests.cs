using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class KeyValueLogFormatterTests
{
    [Test]
    public void KeyValue_FormatsCoreEventFieldsAndStructuredFields()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 3, 0, 0, 0, TimeSpan.Zero);
        LogEvent logEvent = new LogEvent(
            timestamp,
            LogLevel.Info,
            "Application.Worker",
            42,
            "Processed",
            "Processed {Count}",
            "Processed 3",
            new LogField[] { LogField.Int("Count", 3), LogField.String("TenantId", "tenant-1") },
            null,
            "",
            "",
            "",
            11,
            22,
            "machine-a",
            "app-a",
            "",
            "",
            0);

        string output = KeyValueLogFormatter.FormatKeyValue(logEvent);

        Assert.That(output, Is.EqualTo("timestamp=2026-08-03T00:00:00.0000000Z level=Info logger=Application.Worker eventId=42 eventName=Processed messageTemplate=\"Processed {Count}\" message=\"Processed 3\" threadId=11 processId=22 machineName=machine-a applicationName=app-a Count=3 TenantId=tenant-1"));
    }

    [Test]
    public void KeyValue_QuotesAndEscapesSpecialText()
    {
        LogEvent logEvent = LogEvent.Create(
            LogLevel.Warn,
            "Application",
            "message",
            "Hello \"Ada\"\nPath c:\\temp",
            new LogField[] { LogField.String("Note", "needs quotes") });

        string output = KeyValueLogFormatter.FormatKeyValue(logEvent);

        Assert.That(output, Does.Contain("message=\"Hello \\\"Ada\\\"\\nPath c:\\\\temp\""));
        Assert.That(output, Does.Contain("Note=\"needs quotes\""));
        Assert.That(output, Does.Not.Contain("\n"));
    }

    [Test]
    public void KeyValue_RendersStructuredValues()
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
                LogField.Bool("Accepted", true)
            });

        string output = KeyValueLogFormatter.FormatKeyValue(logEvent);

        Assert.That(output, Does.Contain("Items=\"[1, null, two]\""));
        Assert.That(output, Does.Contain("User=\"{Active=true, Score=2.5}\""));
        Assert.That(output, Does.Contain("Accepted=true"));
    }

    [Test]
    public void KeyValue_IncludesOptionalMetadataAndException()
    {
        InvalidOperationException exceptionValue = new InvalidOperationException("failed hard");
        LogEvent logEvent = new LogEvent(
            DateTimeOffset.UnixEpoch,
            LogLevel.Error,
            "Application",
            0,
            "",
            "failed",
            "failed",
            new LogField[0],
            exceptionValue,
            "trace-1",
            "span-1",
            "parent-1",
            1,
            2,
            "",
            "",
            "Worker.cs",
            "Run",
            123);

        string output = KeyValueLogFormatter.FormatKeyValue(logEvent);

        Assert.That(output, Does.Contain("traceId=trace-1"));
        Assert.That(output, Does.Contain("spanId=span-1"));
        Assert.That(output, Does.Contain("parentSpanId=parent-1"));
        Assert.That(output, Does.Contain("sourceFile=Worker.cs"));
        Assert.That(output, Does.Contain("sourceMemberName=Run"));
        Assert.That(output, Does.Contain("sourceLineNumber=123"));
        Assert.That(output, Does.Contain("exceptionType=InvalidOperationException"));
        Assert.That(output, Does.Contain("exceptionMessage=\"failed hard\""));
        Assert.That(output, Does.Contain("exception="));
    }

    [Test]
    public void KeyValue_FormatLine_AppendsConfiguredNewLine()
    {
        LogFormatter formatter = KeyValueLogFormatter.KeyValue();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        formatter.NewLine = "\n";
        string output = formatter.FormatLine(logEvent);

        Assert.That(output, Does.EndWith("\n"));
        Assert.That(output, Does.Contain("message=rendered"));
    }

    [Test]
    public void FormatterDispatch_UsesBuiltInKeyValueFormatter()
    {
        LogFormatter formatter = new LogFormatter();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        string output = formatter.Dispatch("kv", logEvent);

        Assert.That(formatter.CanFormat("key-value"), Is.True);
        Assert.That(output, Does.Contain("level=Info"));
        Assert.That(output, Does.Contain("message=rendered"));
    }
}
