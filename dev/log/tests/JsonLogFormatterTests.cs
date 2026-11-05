using System.Text.Json;
using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class JsonLogFormatterTests
{
    [Test]
    public void Compact_FormatsEventAsSingleJsonObjectLine()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 22, 0, 0, TimeSpan.Zero);
        LogEvent logEvent = LogEvent.CreateAt(
            timestamp,
            LogLevel.Info,
            "Application.Worker",
            42,
            "Processed",
            "Processed {Count}",
            "Processed 3",
            new LogField[] { LogField.Int("Count", 3), LogField.String("TenantId", "tenant-1") },
            null);
        LogFormatter formatter = JsonLogFormatter.Compact();

        string output = formatter.FormatLine(logEvent);
        JsonDocument document = JsonDocument.Parse(output);
        JsonElement root = document.RootElement;

        Assert.That(output, Does.EndWith("\n"));
        Assert.That(output.TrimEnd('\n'), Does.Not.Contain(": "));
        Assert.That(output.TrimEnd('\n'), Does.Not.Contain(", "));
        Assert.That(root.GetProperty("ts").GetString(), Is.EqualTo("2026-08-02T22:00:00.0000000Z"));
        Assert.That(root.GetProperty("level").GetString(), Is.EqualTo("Info"));
        Assert.That(root.GetProperty("logger").GetString(), Is.EqualTo("Application.Worker"));
        Assert.That(root.GetProperty("message").GetString(), Is.EqualTo("Processed 3"));
        Assert.That(root.GetProperty("template").GetString(), Is.EqualTo("Processed {Count}"));
        Assert.That(root.GetProperty("eventId").GetInt32(), Is.EqualTo(42));
        Assert.That(root.GetProperty("eventName").GetString(), Is.EqualTo("Processed"));
        Assert.That(root.GetProperty("fields").GetProperty("Count").GetInt64(), Is.EqualTo(3));
        Assert.That(root.GetProperty("fields").GetProperty("TenantId").GetString(), Is.EqualTo("tenant-1"));
        Assert.That(root.GetProperty("exception").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public void Compact_EscapesTextWithoutAddingEmbeddedLineBreaks()
    {
        LogEvent logEvent = LogEvent.Create(
            LogLevel.Warn,
            "Application",
            "Line",
            "Hello \"Ada\"\nNext",
            new LogField[] { LogField.String("Path", "c:\\temp\\file.txt") });
        LogFormatter formatter = JsonLogFormatter.Compact();

        string output = formatter.FormatLine(logEvent);
        JsonDocument document = JsonDocument.Parse(output);
        string jsonWithoutTrailingNewLine = output.Substring(0, output.Length - 1);

        Assert.That(jsonWithoutTrailingNewLine, Does.Not.Contain("\n"));
        Assert.That(document.RootElement.GetProperty("message").GetString(), Is.EqualTo("Hello \"Ada\"\nNext"));
        Assert.That(document.RootElement.GetProperty("fields").GetProperty("Path").GetString(), Is.EqualTo("c:\\temp\\file.txt"));
    }

    [Test]
    public void Compact_PreservesStructuredFieldValues()
    {
        Dictionary<string, LogValue> dictionary = new Dictionary<string, LogValue>();
        dictionary["Name"] = LogValue.String("Ada");
        dictionary["Active"] = LogValue.Bool(true);
        dictionary["Score"] = LogValue.FloatingPoint(2.5d);
        LogEvent logEvent = LogEvent.Create(
            LogLevel.Info,
            "Application",
            "message",
            "message",
            new LogField[]
            {
                LogField.Array("Items", new LogValue[] { LogValue.Integer(1), LogValue.Null(), LogValue.String("two") }),
                LogField.Dictionary("User", dictionary),
                LogField.Duration("Elapsed", TimeSpan.FromSeconds(3))
            });

        string output = JsonLogFormatter.FormatCompact(logEvent);
        JsonElement fields = JsonDocument.Parse(output).RootElement.GetProperty("fields");

        Assert.That(fields.GetProperty("Items")[0].GetInt64(), Is.EqualTo(1));
        Assert.That(fields.GetProperty("Items")[1].ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(fields.GetProperty("Items")[2].GetString(), Is.EqualTo("two"));
        Assert.That(fields.GetProperty("User").GetProperty("Name").GetString(), Is.EqualTo("Ada"));
        Assert.That(fields.GetProperty("User").GetProperty("Active").GetBoolean(), Is.True);
        Assert.That(fields.GetProperty("User").GetProperty("Score").GetDouble(), Is.EqualTo(2.5d));
        Assert.That(fields.GetProperty("Elapsed").GetString(), Is.EqualTo("00:00:03"));
    }

    [Test]
    public void Compact_FormatsExceptionDetails()
    {
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        LogEvent logEvent = LogEvent.CreateAt(
            DateTimeOffset.UnixEpoch,
            LogLevel.Error,
            "Application",
            0,
            "",
            "failed",
            "failed",
            new LogField[0],
            exceptionValue);

        string output = JsonLogFormatter.FormatCompact(logEvent);
        JsonElement exception = JsonDocument.Parse(output).RootElement.GetProperty("exception");

        Assert.That(exception.GetProperty("type").GetString(), Is.EqualTo(typeof(InvalidOperationException).FullName));
        Assert.That(exception.GetProperty("message").GetString(), Is.EqualTo("failed"));
        Assert.That(exception.GetProperty("text").GetString(), Does.Contain("InvalidOperationException"));
    }

    [Test]
    public void FormatterDispatch_UsesBuiltInCompactJsonFormatter()
    {
        LogFormatter formatter = new LogFormatter();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        string output = formatter.DispatchLine("ndjson", logEvent);
        JsonDocument document = JsonDocument.Parse(output);

        Assert.That(formatter.CanFormat("compact-json"), Is.True);
        Assert.That(output, Does.EndWith("\n"));
        Assert.That(document.RootElement.GetProperty("message").GetString(), Is.EqualTo("rendered"));
    }

    [Test]
    public void Full_FormatsReadableJsonWithGroupedEventData()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 23, 0, 0, TimeSpan.Zero);
        LogEvent logEvent = new LogEvent(
            timestamp,
            LogLevel.Error,
            "Application.Worker",
            7,
            "Failed",
            "Failed {Operation}",
            "Failed sync",
            new LogField[] { LogField.String("Operation", "sync") },
            null,
            "trace-1",
            "span-1",
            "parent-1",
            11,
            22,
            "machine-a",
            "app-a",
            "Worker.cs",
            "Run",
            123);
        LogFormatter formatter = JsonLogFormatter.Full();

        string output = formatter.Format(logEvent);
        JsonElement root = JsonDocument.Parse(output).RootElement;

        Assert.That(output, Does.Contain("\n  \"timestamp\": "));
        Assert.That(output, Does.Contain("\n  \"message\": {"));
        Assert.That(root.GetProperty("timestamp").GetString(), Is.EqualTo("2026-08-02T23:00:00.0000000Z"));
        Assert.That(root.GetProperty("level").GetString(), Is.EqualTo("Error"));
        Assert.That(root.GetProperty("loggerName").GetString(), Is.EqualTo("Application.Worker"));
        Assert.That(root.GetProperty("event").GetProperty("id").GetInt32(), Is.EqualTo(7));
        Assert.That(root.GetProperty("event").GetProperty("name").GetString(), Is.EqualTo("Failed"));
        Assert.That(root.GetProperty("message").GetProperty("template").GetString(), Is.EqualTo("Failed {Operation}"));
        Assert.That(root.GetProperty("message").GetProperty("rendered").GetString(), Is.EqualTo("Failed sync"));
        Assert.That(root.GetProperty("activity").GetProperty("traceId").GetString(), Is.EqualTo("trace-1"));
        Assert.That(root.GetProperty("runtime").GetProperty("threadId").GetInt32(), Is.EqualTo(11));
        Assert.That(root.GetProperty("runtime").GetProperty("machineName").GetString(), Is.EqualTo("machine-a"));
        Assert.That(root.GetProperty("source").GetProperty("file").GetString(), Is.EqualTo("Worker.cs"));
        Assert.That(root.GetProperty("source").GetProperty("lineNumber").GetInt32(), Is.EqualTo(123));
    }

    [Test]
    public void Full_IncludesFieldValueMetadata()
    {
        LogEvent logEvent = LogEvent.Create(
            LogLevel.Info,
            "Application",
            "message",
            "message",
            new LogField[]
            {
                LogField.String("TenantId", "tenant-1"),
                LogField.Array("Items", new LogValue[] { LogValue.Integer(1), LogValue.String("two") })
            });

        string output = JsonLogFormatter.FormatFull(logEvent);
        JsonElement fields = JsonDocument.Parse(output).RootElement.GetProperty("fields");
        JsonElement tenant = fields[0];
        JsonElement items = fields[1];

        Assert.That(tenant.GetProperty("name").GetString(), Is.EqualTo("TenantId"));
        Assert.That(tenant.GetProperty("kind").GetString(), Is.EqualTo("String"));
        Assert.That(tenant.GetProperty("value").GetString(), Is.EqualTo("tenant-1"));
        Assert.That(tenant.GetProperty("isTruncated").GetBoolean(), Is.False);
        Assert.That(tenant.GetProperty("originalLength").GetInt32(), Is.EqualTo(8));
        Assert.That(tenant.GetProperty("capturedLength").GetInt32(), Is.EqualTo(8));
        Assert.That(tenant.GetProperty("maximumLength").GetInt32(), Is.EqualTo(LogValue.DefaultMaximumStringLength));
        Assert.That(items.GetProperty("kind").GetString(), Is.EqualTo("Array"));
        Assert.That(items.GetProperty("value")[0].GetInt64(), Is.EqualTo(1));
        Assert.That(items.GetProperty("value")[1].GetString(), Is.EqualTo("two"));
    }

    [Test]
    public void Full_IncludesInvalidFieldCountAndException()
    {
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        LogEvent logEvent = LogEvent.CreateAt(
            DateTimeOffset.UnixEpoch,
            LogLevel.Error,
            "Application",
            0,
            "",
            "failed",
            "failed",
            new LogField[] { new LogField("bad name", LogValue.String("value")) },
            exceptionValue);

        string output = JsonLogFormatter.FormatFull(logEvent);
        JsonElement root = JsonDocument.Parse(output).RootElement;

        Assert.That(root.GetProperty("fields").GetArrayLength(), Is.EqualTo(0));
        Assert.That(root.GetProperty("invalidFieldCount").GetInt32(), Is.EqualTo(1));
        Assert.That(root.GetProperty("exception").GetProperty("message").GetString(), Is.EqualTo("failed"));
    }

    [Test]
    public void FormatterDispatch_UsesBuiltInFullJsonFormatter()
    {
        LogFormatter formatter = new LogFormatter();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        string output = formatter.Dispatch("json", logEvent);
        JsonDocument document = JsonDocument.Parse(output);

        Assert.That(formatter.CanFormat("json"), Is.True);
        Assert.That(output, Does.Not.EndWith("\n"));
        Assert.That(document.RootElement.GetProperty("message").GetProperty("rendered").GetString(), Is.EqualTo("rendered"));
    }
}
