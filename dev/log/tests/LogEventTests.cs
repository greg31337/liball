using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogEventTests
{
    [Test]
    public void Constructor_StoresCoreMessageAndEventData()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 15, 0, 0, TimeSpan.Zero);
        InvalidOperationException exception = new InvalidOperationException("failed");
        LogField[] fields = new LogField[]
        {
            LogField.String("user.id", "42"),
            LogField.String("bad field", "ignored"),
            null!
        };

        LogEvent logEvent = new LogEvent(timestamp, LogLevel.Error, "Application.Worker", 12, "WorkerFailed", "Worker {Id} failed", "Worker 42 failed", fields, exception, "trace", "span", "parent", 7, 99, "machine", "app", "Worker.cs", "Run", 123);

        Assert.That(logEvent.Timestamp, Is.EqualTo(timestamp));
        Assert.That(logEvent.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(logEvent.EventId, Is.EqualTo(12));
        Assert.That(logEvent.EventName, Is.EqualTo("WorkerFailed"));
        Assert.That(logEvent.MessageTemplate, Is.EqualTo("Worker {Id} failed"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("Worker 42 failed"));
        Assert.That(logEvent.ExceptionValue, Is.SameAs(exception));
        Assert.That(logEvent.IsValid, Is.True);
    }

    [Test]
    public void Constructor_StoresActivityRuntimeAndSourceData()
    {
        LogEvent logEvent = new LogEvent(DateTimeOffset.MinValue, LogLevel.Info, "Application.Worker", 0, "", "", "", new LogField[0], null, "trace-id", "span-id", "parent-id", 11, 22, "machine", "app", "File.cs", "Method", 33);

        Assert.That(logEvent.TraceId, Is.EqualTo("trace-id"));
        Assert.That(logEvent.SpanId, Is.EqualTo("span-id"));
        Assert.That(logEvent.ParentSpanId, Is.EqualTo("parent-id"));
        Assert.That(logEvent.ThreadId, Is.EqualTo(11));
        Assert.That(logEvent.ProcessId, Is.EqualTo(22));
        Assert.That(logEvent.MachineName, Is.EqualTo("machine"));
        Assert.That(logEvent.ApplicationName, Is.EqualTo("app"));
        Assert.That(logEvent.SourceFile, Is.EqualTo("File.cs"));
        Assert.That(logEvent.SourceMemberName, Is.EqualTo("Method"));
        Assert.That(logEvent.SourceLineNumber, Is.EqualTo(33));
    }

    [Test]
    public void Constructor_CopiesOnlyValidFieldsAndCountsRejectedFields()
    {
        LogField[] fields = new LogField[]
        {
            LogField.String("first", "a"),
            LogField.String("bad field", "b"),
            null!,
            LogField.Int("second", 2)
        };

        LogEvent logEvent = LogEvent.CreateAt(DateTimeOffset.MinValue, LogLevel.Info, "", 0, "", "", "", fields, null);
        fields[0] = LogField.String("changed", "c");

        Assert.That(logEvent.Fields.Length, Is.EqualTo(2));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("first"));
        Assert.That(logEvent.Fields[0].Value.TextValue, Is.EqualTo("a"));
        Assert.That(logEvent.Fields[1].Name, Is.EqualTo("second"));
        Assert.That(logEvent.InvalidFieldCount, Is.EqualTo(2));
    }

    [Test]
    public void Constructor_AllowsEmptyRootLoggerButMarksInvalidNamedLogger()
    {
        LogEvent rootEvent = LogEvent.CreateAt(DateTimeOffset.MinValue, LogLevel.Info, "", 0, "", "", "", new LogField[0], null);
        LogEvent invalidEvent = LogEvent.CreateAt(DateTimeOffset.MinValue, LogLevel.Info, "bad logger", 0, "", "", "", new LogField[0], null);

        Assert.That(rootEvent.IsValid, Is.True);
        Assert.That(rootEvent.LoggerName, Is.EqualTo(""));
        Assert.That(invalidEvent.IsValid, Is.False);
        Assert.That(invalidEvent.ErrorMessage, Is.EqualTo("logger name can contain only letters, digits, underscores, and dots"));
    }

    [Test]
    public void Constructor_NormalizesNullTextAndNegativeRuntimeNumbers()
    {
        LogEvent logEvent = new LogEvent(DateTimeOffset.MinValue, LogLevel.Info, null!, 0, null!, null!, null!, null!, null, null!, null!, null!, -1, -2, null!, null!, null!, null!, -3);

        Assert.That(logEvent.LoggerName, Is.EqualTo(""));
        Assert.That(logEvent.EventName, Is.EqualTo(""));
        Assert.That(logEvent.MessageTemplate, Is.EqualTo(""));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo(""));
        Assert.That(logEvent.TraceId, Is.EqualTo(""));
        Assert.That(logEvent.ThreadId, Is.EqualTo(0));
        Assert.That(logEvent.ProcessId, Is.EqualTo(0));
        Assert.That(logEvent.SourceLineNumber, Is.EqualTo(0));
    }

    [Test]
    public void Create_CapturesCurrentRuntimeMetadata()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;

        LogEvent logEvent = LogEvent.Create(LogLevel.Warn, "Application.Worker", "message", "message", new LogField[0]);

        DateTimeOffset after = DateTimeOffset.UtcNow;

        Assert.That(logEvent.Timestamp, Is.GreaterThanOrEqualTo(before));
        Assert.That(logEvent.Timestamp, Is.LessThanOrEqualTo(after));
        Assert.That(logEvent.ThreadId, Is.EqualTo(Environment.CurrentManagedThreadId));
        Assert.That(logEvent.ProcessId, Is.EqualTo(Environment.ProcessId));
        Assert.That(logEvent.MachineName.Length, Is.GreaterThan(0));
        Assert.That(logEvent.ApplicationName.Length, Is.GreaterThan(0));
    }
}
