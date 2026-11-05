using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogEventBuilderTests
{
    [Test]
    public void Build_UsesClockAndCoreEventData()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 17, 0, 0, TimeSpan.Zero);
        LogEventBuilder builder = LogEventBuilder.Create(LogClock.Fixed(timestamp));

        LogEvent logEvent = builder
            .SetLevel(LogLevel.Warn)
            .SetLoggerName("Application.Worker")
            .SetEvent(7, "WorkerSlow")
            .SetMessage("Worker {Id} slow", "Worker 42 slow")
            .Build();

        Assert.That(logEvent.Timestamp, Is.EqualTo(timestamp));
        Assert.That(logEvent.Level, Is.EqualTo(LogLevel.Warn));
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(logEvent.EventId, Is.EqualTo(7));
        Assert.That(logEvent.EventName, Is.EqualTo("WorkerSlow"));
        Assert.That(logEvent.MessageTemplate, Is.EqualTo("Worker {Id} slow"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("Worker 42 slow"));
    }

    [Test]
    public void Build_UsesTemplateAsRenderedMessageWhenRenderedMessageMissing()
    {
        LogEventBuilder builder = LogEventBuilder.Create(LogClock.Fixed(DateTimeOffset.MinValue));

        LogEvent logEvent = builder.SetMessageTemplate("plain template").Build();

        Assert.That(logEvent.MessageTemplate, Is.EqualTo("plain template"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("plain template"));
    }

    [Test]
    public void Build_MergesScopeEventAndEnricherFieldsInOrder()
    {
        LogEventBuilder builder = LogEventBuilder.Create(LogClock.Fixed(DateTimeOffset.MinValue));

        LogEvent logEvent = builder
            .AddScopeField(LogField.String("scope", "request"))
            .AddField(LogField.String("event", "value"))
            .AddEnricherField(LogField.String("machine", "host"))
            .Build();

        Assert.That(logEvent.Fields.Length, Is.EqualTo(3));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("scope"));
        Assert.That(logEvent.Fields[1].Name, Is.EqualTo("event"));
        Assert.That(logEvent.Fields[2].Name, Is.EqualTo("machine"));
    }

    [Test]
    public void Build_CountsInvalidFieldsFromEverySource()
    {
        LogEventBuilder builder = LogEventBuilder.Create(LogClock.Fixed(DateTimeOffset.MinValue));

        LogEvent logEvent = builder
            .AddScopeField(LogField.String("bad scope", "ignored"))
            .AddField(null)
            .AddEnricherField(LogField.String("bad enricher", "ignored"))
            .Build();

        Assert.That(logEvent.Fields.Length, Is.EqualTo(0));
        Assert.That(logEvent.InvalidFieldCount, Is.EqualTo(3));
    }

    [Test]
    public void Build_StoresExceptionActivityRuntimeAndSourceData()
    {
        InvalidOperationException exception = new InvalidOperationException("failed");
        LogEventBuilder builder = LogEventBuilder.Create(LogClock.Fixed(DateTimeOffset.MinValue));

        LogEvent logEvent = builder
            .SetException(exception)
            .SetActivity("trace", "span", "parent")
            .SetRuntime(10, 20, "machine", "application")
            .SetSource("Source.cs", "Run", 30)
            .Build();

        Assert.That(logEvent.ExceptionValue, Is.SameAs(exception));
        Assert.That(logEvent.TraceId, Is.EqualTo("trace"));
        Assert.That(logEvent.SpanId, Is.EqualTo("span"));
        Assert.That(logEvent.ParentSpanId, Is.EqualTo("parent"));
        Assert.That(logEvent.ThreadId, Is.EqualTo(10));
        Assert.That(logEvent.ProcessId, Is.EqualTo(20));
        Assert.That(logEvent.MachineName, Is.EqualTo("machine"));
        Assert.That(logEvent.ApplicationName, Is.EqualTo("application"));
        Assert.That(logEvent.SourceFile, Is.EqualTo("Source.cs"));
        Assert.That(logEvent.SourceMemberName, Is.EqualTo("Run"));
        Assert.That(logEvent.SourceLineNumber, Is.EqualTo(30));
    }

    [Test]
    public void AddFields_AllowsNullArraysWithoutChangingState()
    {
        LogEventBuilder builder = LogEventBuilder.Create(LogClock.Fixed(DateTimeOffset.MinValue));

        LogEvent logEvent = builder
            .AddFields(null!)
            .AddScopeFields(null!)
            .AddEnricherFields(null!)
            .Build();

        Assert.That(logEvent.Fields.Length, Is.EqualTo(0));
        Assert.That(logEvent.InvalidFieldCount, Is.EqualTo(0));
    }
}
