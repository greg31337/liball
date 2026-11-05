using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicLogEventBuilderTests
{
    [Test]
    public void Builder_CreatesCompleteEventForWriteEvent()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 25, 14, 30, 0, TimeSpan.Zero);
        FixedClock clock = new FixedClock(timestamp);
        LogFactory factory = new LogFactory();
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        CapturingLogger logger = new CapturingLogger();
        ILogEventBuilder builder = new LogEventBuilder(clock);

        LogEvent logEvent = builder
            .SetLevel(LogLevel.Error)
            .SetLoggerName("Application.Worker")
            .SetEvent(42, "WorkFailed")
            .SetMessageTemplateAndValues("Failed item {ItemId}", new object?[] { 17 })
            .SetException(exceptionValue)
            .AddField(factory.CreateField("password", "secret"))
            .SetTraceContext("trace-id", "span-id", "parent-id")
            .SetRuntimeMetadata(7, 11, "machine-a", "application-a")
            .SetSourceHints("Worker.cs", "Run", 123)
            .AddRedactedFieldName("password")
            .Build();

        LogResult result = logger.WriteEvent(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.Event, Is.SameAs(logEvent));
        Assert.That(logEvent.Timestamp, Is.EqualTo(timestamp));
        Assert.That(logEvent.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(logEvent.EventId, Is.EqualTo(42));
        Assert.That(logEvent.EventName, Is.EqualTo("WorkFailed"));
        Assert.That(logEvent.MessageTemplate, Is.EqualTo("Failed item {ItemId}"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("Failed item 17"));
        Assert.That(logEvent.Fields.Length, Is.EqualTo(2));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("ItemId"));
        Assert.That(logEvent.Fields[0].Value.IntegerValue, Is.EqualTo(17));
        Assert.That(logEvent.Fields[1].Name, Is.EqualTo("password"));
        Assert.That(logEvent.Fields[1].Value.TextValue, Is.EqualTo("[redacted]"));
        Assert.That(logEvent.ExceptionValue, Is.SameAs(exceptionValue));
        Assert.That(logEvent.TraceId, Is.EqualTo("trace-id"));
        Assert.That(logEvent.SpanId, Is.EqualTo("span-id"));
        Assert.That(logEvent.ParentSpanId, Is.EqualTo("parent-id"));
        Assert.That(logEvent.ThreadId, Is.EqualTo(7));
        Assert.That(logEvent.ProcessId, Is.EqualTo(11));
        Assert.That(logEvent.MachineName, Is.EqualTo("machine-a"));
        Assert.That(logEvent.ApplicationName, Is.EqualTo("application-a"));
        Assert.That(logEvent.SourceFile, Is.EqualTo("Worker.cs"));
        Assert.That(logEvent.SourceMemberName, Is.EqualTo("Run"));
        Assert.That(logEvent.SourceLineNumber, Is.EqualTo(123));
    }

    [Test]
    public void Builder_NormalizesOptionalInputsAndCanSuppressRuntimeMetadata()
    {
        LogEvent logEvent = new LogEventBuilder(new FixedClock(DateTimeOffset.MinValue))
            .SetLoggerName(null)
            .SetEvent(-1, null)
            .SetMessageTemplateAndValues(null, null)
            .AddFields(null)
            .SetTraceContext(null, null, null)
            .SetRuntimeMetadataEnabled(false)
            .SetSourceHints(null, null, -3)
            .SetRedactedFieldNames(null)
            .Build();

        Assert.That(logEvent.Timestamp, Is.EqualTo(DateTimeOffset.MinValue));
        Assert.That(logEvent.LoggerName, Is.Empty);
        Assert.That(logEvent.EventId, Is.EqualTo(-1));
        Assert.That(logEvent.EventName, Is.Empty);
        Assert.That(logEvent.MessageTemplate, Is.Empty);
        Assert.That(logEvent.RenderedMessage, Is.Empty);
        Assert.That(logEvent.Fields, Is.Empty);
        Assert.That(logEvent.TraceId, Is.Empty);
        Assert.That(logEvent.SpanId, Is.Empty);
        Assert.That(logEvent.ParentSpanId, Is.Empty);
        Assert.That(logEvent.ThreadId, Is.Zero);
        Assert.That(logEvent.ProcessId, Is.Zero);
        Assert.That(logEvent.MachineName, Is.Empty);
        Assert.That(logEvent.ApplicationName, Is.Empty);
        Assert.That(logEvent.SourceFile, Is.Empty);
        Assert.That(logEvent.SourceMemberName, Is.Empty);
        Assert.That(logEvent.SourceLineNumber, Is.Zero);
    }

    [Test]
    public void Builder_ReportsInvalidInputsAndReplacesRedactionNames()
    {
        ILogValueFactory factory = new LogFactory();
        LogField invalid = new LogField("bad name", factory.CreateStringValue("invalid"));
        LogEvent logEvent = new LogEventBuilder(new FixedClock(DateTimeOffset.UnixEpoch))
            .SetLevel(LogLevel.Warn)
            .SetLoggerName("bad logger")
            .SetMessageTemplateAndValues("Values {First} {Second}", new object?[] { 1 })
            .AddField(factory.CreateStringField("Secret", "visible"))
            .AddFields(new LogField[] { factory.CreateStringField("Token", "hidden"), invalid })
            .AddRedactedFieldName("Secret")
            .SetRedactedFieldNames(new string[] { "Token" })
            .Build();

        Assert.That(logEvent.IsValid, Is.False);
        Assert.That(logEvent.ErrorMessage, Is.Not.Empty);
        Assert.That(logEvent.InvalidFieldCount, Is.EqualTo(1));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("Values 1 null"));
        Assert.That(FindField(logEvent, "First").Value.IntegerValue, Is.EqualTo(1));
        Assert.That(FindField(logEvent, "Second").Value.Kind, Is.EqualTo(LogValueKind.Null));
        Assert.That(FindField(logEvent, "Secret").Value.TextValue, Is.EqualTo("visible"));
        Assert.That(FindField(logEvent, "Token").Value.TextValue, Is.EqualTo("[redacted]"));
    }

    static LogField FindField(LogEvent logEvent, string name)
    {
        foreach (LogField field in logEvent.Fields)
        {
            if (field.Name == name)
            {
                return field;
            }
        }

        throw new InvalidOperationException("field not found: " + name);
    }

    private sealed class FixedClock : ILogClock
    {
        public DateTimeOffset Timestamp;

        public FixedClock(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
        }

        public DateTimeOffset UtcNow()
        {
            return Timestamp;
        }
    }

    private sealed class CapturingLogger : ILogger
    {
        public LogEvent? Event;

        public bool IsEnabled(LogLevel level)
        {
            return true;
        }

        public LogResult WriteWithValues(LogLevel level, string? messageTemplate, object?[]? values)
        {
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult WriteWithExceptionAndValues(LogLevel level, Exception? exceptionValue, string? messageTemplate, object?[]? values)
        {
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult WriteEvent(LogEvent? logEvent)
        {
            Event = logEvent;
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }
    }
}
