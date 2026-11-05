using System.Text.Json;
using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicFormatterGoldenTests
{
    [Test]
    public void BuiltInFormatters_ProduceGoldenCoreOutputAndDocumentedLineEndings()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent logEvent = SimpleEvent(factory);
        LogFormatter text = logFactory.CreateTextFormatter();
        LogFormatter compactJson = logFactory.CreateCompactJsonFormatter();
        LogFormatter fullJson = logFactory.CreateFullJsonFormatter();
        LogFormatter plainConsole = logFactory.CreatePlainConsoleFormatter();
        LogFormatter colorConsole = logFactory.CreateColorConsoleFormatter();
        LogFormatter keyValue = logFactory.CreateKeyValueFormatter();
        LogFormatter template = logFactory.CreateTemplateFormatter();
        LogFormatter outputTemplate = logFactory.CreateOutputTemplateFormatter("{Level}|{Logger}|{Message}|{Name}");
        string expectedText = "2026-08-25T18:30:45.0000000Z [Warn] Application.Worker: Hello Ada {Name=Ada}";
        string expectedKeyValue = "timestamp=2026-08-25T18:30:45.0000000Z level=Warn logger=Application.Worker eventId=7 eventName=Greeting messageTemplate=\"Hello {Name}\" message=\"Hello Ada\" traceId=trace-1 spanId=span-1 parentSpanId=parent-1 threadId=11 processId=22 machineName=machine-a applicationName=app-a sourceFile=Worker.cs sourceMemberName=Run sourceLineNumber=42 Name=Ada";

        Assert.That(text.Format(logEvent), Is.EqualTo(expectedText));
        Assert.That(plainConsole.Format(logEvent), Is.EqualTo(expectedText));
        Assert.That(template.Format(logEvent), Is.EqualTo(expectedText));
        Assert.That(colorConsole.Format(logEvent), Is.EqualTo("\u001b[33m" + expectedText + "\u001b[0m"));
        Assert.That(keyValue.Format(logEvent), Is.EqualTo(expectedKeyValue));
        Assert.That(outputTemplate.Format(logEvent), Is.EqualTo("Warn|Application.Worker|Hello Ada|Ada"));
        Assert.That(text.FormatLine(logEvent), Is.EqualTo(expectedText + Environment.NewLine));
        Assert.That(plainConsole.FormatLine(logEvent), Is.EqualTo(expectedText + Environment.NewLine));
        Assert.That(colorConsole.FormatLine(logEvent), Is.EqualTo("\u001b[33m" + expectedText + "\u001b[0m" + Environment.NewLine));
        Assert.That(keyValue.FormatLine(logEvent), Is.EqualTo(expectedKeyValue + Environment.NewLine));
        Assert.That(template.FormatLine(logEvent), Is.EqualTo(expectedText + Environment.NewLine));
        Assert.That(outputTemplate.FormatLine(logEvent), Is.EqualTo("Warn|Application.Worker|Hello Ada|Ada" + Environment.NewLine));
        Assert.That(compactJson.FormatLine(logEvent), Is.EqualTo(compactJson.Format(logEvent) + "\n"));
        Assert.That(fullJson.FormatLine(logEvent), Is.EqualTo(fullJson.Format(logEvent)));
        Assert.That(JsonDocument.Parse(compactJson.FormatLine(logEvent)).RootElement.GetProperty("message").GetString(), Is.EqualTo("Hello Ada"));
        Assert.That(JsonDocument.Parse(fullJson.FormatLine(logEvent)).RootElement.GetProperty("message").GetProperty("rendered").GetString(), Is.EqualTo("Hello Ada"));
    }

    [Test]
    public void JsonFormatters_PreserveEscapingRawValuesActivityExceptionsAndNonFiniteNumbers()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent logEvent = RichEvent(factory);
        LogFormatter compactFormatter = logFactory.CreateCompactJsonFormatter();
        LogFormatter fullFormatter = logFactory.CreateFullJsonFormatter();
        string compactOutput = compactFormatter.Format(logEvent);
        string fullOutput = fullFormatter.Format(logEvent);
        JsonElement compact = JsonDocument.Parse(compactOutput).RootElement;
        JsonElement full = JsonDocument.Parse(fullOutput).RootElement;

        Assert.That(compactOutput, Does.Not.Contain("\n"));
        Assert.That(compact.GetProperty("message").GetString(), Is.EqualTo("line \"one\"\nline two"));
        Assert.That(compact.GetProperty("traceId").GetString(), Is.EqualTo("trace-1"));
        Assert.That(compact.GetProperty("spanId").GetString(), Is.EqualTo("span-1"));
        Assert.That(compact.GetProperty("parentSpanId").GetString(), Is.EqualTo("parent-1"));
        Assert.That(compact.GetProperty("fields").GetProperty("Raw").GetProperty("ok").GetBoolean(), Is.True);
        Assert.That(compact.GetProperty("fields").GetProperty("Raw").GetProperty("items")[1].GetString(), Is.EqualTo("a\"b"));
        Assert.That(compact.GetProperty("fields").GetProperty("Escaped").GetString(), Is.EqualTo("tab\tline\nslash\\quote\""));
        Assert.That(compact.GetProperty("fields").GetProperty("NotANumber").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(compact.GetProperty("fields").GetProperty("PositiveInfinity").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(compact.GetProperty("fields").GetProperty("NegativeInfinity").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(compact.GetProperty("exception").GetProperty("message").GetString(), Is.EqualTo("outer failed"));
        Assert.That(full.GetProperty("activity").GetProperty("traceId").GetString(), Is.EqualTo("trace-1"));
        Assert.That(FindFullField(full.GetProperty("fields"), "Raw").GetProperty("value").GetProperty("ok").GetBoolean(), Is.True);
        Assert.That(FindFullField(full.GetProperty("fields"), "NotANumber").GetProperty("value").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(full.GetProperty("exception").GetProperty("text").GetString(), Does.Contain("System.InvalidOperationException: outer failed"));
        Assert.That(full.GetProperty("exception").GetProperty("text").GetString(), Does.Contain("System.ArgumentException: inner failed"));
    }

    [Test]
    public void TextKeyValueAndOutputTemplateFormatters_HandleRichEdgeCases()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent logEvent = RichEvent(factory);
        LogFormatter text = logFactory.CreateTextFormatter();
        LogFormatter keyValue = logFactory.CreateKeyValueFormatter();
        LogFormatter outputTemplate = logFactory.CreateOutputTemplateFormatter(
            "{{begin}}|{Timestamp:yyyyMMddHHmmss}|{UtcTimestamp:HHmmss}|{Level}|{LoggerName}|{EventId:000}|{EventName}|{MessageTemplate}|{RenderedMessage}|{TraceId}|{SpanId}|{ParentSpanId}|{ThreadId:000}|{ProcessId}|{MachineName}|{ApplicationName}|{SourceFile}|{SourceMemberName}|{SourceLineNumber:000}|{Properties}|{Exception}|{NewLine}{Escaped}|{Missing}");

        string textOutput = text.Format(logEvent);
        string keyValueOutput = keyValue.Format(logEvent);
        string templateOutput = outputTemplate.Format(logEvent);

        Assert.That(textOutput, Does.Not.Contain("\n"));
        Assert.That(textOutput, Does.Contain("line \"one\" line two"));
        Assert.That(textOutput, Does.Contain("Raw={\"ok\":true,\"items\":[1,\"a\\\"b\"]}"));
        Assert.That(textOutput, Does.Contain("NotANumber=null"));
        Assert.That(textOutput, Does.Contain("exception=InvalidOperationException: outer failed"));
        Assert.That(keyValueOutput, Does.Not.Contain("\n"));
        Assert.That(keyValueOutput, Does.Contain("message=\"line \\\"one\\\"\\nline two\""));
        Assert.That(keyValueOutput, Does.Contain("traceId=trace-1 spanId=span-1 parentSpanId=parent-1"));
        Assert.That(keyValueOutput, Does.Contain("Escaped=\"tab line slash\\\\quote\\\"\""));
        Assert.That(templateOutput, Is.EqualTo(
            "{begin}|20260825183045|183045|Error|Application.Worker|009|Processed|Line {Value}|line \"one\" line two|trace-1|span-1|parent-1|011|22|machine-a|app-a|Worker.cs|Run|123| {Raw={\"ok\":true,\"items\":[1,\"a\\\"b\"]}, Escaped=tab line slash\\quote\", NotANumber=null, PositiveInfinity=null, NegativeInfinity=null}| exception=InvalidOperationException: outer failed|" + Environment.NewLine + "tab line slash\\quote\"|"));
    }

    [Test]
    public void ExceptionFormatters_RenderFullSummaryAndMissingExceptionForms()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent richEvent = RichEvent(factory);
        LogEvent noException = SimpleEvent(factory);
        LogFormatter full = logFactory.CreateFullExceptionFormatter();
        LogFormatter summary = logFactory.CreateSummaryExceptionFormatter();
        LogFormatter named = logFactory.CreateNamedFormatter("exception");

        Assert.That(full.Format(richEvent), Is.EqualTo("System.InvalidOperationException: outer failed" + Environment.NewLine + "Caused by: System.ArgumentException: inner failed"));
        Assert.That(summary.Format(richEvent), Is.EqualTo("System.InvalidOperationException: outer failed"));
        Assert.That(named.Format(richEvent), Is.EqualTo(full.Format(richEvent)));
        Assert.That(full.Format(noException), Is.Empty);
        Assert.That(summary.Format(noException), Is.Empty);
        Assert.That(full.FormatLine(richEvent), Is.EqualTo(full.Format(richEvent)));
        Assert.That(summary.FormatLine(richEvent), Is.EqualTo(summary.Format(richEvent)));
    }

    [Test]
    public void FormatterComposition_UsesCustomInterfaceNamedAndChildOperations()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent logEvent = SimpleEvent(factory);
        LogFormatter dispatcher = logFactory.CreateNamedFormatter("router");
        LogFormatter upper = logFactory.CreateCustomFormatter("upper", current => current.RenderedMessage.ToUpperInvariant());
        LogFormatter suffix = logFactory.CreateCustomFormatter("suffix", current => current.RenderedMessage + ":suffix");
        LogFormatter compact = logFactory.CreateNamedFormatter("ndjson");
        LogFormatter wrapped = logFactory.WrapFormatter(new BracketFormatter());
        LogFormatter fallback = logFactory.WrapFormatter(null);
        ILogFormatterConfiguration configuration = dispatcher;
        LogFormatter[] children = new LogFormatter[] { upper };

        configuration.SetFormatters(children);
        children[0] = suffix;
        configuration.AddFormatter(suffix);
        configuration.AddFormatter(compact);

        Assert.That(configuration.CanFormat("upper"), Is.True);
        Assert.That(configuration.CanFormat("suffix"), Is.True);
        Assert.That(configuration.CanFormat("compactjson"), Is.True);
        Assert.That(configuration.CanFormat("missing"), Is.False);
        Assert.That(configuration.FindFormatter("upper"), Is.SameAs(upper));
        Assert.That(configuration.FindFormatter("missing"), Is.SameAs(dispatcher));
        Assert.That(configuration.Dispatch("upper", logEvent), Is.EqualTo("HELLO ADA"));
        Assert.That(configuration.Dispatch("suffix", logEvent), Is.EqualTo("Hello Ada:suffix"));
        Assert.That(configuration.DispatchLine("ndjson", logEvent), Does.EndWith("\n"));
        Assert.That(wrapped.Format(logEvent), Is.EqualTo("[Hello Ada]"));
        Assert.That(fallback.Format(logEvent), Is.EqualTo(logFactory.CreateTextFormatter().Format(logEvent)));

        configuration.SetFormatAction(current => "override:" + current.EventName);
        Assert.That(dispatcher.Format(logEvent), Is.EqualTo("override:Greeting"));
        configuration.ClearFormatAction();
        Assert.That(dispatcher.Format(logEvent), Is.EqualTo("Hello Ada"));

        configuration.ClearFormatters();
        Assert.That(configuration.CanFormat("upper"), Is.False);
        Assert.That(configuration.CanFormat("compact-json"), Is.True);
        Assert.That(configuration.Dispatch("unknown", logEvent), Is.EqualTo("Hello Ada"));
        Assert.That(((ILogFormatter)dispatcher).Format(null), Is.Empty);
    }

    private static LogEvent SimpleEvent(ILogFactory factory)
    {
        return new LogEvent(
            new DateTimeOffset(2026, 8, 25, 18, 30, 45, TimeSpan.Zero),
            LogLevel.Warn,
            "Application.Worker",
            7,
            "Greeting",
            "Hello {Name}",
            "Hello Ada",
            new LogField[] { factory.CreateField("Name", "Ada") },
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
            42);
    }

    private static LogEvent RichEvent(LogFactory factory)
    {
        return new LogEvent(
            new DateTimeOffset(2026, 8, 25, 18, 30, 45, TimeSpan.Zero),
            LogLevel.Error,
            "Application.Worker",
            9,
            "Processed",
            "Line {Value}",
            "line \"one\"\nline two",
            new LogField[]
            {
                factory.CreateRawJsonField("Raw", "{\"ok\":true,\"items\":[1,\"a\\\"b\"]}"),
                factory.CreateStringField("Escaped", "tab\tline\nslash\\quote\""),
                factory.CreateDoubleField("NotANumber", double.NaN),
                factory.CreateDoubleField("PositiveInfinity", double.PositiveInfinity),
                factory.CreateDoubleField("NegativeInfinity", double.NegativeInfinity)
            },
            new InvalidOperationException("outer failed", new ArgumentException("inner failed")),
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

    private static JsonElement FindFullField(JsonElement fields, string name)
    {
        foreach (JsonElement field in fields.EnumerateArray())
        {
            if (field.GetProperty("name").GetString() == name)
            {
                return field;
            }
        }

        throw new InvalidOperationException("field not found: " + name);
    }

    private sealed class BracketFormatter : ILogFormatter
    {
        public string Format(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return "";
            }

            return "[" + logEvent.RenderedMessage + "]";
        }
    }
}
