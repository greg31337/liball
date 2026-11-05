using System.Text.Json;
using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogFormatterWorkflowTests
{
    [Test]
    public void AllBuiltInFormatters_RenderSameCoreEvent()
    {
        LogEvent logEvent = RichEvent();

        string compactJson = JsonLogFormatter.Compact().FormatLine(logEvent);
        string fullJson = JsonLogFormatter.Full().Format(logEvent);
        string text = TextLogFormatter.Plain().Format(logEvent);
        string console = ConsoleTextLogFormatter.Plain().Format(logEvent);
        string keyValue = KeyValueLogFormatter.KeyValue().Format(logEvent);
        string template = TemplateLogFormatter.WithTemplate("{Level}|{LoggerName}|{Message}|{TenantId}|{Exception}").Format(logEvent);

        Assert.That(JsonDocument.Parse(compactJson).RootElement.GetProperty("message").GetString(), Is.EqualTo("Processed 3"));
        Assert.That(JsonDocument.Parse(fullJson).RootElement.GetProperty("message").GetProperty("rendered").GetString(), Is.EqualTo("Processed 3"));
        Assert.That(text, Does.Contain("[Error] Application.Worker: Processed 3"));
        Assert.That(console, Is.EqualTo(text));
        Assert.That(ConsoleTextLogFormatter.HasAnsiColor(console), Is.False);
        Assert.That(keyValue, Does.Contain("message=\"Processed 3\""));
        Assert.That(keyValue, Does.Contain("TenantId=tenant-1"));
        Assert.That(template, Is.EqualTo("Error|Application.Worker|Processed 3|tenant-1| exception=InvalidOperationException: failed"));
    }

    [Test]
    public void JsonFormatters_PreserveEscapedRawAndNestedValues()
    {
        Dictionary<string, LogValue> nested = new Dictionary<string, LogValue>();
        nested["Quote"] = LogValue.String("Hello \"Ada\"\nNext");
        nested["Enabled"] = LogValue.Bool(true);
        LogEvent logEvent = LogEvent.Create(
            LogLevel.Info,
            "Application",
            "message",
            "rendered",
            new LogField[]
            {
                LogField.Raw("Raw", "{\"ok\":true,\"count\":2}"),
                LogField.Dictionary("Nested", nested)
            });

        JsonElement compactFields = JsonDocument.Parse(JsonLogFormatter.FormatCompact(logEvent)).RootElement.GetProperty("fields");
        JsonElement fullFields = JsonDocument.Parse(JsonLogFormatter.FormatFull(logEvent)).RootElement.GetProperty("fields");

        Assert.That(compactFields.GetProperty("Raw").GetProperty("ok").GetBoolean(), Is.True);
        Assert.That(compactFields.GetProperty("Raw").GetProperty("count").GetInt32(), Is.EqualTo(2));
        Assert.That(compactFields.GetProperty("Nested").GetProperty("Quote").GetString(), Is.EqualTo("Hello \"Ada\"\nNext"));
        Assert.That(fullFields[0].GetProperty("name").GetString(), Is.EqualTo("Raw"));
        Assert.That(fullFields[0].GetProperty("value").GetProperty("ok").GetBoolean(), Is.True);
        Assert.That(fullFields[1].GetProperty("value").GetProperty("Enabled").GetBoolean(), Is.True);
    }

    [Test]
    public void Formatters_NormalizeNonFiniteFloatingPointFields()
    {
        LogEvent logEvent = LogEvent.Create(
            LogLevel.Info,
            "Application",
            "metrics",
            "metrics",
            new LogField[]
            {
                LogField.Double("NotANumber", double.NaN),
                LogField.Double("PositiveInfinity", double.PositiveInfinity),
                LogField.Double("NegativeInfinity", double.NegativeInfinity),
                LogField.Double("Finite", 1.5d)
            });

        JsonElement compactFields = JsonDocument.Parse(JsonLogFormatter.FormatCompact(logEvent)).RootElement.GetProperty("fields");
        JsonElement fullFields = JsonDocument.Parse(JsonLogFormatter.FormatFull(logEvent)).RootElement.GetProperty("fields");
        string text = TextLogFormatter.Plain().Format(logEvent);
        string keyValue = KeyValueLogFormatter.FormatKeyValue(logEvent);

        Assert.That(compactFields.GetProperty("NotANumber").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(compactFields.GetProperty("PositiveInfinity").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(compactFields.GetProperty("NegativeInfinity").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(compactFields.GetProperty("Finite").GetDouble(), Is.EqualTo(1.5d));
        Assert.That(fullFields[0].GetProperty("value").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(fullFields[1].GetProperty("value").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(fullFields[2].GetProperty("value").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(fullFields[3].GetProperty("value").GetDouble(), Is.EqualTo(1.5d));
        Assert.That(text, Does.Contain("NotANumber=null"));
        Assert.That(text, Does.Contain("PositiveInfinity=null"));
        Assert.That(text, Does.Contain("NegativeInfinity=null"));
        Assert.That(text, Does.Contain("Finite=1.5"));
        Assert.That(keyValue, Does.Contain("NotANumber=null"));
        Assert.That(keyValue, Does.Contain("PositiveInfinity=null"));
        Assert.That(keyValue, Does.Contain("NegativeInfinity=null"));
        Assert.That(keyValue, Does.Contain("Finite=1.5"));
    }

    [Test]
    public void TextKeyValueAndConsoleFormatters_SanitizeLineBreaksDifferently()
    {
        LogEvent logEvent = LogEvent.Create(
            LogLevel.Warn,
            "Application",
            "message",
            "line one\nline two",
            new LogField[] { LogField.String("Path", "c:\\temp\tfile.txt") });

        string text = TextLogFormatter.Plain().Format(logEvent);
        string keyValue = KeyValueLogFormatter.KeyValue().Format(logEvent);
        string consoleAnsi = ConsoleTextLogFormatter.AnsiColor().Format(logEvent);

        Assert.That(text, Does.Contain("line one line two"));
        Assert.That(text, Does.Contain("Path=c:\\temp file.txt"));
        Assert.That(keyValue, Does.Contain("message=\"line one\\nline two\""));
        Assert.That(keyValue, Does.Contain("Path=\"c:\\\\temp file.txt\""));
        Assert.That(consoleAnsi, Does.StartWith(ConsoleTextLogFormatter.WarnColor));
        Assert.That(consoleAnsi, Does.EndWith(ConsoleTextLogFormatter.Reset));
        Assert.That(consoleAnsi, Does.Contain("line one line two"));
    }

    [Test]
    public void TemplateFormatter_RendersActivityRuntimeSourceFieldsAndFormats()
    {
        LogEvent logEvent = RichEvent();

        string output = TemplateLogFormatter.FormatTemplate(
            logEvent,
            "{Timestamp:yyyyMMddHHmmss}|{Level}|{Logger}|{EventId:000}|{EventName}|{TraceId}|{SpanId}|{ParentSpanId}|{ThreadId:000}|{ProcessId}|{MachineName}|{ApplicationName}|{SourceFile}|{SourceMemberName}|{SourceLineNumber:000}|{Properties}");

        Assert.That(output, Is.EqualTo("20260802010203|Error|Application.Worker|009|Processed|trace-1|span-1|parent-1|011|22|machine-a|app-a|Worker.cs|Run|123| {TenantId=tenant-1, Count=3}"));
    }

    [Test]
    public void FormatterDispatch_ResolvesAliasesAndUnknownNamesToCurrentFormatter()
    {
        LogEvent logEvent = RichEvent();
        LogFormatter formatter = LogFormatter.Named("text");

        string compact = formatter.DispatchLine("ndjson", logEvent);
        string keyValue = formatter.Dispatch("kv", logEvent);
        string unknown = formatter.Dispatch("unknown", logEvent);

        Assert.That(JsonDocument.Parse(compact).RootElement.GetProperty("level").GetString(), Is.EqualTo("Error"));
        Assert.That(keyValue, Does.Contain("level=Error"));
        Assert.That(unknown, Is.EqualTo(formatter.Format(logEvent)));
    }

    static LogEvent RichEvent()
    {
        return new LogEvent(
            new DateTimeOffset(2026, 8, 2, 1, 2, 3, TimeSpan.Zero),
            LogLevel.Error,
            "Application.Worker",
            9,
            "Processed",
            "Processed {Count}",
            "Processed 3",
            new LogField[]
            {
                LogField.String("TenantId", "tenant-1"),
                LogField.Int("Count", 3)
            },
            new InvalidOperationException("failed"),
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
    }
}
