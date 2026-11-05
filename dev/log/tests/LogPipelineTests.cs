using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogPipelineTests
{
    [Test]
    public void Write_WithNoSinksSucceeds()
    {
        LogPipeline pipeline = new LogPipeline();
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        LogResult result = pipeline.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(pipeline.LastEvent, Is.SameAs(logEvent));
        Assert.That(pipeline.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_FansOutToEverySink()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink first = LogSink.Named("first");
        LogSink second = LogSink.Named("second");
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        pipeline.AddSink(first);
        pipeline.AddSink(second);
        LogResult result = pipeline.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(first.LastEvent, Is.SameAs(logEvent));
        Assert.That(second.LastEvent, Is.SameAs(logEvent));
        Assert.That(first.WrittenCount, Is.EqualTo(1));
        Assert.That(second.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_CombinesSinkResultsAfterWritingAllSinks()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink success = LogSink.Named("success");
        LogSink failure = LogSink.Named("failure");
        LogSink dropped = LogSink.Named("dropped");
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        failure.SetResult(LogResult.FailureCode("sink.failure", "failed"));
        dropped.SetResult(LogResult.Dropped("dropped"));
        pipeline.SetSinks(new LogSink[] { success, failure, dropped });
        LogResult result = pipeline.Write(logEvent);

        Assert.That(success.WrittenCount, Is.EqualTo(1));
        Assert.That(failure.WrittenCount, Is.EqualTo(1));
        Assert.That(dropped.WrittenCount, Is.EqualTo(1));
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.IsDropped, Is.True);
        Assert.That(result.FailureCount, Is.EqualTo(1));
        Assert.That(result.DroppedCount, Is.EqualTo(1));
        Assert.That(result.Code, Is.EqualTo("sink.failure"));
    }

    [Test]
    public void SetSinks_CopiesSinkArray()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink first = LogSink.Named("first");
        LogSink second = LogSink.Named("second");
        LogSink[] sinks = new LogSink[] { first };

        pipeline.SetSinks(sinks);
        sinks[0] = second;

        Assert.That(pipeline.Sinks[0], Is.SameAs(first));
    }

    [Test]
    public void Write_SkipsSinksDisabledByThresholdButWritesEligibleSinks()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink infoSink = LogSink.Named("info");
        LogSink errorSink = LogSink.Named("error");
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);

        errorSink.SetMinimumLevel(LogLevel.Error);
        pipeline.SetSinks(new LogSink[] { infoSink, errorSink });
        LogResult result = pipeline.Write(infoEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DroppedCount, Is.EqualTo(1));
        Assert.That(infoSink.WrittenCount, Is.EqualTo(1));
        Assert.That(errorSink.WrittenCount, Is.EqualTo(0));
    }

    [Test]
    public void Write_ReturnsDroppedWhenEverySinkSkipsEvent()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink sink = LogSink.Named("error");
        LogEvent infoEvent = LogEvent.Create(LogLevel.Info, "Application", "info", "info", new LogField[0]);

        sink.SetMinimumLevel(LogLevel.Error);
        pipeline.AddSink(sink);
        LogResult result = pipeline.Write(infoEvent);

        Assert.That(result.IsDropped, Is.True);
        Assert.That(sink.WrittenCount, Is.EqualTo(0));
    }

    [Test]
    public void IsEnabled_ReturnsTrueWhenAnySinkCanAcceptLevel()
    {
        LogPipeline pipeline = new LogPipeline();
        LogSink warnSink = LogSink.Named("warn");
        LogSink errorSink = LogSink.Named("error");

        warnSink.SetMinimumLevel(LogLevel.Warn);
        errorSink.SetMinimumLevel(LogLevel.Error);
        pipeline.SetSinks(new LogSink[] { warnSink, errorSink });

        Assert.That(pipeline.IsEnabled(LogLevel.Info), Is.False);
        Assert.That(pipeline.IsEnabled(LogLevel.Warn), Is.True);
        Assert.That(pipeline.IsEnabled(LogLevel.Error), Is.True);
    }

    [Test]
    public void Write_FansOutToDirectSinksAndSubPipelines()
    {
        LogPipeline root = new LogPipeline();
        LogPipeline branch = new LogPipeline();
        LogSink direct = LogSink.Named("direct");
        LogSink nested = LogSink.Named("nested");
        LogEvent logEvent = LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]);

        branch.AddSink(nested);
        root.AddSink(direct);
        root.AddPipeline("branch", branch);
        LogResult result = root.Write(logEvent);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(direct.LastEvent, Is.SameAs(logEvent));
        Assert.That(nested.LastEvent, Is.SameAs(logEvent));
        Assert.That(branch.LastEvent, Is.SameAs(logEvent));
        Assert.That(root.WrittenCount, Is.EqualTo(1));
        Assert.That(branch.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void Write_PropagatesSubPipelineFailure()
    {
        LogPipeline root = new LogPipeline();
        LogPipeline branch = new LogPipeline();
        LogSink nested = LogSink.Named("nested");
        LogEvent logEvent = LogEvent.Create(LogLevel.Error, "Application", "message", "message", new LogField[0]);

        nested.SetResult(LogResult.FailureCode("nested.failure", "nested failed"));
        branch.AddSink(nested);
        root.AddPipeline("branch", branch);
        LogResult result = root.Write(logEvent);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("nested.failure"));
        Assert.That(nested.WrittenCount, Is.EqualTo(1));
    }

    [Test]
    public void IsEnabled_ChecksNestedPipelineSinks()
    {
        LogPipeline root = new LogPipeline();
        LogPipeline branch = new LogPipeline();
        LogSink nested = LogSink.Named("nested");

        nested.SetMinimumLevel(LogLevel.Error);
        branch.AddSink(nested);
        root.AddPipeline("branch", branch);

        Assert.That(root.IsEnabled(LogLevel.Warn), Is.False);
        Assert.That(root.IsEnabled(LogLevel.Error), Is.True);
    }
}
