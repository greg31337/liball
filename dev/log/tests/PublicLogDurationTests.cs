using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicLogDurationTests
{
    [Test]
    public void Complete_UsesInjectedTimeProviderAndEmitsOnlyOnce()
    {
        LogFactory factory = new LogFactory();
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        LogSink sink = ((ILogFactory)factory).CreateMemorySink("memory", 0);
        ((ILoggerPipeline)logger).AddSink(sink);
        ManualTimeProvider timeProvider = new ManualTimeProvider(1000);
        object?[] values = new object?[] { "original" };
        ILogDuration duration = ((ILoggerDuration)logger).BeginDuration(LogLevel.Info, "Completed {Operation}", values, "Elapsed", timeProvider);
        values[0] = "changed";

        Assert.That(duration.GetIsCompleted(), Is.False);
        Assert.That(duration.GetElapsed(), Is.EqualTo(TimeSpan.Zero));

        timeProvider.Advance(TimeSpan.FromMilliseconds(275));
        LogResult firstResult = duration.Complete();
        LogResult secondResult = duration.Complete();
        LogEvent[] events = ((ILogFactory)factory).GetMemorySinkEvents(sink);

        Assert.That(firstResult.IsSuccess, Is.True);
        Assert.That(secondResult, Is.SameAs(firstResult));
        Assert.That(duration.GetResult(), Is.SameAs(firstResult));
        Assert.That(duration.GetIsCompleted(), Is.True);
        Assert.That(duration.GetElapsed(), Is.EqualTo(TimeSpan.FromMilliseconds(275)));
        Assert.That(duration.GetFieldName(), Is.EqualTo("Elapsed"));
        Assert.That(timeProvider.GetTimestampCallCount, Is.EqualTo(2));
        Assert.That(events, Has.Length.EqualTo(1));
        Assert.That(logger.GetLastEvent(), Is.SameAs(events[0]));
        Assert.That(events[0].RenderedMessage, Is.EqualTo("Completed original"));
        Assert.That(FindField(events[0], "Elapsed").Value.Kind, Is.EqualTo(LogValueKind.Duration));
        Assert.That(FindField(events[0], "Elapsed").Value.DurationValue, Is.EqualTo(TimeSpan.FromMilliseconds(275)));
    }

    [Test]
    public void CompletionEvent_UsesContextFiltersLevelsAndSubPipelineRouting()
    {
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;
        Logger logger = ((ILoggerFactory)factory).CreateNamedLoggerWithLevel("Application.Worker", LogLevel.Trace);
        ILoggerPipeline pipeline = logger;
        LogSink directSink = componentFactory.CreateMemorySink("direct", 0);
        LogSink branchSink = componentFactory.CreateMemorySink("branch", 0);
        LogSinkPipeline branch = componentFactory.CreateSinkPipeline("duration-branch");
        branch.AddSink(branchSink);
        pipeline.AddSink(directSink);
        LogSink route = pipeline.InstallSubPipeline(branch);
        route.SetMinimumLevel(LogLevel.Warn);
        route.AddFilter(componentFactory.CreateFieldExistsFilter("TenantId"));
        logger.AddFilter(componentFactory.CreateFieldExistsFilter("Elapsed"));
        Logger contextual = logger.WithField("TenantId", "tenant-1");
        ManualTimeProvider timeProvider = new ManualTimeProvider(1000000);

        LogScope scope = contextual.BeginScopeField("RequestId", "request-1");
        ILogDuration duration = ((ILoggerDuration)contextual).BeginDuration(LogLevel.Warn, "Processed {Count}", new object?[] { 3 }, "Elapsed", timeProvider);
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        LogResult result = duration.Complete();
        scope.Dispose();

        LogEvent[] directEvents = componentFactory.GetMemorySinkEvents(directSink);
        LogEvent[] branchEvents = componentFactory.GetMemorySinkEvents(branchSink);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(directEvents, Has.Length.EqualTo(1));
        Assert.That(branchEvents, Has.Length.EqualTo(1));
        Assert.That(branchEvents[0], Is.SameAs(directEvents[0]));
        Assert.That(directEvents[0].Level, Is.EqualTo(LogLevel.Warn));
        Assert.That(directEvents[0].LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(directEvents[0].RenderedMessage, Is.EqualTo("Processed 3"));
        Assert.That(FindField(directEvents[0], "TenantId").Value.TextValue, Is.EqualTo("tenant-1"));
        Assert.That(FindField(directEvents[0], "RequestId").Value.TextValue, Is.EqualTo("request-1"));
        Assert.That(FindField(directEvents[0], "Count").Value.IntegerValue, Is.EqualTo(3));
        Assert.That(FindField(directEvents[0], "Elapsed").Value.DurationValue, Is.EqualTo(TimeSpan.FromSeconds(2)));
    }

    [Test]
    public void Complete_UsesNormalDisabledLevelResultWithoutWriting()
    {
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Error);
        LogSink sink = componentFactory.CreateMemorySink("memory", 0);
        ((ILoggerPipeline)logger).AddSink(sink);
        ManualTimeProvider timeProvider = new ManualTimeProvider(1000);
        ILogDuration duration = ((ILoggerDuration)logger).BeginDuration(LogLevel.Info, "ignored", null, null, timeProvider);
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        LogResult result = duration.Complete();

        Assert.That(result.IsDropped, Is.True);
        Assert.That(duration.GetIsCompleted(), Is.True);
        Assert.That(duration.GetFieldName(), Is.EqualTo("Duration"));
        Assert.That(duration.GetElapsed(), Is.EqualTo(TimeSpan.FromSeconds(1)));
        Assert.That(componentFactory.GetMemorySinkEvents(sink), Is.Empty);
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

    class ManualTimeProvider : TimeProvider
    {
        readonly long frequency;
        long timestamp;
        public int GetTimestampCallCount;

        public ManualTimeProvider(long timestampFrequency)
        {
            frequency = timestampFrequency;
            timestamp = 0;
            GetTimestampCallCount = 0;
        }

        public override long TimestampFrequency
        {
            get
            {
                return frequency;
            }
        }

        public override long GetTimestamp()
        {
            GetTimestampCallCount = GetTimestampCallCount + 1;
            return timestamp;
        }

        public void Advance(TimeSpan elapsed)
        {
            timestamp = timestamp + (long)(elapsed.TotalSeconds * frequency);
        }
    }
}
