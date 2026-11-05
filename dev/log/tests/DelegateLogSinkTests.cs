using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class DelegateLogSinkTests
{
    [Test]
    public void Create_ReturnsEnabledDelegateSink()
    {
        LogSink sink = DelegateLogSink.Create(logEvent => LogResult.Success());

        Assert.That(sink.Name, Is.EqualTo(DelegateLogSink.DefaultName));
        Assert.That(sink.IsEnabled, Is.True);
        Assert.That(sink.IsDelegateSink, Is.True);
        Assert.That(DelegateLogSink.IsDelegate(sink), Is.True);
        Assert.That(sink.DelegateWriteAction, Is.Not.Null);
    }

    [Test]
    public void Named_UsesConfiguredName()
    {
        LogSink sink = DelegateLogSink.Named("callbacks", logEvent => LogResult.Success());

        Assert.That(sink.Name, Is.EqualTo("callbacks"));
        Assert.That(sink.IsDelegateSink, Is.True);
    }

    [Test]
    public void Write_InvokesDelegateWithAcceptedEvent()
    {
        LogEvent? captured = null;
        LogResult expected = LogResult.DiagnosticCode("delegate.accepted", "accepted");
        LogSink sink = DelegateLogSink.Create(logEvent =>
        {
            captured = logEvent;
            return expected;
        });
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result, Is.SameAs(expected));
        Assert.That(captured, Is.SameAs(logEvent));
        Assert.That(sink.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_NormalizesNullDelegateResultToSuccess()
    {
        LogSink sink = DelegateLogSink.Create(logEvent => null);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(sink.LastResult.IsSuccess, Is.True);
    }

    [Test]
    public void Write_ReturnsFailureWhenDelegateIsMissing()
    {
        LogSink sink = DelegateLogSink.Create(null);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.delegate.missing"));
        Assert.That(sink.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void SetWriteAction_ReplacesDelegate()
    {
        string captured = "";
        LogSink sink = DelegateLogSink.Create(logEvent => LogResult.FailureCode("old", "old"));
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "rendered", new LogField[0]);

        DelegateLogSink.SetWriteAction(sink, logEvent =>
        {
            captured = logEvent.RenderedMessage;
            return LogResult.Success();
        });
        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(captured, Is.EqualTo("rendered"));
    }

    [Test]
    public void Write_HonorsThresholdsAndFiltersBeforeInvokingDelegate()
    {
        int invokedCount = 0;
        LogSink sink = DelegateLogSink.Create(logEvent =>
        {
            invokedCount = invokedCount + 1;
            return LogResult.Success();
        });
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent missingEvent = LogEvent.Create(LogLevel.Error, "Application", "missing", "missing", new LogField[0]);
        LogEvent presentEvent = LogEvent.Create(LogLevel.Error, "Application", "present", "present", new LogField[] { LogField.String("TenantId", "tenant-1") });

        sink.SetMinimumLevel(LogLevel.Error);
        sink.AddFilter(LogFilter.FieldExists("TenantId"));
        LogResult infoResult = sink.Write(infoEvent);
        LogResult missingResult = sink.Write(missingEvent);
        LogResult presentResult = sink.Write(presentEvent);

        Assert.That(infoResult.IsDropped, Is.True);
        Assert.That(missingResult.IsDropped, Is.True);
        Assert.That(presentResult.IsSuccess, Is.True);
        Assert.That(invokedCount, Is.EqualTo(1));
        Assert.That(sink.LastEvent, Is.SameAs(presentEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Logger_CanWriteThroughDelegateSink()
    {
        LogEvent? captured = null;
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = DelegateLogSink.Create(logEvent =>
        {
            captured = logEvent;
            return LogResult.Success();
        });

        logger.AddSink(sink);
        LogResult result = logger.InfoWithValues("Processed {Count}", new object[] { 3 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(captured, Is.SameAs(logger.LastEvent));
        Assert.That(captured!.RenderedMessage, Is.EqualTo("Processed 3"));
        Assert.That(captured.Fields[0].Name, Is.EqualTo("Count"));
    }

    [Test]
    public void Logger_DoesNotAcceptEventWhenDelegateFails()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = DelegateLogSink.Create(logEvent => LogResult.FailureCode("delegate.failed", "failed"));

        logger.AddSink(sink);
        LogResult result = logger.Info("message");

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("delegate.failed"));
        Assert.That(logger.LastEvent, Is.Null);
        Assert.That(logger.WrittenCount, Is.EqualTo(0));
        Assert.That(sink.LastEvent, Is.Not.Null);
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }
}
