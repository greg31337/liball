using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogValueRedactionReservedWorkflowTests
{
    [Test]
    public void ToLogValueLimit_NormalizesNestedDictionariesArraysAndLimits()
    {
        Dictionary<string, object> payload = new Dictionary<string, object>();
        payload["name"] = "abcdef";
        payload["items"] = new object[] { "first", "second", "third" };
        payload["nested"] = new Dictionary<string, object> { ["tooDeep"] = "value" };

        LogValue value = LogTemplateBinder.ToLogValueLimit(payload, 3, 2, 2);

        Assert.That(value.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(value.DictionaryValue.Count, Is.EqualTo(2));
        Assert.That(value.IsTruncated, Is.True);
        Assert.That(value.DictionaryValue["name"].TextValue, Is.EqualTo("abc"));
        Assert.That(value.DictionaryValue["name"].IsTruncated, Is.True);
        Assert.That(value.DictionaryValue["items"].ArrayValue.Length, Is.EqualTo(2));
        Assert.That(value.DictionaryValue["items"].IsTruncated, Is.True);
    }

    [Test]
    public void ToLogValue_CopiesNestedValuesDefensively()
    {
        Dictionary<string, object> payload = new Dictionary<string, object>();
        object[] items = new object[] { "first", 2 };
        payload["items"] = items;

        LogValue value = LogTemplateBinder.ToLogValue(payload);

        items[0] = "changed";
        payload["items"] = new object[] { "other" };

        Assert.That(value.DictionaryValue["items"].ArrayValue[0].TextValue, Is.EqualTo("first"));
        Assert.That(value.DictionaryValue["items"].ArrayValue[1].IntegerValue, Is.EqualTo(2));
    }

    [Test]
    public void RedactFields_DoesNotMutateOriginalAndMatchesReservedSuffixes()
    {
        LogField[] fields = new LogField[]
        {
            LogField.String("field_message_1", "secret"),
            LogField.String("user", "Ada")
        };

        LogField[] redacted = LogRedactor.RedactFields(fields, new string[] { "message" });

        Assert.That(redacted[0].Value.TextValue, Is.EqualTo(LogRedactor.RedactedText));
        Assert.That(redacted[1].Value.TextValue, Is.EqualTo("Ada"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo("secret"));
        Assert.That(fields[1].Value.TextValue, Is.EqualTo("Ada"));
    }

    [Test]
    public void RedactEvent_PreservesMetadataAndRedactsOnlyFields()
    {
        InvalidOperationException exception = new InvalidOperationException("failed");
        LogEvent logEvent = new LogEvent(
            new DateTimeOffset(2026, 8, 2, 22, 0, 0, TimeSpan.Zero),
            LogLevel.Error,
            "Application.Worker",
            17,
            "Failure",
            "Failed {Password}",
            "Failed secret",
            new LogField[] { LogField.String("Password", "secret") },
            exception,
            "trace",
            "span",
            "parent",
            11,
            22,
            "machine",
            "application",
            "file.cs",
            "Run",
            33);

        LogEvent redacted = LogRedactor.RedactEvent(logEvent, new string[] { "password" });

        Assert.That(redacted.Timestamp, Is.EqualTo(logEvent.Timestamp));
        Assert.That(redacted.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(redacted.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(redacted.ExceptionValue, Is.SameAs(exception));
        Assert.That(redacted.TraceId, Is.EqualTo("trace"));
        Assert.That(redacted.SourceLineNumber, Is.EqualTo(33));
        Assert.That(redacted.Fields[0].Value.TextValue, Is.EqualTo(LogRedactor.RedactedText));
    }

    [Test]
    public void ReservedFields_ResolveMultipleReservedNamesAndExistingCollisions()
    {
        LogField[] resolved = LogReservedFields.Resolve(
            new LogField[]
            {
                LogField.String("field_timestamp", "existing"),
                LogField.String("timestamp", "reserved"),
                LogField.String("level", "level"),
                LogField.String("fields", "fields")
            });

        Assert.That(resolved.Length, Is.EqualTo(4));
        Assert.That(resolved[0].Name, Is.EqualTo("field_timestamp"));
        Assert.That(resolved[1].Name, Is.EqualTo("field_timestamp_1"));
        Assert.That(resolved[2].Name, Is.EqualTo("field_level"));
        Assert.That(resolved[3].Name, Is.EqualTo("field_fields"));
    }

    [Test]
    public void Logger_Write_ResolvesReservedDuplicateTemplateFields()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        logger.AddSink(sink);

        LogResult result = logger.InfoWithValues("{message} {field_message} {message}", new object[] { "first", "existing", "second" });
        LogEvent logEvent = MemoryLogSink.Events(sink)[0];

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("first existing second"));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("field_message_1"));
        Assert.That(logEvent.Fields[0].Value.TextValue, Is.EqualTo("first"));
        Assert.That(logEvent.Fields[1].Name, Is.EqualTo("field_message"));
        Assert.That(logEvent.Fields[1].Value.TextValue, Is.EqualTo("existing"));
        Assert.That(logEvent.Fields[2].Name, Is.EqualTo("field_message_2"));
        Assert.That(logEvent.Fields[2].Value.TextValue, Is.EqualTo("second"));
    }
}
