using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class OverflowCheckedArithmeticTests
{
    [Test]
    public void AddCounts_SaturatesAndNormalizesCounts()
    {
        Assert.That(LogResult.AddCounts(int.MaxValue, 1), Is.EqualTo(int.MaxValue));
        Assert.That(LogResult.AddCounts(int.MaxValue - 1, 2), Is.EqualTo(int.MaxValue));
        Assert.That(LogResult.AddCounts(-1, 2), Is.EqualTo(2));
    }

    [Test]
    public void Combine_SaturatesResultCounts()
    {
        LogResult maximum = LogResult.DroppedCountResult("maximum", int.MaxValue);
        LogResult additional = LogResult.Dropped("additional");

        LogResult combined = LogResult.Combine(new LogResult[] { maximum, additional });

        Assert.That(combined.DroppedCount, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void LoggingCounters_SaturateAtMaximumValue()
    {
        LogSink sink = NullLogSink.Create();
        sink.WrittenCount = int.MaxValue;
        LogPipeline pipeline = new LogPipeline();
        pipeline.WrittenCount = int.MaxValue;
        pipeline.AddSink(sink);
        Logger logger = Logger.Root();
        logger.WrittenCount = int.MaxValue;
        logger.Pipeline = pipeline;

        LogResult result = logger.Info("message");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(sink.WrittenCount, Is.EqualTo(int.MaxValue));
        Assert.That(pipeline.WrittenCount, Is.EqualTo(int.MaxValue));
        Assert.That(logger.WrittenCount, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void ResultCounters_SaturateAtMaximumValue()
    {
        LogSink sink = NullLogSink.Create();
        sink.IsEnabled = false;
        sink.DroppedCount = int.MaxValue;
        LogPipeline pipeline = new LogPipeline();
        pipeline.DroppedCount = int.MaxValue;
        pipeline.AddSink(sink);

        LogResult result = pipeline.Write(LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));

        Assert.That(result.IsDropped, Is.True);
        Assert.That(sink.DroppedCount, Is.EqualTo(int.MaxValue));
        Assert.That(pipeline.DroppedCount, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void SampleEvery_RestartsItsCycleAtMaximumCount()
    {
        LogFilter filter = LogFilter.SampleEvery(int.MaxValue);
        filter.SeenCount = int.MaxValue - 1;

        Assert.That(filter.DecideSampleEvery(), Is.EqualTo(LogDecision.Include));
        Assert.That(filter.SeenCount, Is.EqualTo(int.MaxValue));
        Assert.That(filter.DecideSampleEvery(), Is.EqualTo(LogDecision.Exclude));
        Assert.That(filter.SeenCount, Is.EqualTo(1));
    }

    [Test]
    public void RateLimit_ExcludesAfterMaximumCountWithoutOverflowing()
    {
        LogFilter filter = LogFilter.RateLimit(int.MaxValue);
        filter.WindowStartedAt = filter.Clock.UtcNow();
        filter.SeenCount = int.MaxValue - 1;

        Assert.That(filter.DecideRateLimit(), Is.EqualTo(LogDecision.Include));
        Assert.That(filter.SeenCount, Is.EqualTo(int.MaxValue));
        Assert.That(filter.DecideRateLimit(), Is.EqualTo(LogDecision.Exclude));
        Assert.That(filter.SeenCount, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void CircuitBreakerFailureCount_SaturatesAtMaximumValue()
    {
        LogSink sink = CircuitBreakerLogSink.WithThreshold(null, int.MaxValue);
        sink.CircuitBreakerConsecutiveFailureCount = int.MaxValue;

        LogResult result = CircuitBreakerLogSink.RecordInnerResult(sink, LogResult.Failure("failure"));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(sink.CircuitBreakerConsecutiveFailureCount, Is.EqualTo(int.MaxValue));
        Assert.That(sink.CircuitBreakerIsOpen, Is.True);
    }

    [Test]
    public void RollingSequence_ReturnsFailureWhenSequenceIsExhausted()
    {
        string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "log-tests", System.Guid.NewGuid().ToString("N"));
        string filePath = System.IO.Path.Combine(directory, "events.log");
        System.IO.Directory.CreateDirectory(directory);
        System.IO.File.WriteAllText(filePath, "full");
        LogSink sink = FileLogSink.WithRollingPolicy(filePath, TextLogFormatter.Plain(), true, LogRollingPolicy.BySize(1));
        sink.ActiveFilePath = filePath;
        sink.RollingSequence = int.MaxValue;

        LogResult result = LogRollingPolicy.Apply(sink, LogEvent.Create(LogLevel.Info, "Application", "message", "message", new LogField[0]));

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Code, Is.EqualTo("sink.rolling.sequence.exhausted"));
        Assert.That(sink.RollingSequence, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void TemplateCacheCounters_SaturateAtMaximumValue()
    {
        LogTemplateCache cache = LogTemplateCache.Create(1);
        cache.MissCount = int.MaxValue;
        cache.GetOrAdd("message");
        cache.HitCount = int.MaxValue;

        cache.GetOrAdd("message");

        Assert.That(cache.MissCount, Is.EqualTo(int.MaxValue));
        Assert.That(cache.HitCount, Is.EqualTo(int.MaxValue));
    }
}
