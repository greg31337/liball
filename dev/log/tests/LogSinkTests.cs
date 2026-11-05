using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogSinkTests
{
    [Test]
    public void Write_StoresEventAndReturnsConfiguredResult()
    {
        LogSink sink = LogSink.Named("primary");
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        sink.SetResult(LogResult.DiagnosticCode("sink.test", "accepted"));
        LogResult result = sink.Write(logEvent);

        Assert.That(sink.Name, Is.EqualTo("primary"));
        Assert.That(sink.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
        Assert.That(result.IsDiagnostic, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.test"));
    }

    [Test]
    public void Write_ReturnsDroppedWhenDisabled()
    {
        LogSink sink = new LogSink();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        sink.IsEnabled = false;
        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsDropped, Is.True);
        Assert.That(sink.LastEvent, Is.Null);
        Assert.That(sink.WrittenCount, Is.EqualTo(0));
    }

    [Test]
    public void Write_ReturnsFailureForNullEvent()
    {
        LogSink sink = new LogSink();

        LogResult result = sink.Write(null);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.event.null"));
    }

    [Test]
    public void Write_UsesSinkSpecificMinimumLevel()
    {
        LogSink sink = new LogSink();
        LogEvent warnEvent = LogEvent.Create(LogLevel.Warn, "Application", "warn", "warn", new LogField[0]);
        LogEvent errorEvent = LogEvent.Create(LogLevel.Error, "Application", "error", "error", new LogField[0]);

        sink.SetMinimumLevel(LogLevel.Error);
        LogResult warnResult = sink.Write(warnEvent);
        LogResult errorResult = sink.Write(errorEvent);

        Assert.That(warnResult.IsDropped, Is.True);
        Assert.That(warnResult.Message, Is.EqualTo("sink level disabled"));
        Assert.That(errorResult.IsSuccess, Is.True);
        Assert.That(sink.LastEvent, Is.SameAs(errorEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_UsesSinkSpecificFilters()
    {
        LogSink sink = new LogSink();
        LogEvent missingEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);
        LogEvent presentEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[] { LogField.String("TenantId", "tenant-1") });

        sink.AddFilter(LogFilter.FieldExists("TenantId"));
        LogResult missingResult = sink.Write(missingEvent);
        LogResult presentResult = sink.Write(presentEvent);

        Assert.That(missingResult.IsDropped, Is.True);
        Assert.That(missingResult.Message, Is.EqualTo("sink event filtered"));
        Assert.That(presentResult.IsSuccess, Is.True);
        Assert.That(sink.LastEvent, Is.SameAs(presentEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void IsEnabledForLevel_UsesEnabledFlagAndMinimumLevel()
    {
        LogSink sink = new LogSink();

        sink.SetMinimumLevel(LogLevel.Warn);

        Assert.That(sink.IsEnabledForLevel(LogLevel.Info), Is.False);
        Assert.That(sink.IsEnabledForLevel(LogLevel.Warn), Is.True);

        sink.IsEnabled = false;

        Assert.That(sink.IsEnabledForLevel(LogLevel.Fatal), Is.False);
    }

    [Test]
    public void Write_ForwardsEventToSubPipeline()
    {
        LogPipeline childPipeline = new LogPipeline();
        LogSink first = LogSink.Named("first");
        LogSink second = LogSink.Named("second");
        LogSink sink = LogSink.PipelineSink("branch", childPipeline);
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        childPipeline.SetSinks(new LogSink[] { first, second });
        LogResult result = sink.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(sink.LastEvent, Is.SameAs(logEvent));
        Assert.That(sink.WrittenCount, Is.EqualTo(1));
        Assert.That(first.LastEvent, Is.SameAs(logEvent));
        Assert.That(second.LastEvent, Is.SameAs(logEvent));
        Assert.That(first.WrittenCount, Is.EqualTo(1));
        Assert.That(second.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_AppliesSinkRulesBeforeSubPipeline()
    {
        LogPipeline childPipeline = new LogPipeline();
        LogSink child = LogSink.Named("child");
        LogSink sink = LogSink.PipelineSink("branch", childPipeline);
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);
        LogEvent errorEvent = LogEvent.Create(LogLevel.Error, "Application", "error", "error", new LogField[0]);

        childPipeline.AddSink(child);
        sink.SetMinimumLevel(LogLevel.Error);
        LogResult infoResult = sink.Write(infoEvent);
        LogResult errorResult = sink.Write(errorEvent);

        Assert.That(infoResult.IsDropped, Is.True);
        Assert.That(child.WrittenCount, Is.EqualTo(1));
        Assert.That(child.LastEvent, Is.SameAs(errorEvent));
        Assert.That(errorResult.IsSuccess, Is.True);
    }

    [Test]
    public void IsEnabledForLevel_UsesSubPipelineEligibility()
    {
        LogPipeline childPipeline = new LogPipeline();
        LogSink child = LogSink.Named("child");
        LogSink sink = LogSink.PipelineSink("branch", childPipeline);

        child.SetMinimumLevel(LogLevel.Error);
        childPipeline.AddSink(child);

        Assert.That(sink.IsEnabledForLevel(LogLevel.Warn), Is.False);
        Assert.That(sink.IsEnabledForLevel(LogLevel.Error), Is.True);
    }
}
