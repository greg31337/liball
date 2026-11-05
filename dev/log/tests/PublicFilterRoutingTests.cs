using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicFilterRoutingTests
{
    [Test]
    public void BuiltInFilterFactories_ApplyLevelCategoryAndFieldRules()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent info = logFactory.CreateEvent(LogLevel.Info, "Application.Worker", "info", "info", new LogField[]
        {
            factory.CreateStringField("TenantId", "tenant-1"),
            factory.CreateArrayField("Roles", new object?[] { "reader", 2 })
        });
        LogEvent error = logFactory.CreateEvent(LogLevel.Error, "Application.Worker.Job", "error", "error", info.Fields);
        LogEvent sibling = logFactory.CreateEvent(LogLevel.Info, "ApplicationHost", "sibling", "sibling", new LogField[0]);

        LogFilter minimum = logFactory.CreateMinimumLevelFilter(LogLevel.Warn);
        LogFilter category = logFactory.CreateCategoryFilter("Application.Worker", LogLevel.Error);
        LogFilter exists = logFactory.CreateFieldExistsFilter("TenantId");
        LogFilter equals = logFactory.CreateFieldEqualsFilter("TenantId", "tenant-1");
        LogFilter structuredEquals = logFactory.CreateStructuredFieldEqualsFilter(
            "Roles",
            factory.CreateArrayValue(new object?[] { "reader", 2 }));

        Assert.That(minimum.Decide(info), Is.EqualTo(LogDecision.Exclude));
        Assert.That(minimum.Decide(error), Is.EqualTo(LogDecision.Include));
        Assert.That(category.Decide(info), Is.EqualTo(LogDecision.Exclude));
        Assert.That(category.Decide(error), Is.EqualTo(LogDecision.Include));
        Assert.That(category.Decide(sibling), Is.EqualTo(LogDecision.Neutral));
        Assert.That(exists.Decide(info), Is.EqualTo(LogDecision.Include));
        Assert.That(exists.Decide(sibling), Is.EqualTo(LogDecision.Exclude));
        Assert.That(equals.Decide(info), Is.EqualTo(LogDecision.Include));
        Assert.That(logFactory.CreateFieldEqualsFilter("TenantId", "tenant-2").Decide(info), Is.EqualTo(LogDecision.Exclude));
        Assert.That(structuredEquals.Decide(info), Is.EqualTo(LogDecision.Include));
        Assert.That(logFactory.CreateStructuredFieldEqualsFilter("Roles", factory.CreateArrayValue(new object?[] { "writer" })).Decide(info), Is.EqualTo(LogDecision.Exclude));
    }

    [Test]
    public void SamplingFilter_SharesItsCounterAcrossChildrenAndFanoutSinks()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        Logger root = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        Logger worker = ((ILoggerContext)root).For("Application.Worker");
        Logger scheduler = ((ILoggerContext)root).For("Application.Scheduler");
        LogSink firstSink = logFactory.CreateMemorySink("first", 0);
        LogSink secondSink = logFactory.CreateMemorySink("second", 0);
        ILoggerPipeline pipeline = root;
        pipeline.AddSink(firstSink);
        pipeline.AddSink(secondSink);
        pipeline.AddFilter(logFactory.CreateSamplingFilter(2));

        LogResult first = worker.WriteWithValues(LogLevel.Info, "first", null);
        LogResult second = scheduler.WriteWithValues(LogLevel.Info, "second", null);
        LogResult third = worker.WriteWithValues(LogLevel.Info, "third", null);
        LogResult fourth = scheduler.WriteWithValues(LogLevel.Info, "fourth", null);

        Assert.That(first.IsDropped, Is.True);
        Assert.That(second.IsSuccess, Is.True);
        Assert.That(third.IsDropped, Is.True);
        Assert.That(fourth.IsSuccess, Is.True);
        Assert.That(logFactory.GetMemorySinkEvents(firstSink), Has.Length.EqualTo(2));
        Assert.That(logFactory.GetMemorySinkEvents(secondSink), Has.Length.EqualTo(2));
        Assert.That(logFactory.GetMemorySinkEvents(firstSink)[0].RenderedMessage, Is.EqualTo("second"));
        Assert.That(logFactory.GetMemorySinkEvents(firstSink)[1].RenderedMessage, Is.EqualTo("fourth"));
        Assert.That(logFactory.GetMemorySinkEvents(secondSink)[0], Is.SameAs(logFactory.GetMemorySinkEvents(firstSink)[0]));
    }

    [Test]
    public void RateLimitFactories_EnforceCountsAndClockedWindowReset()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        LogEvent logEvent = logFactory.CreateEvent(LogLevel.Info, "Application", "message", "message", new LogField[0]);
        LogFilter oneSecond = logFactory.CreateRateLimitFilter(1);
        LogFilter windowed = logFactory.CreateWindowedRateLimitFilter(2, TimeSpan.FromHours(1));
        DateTimeOffset start = new DateTimeOffset(2026, 8, 25, 14, 0, 0, TimeSpan.Zero);
        MutableClock clock = new MutableClock(start);
        LogFilter clocked = logFactory.CreateClockedRateLimitFilter(1, TimeSpan.FromSeconds(10), clock);

        Assert.That(oneSecond.Decide(logEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(oneSecond.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));
        Assert.That(windowed.Decide(logEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(windowed.Decide(logEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(windowed.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));
        Assert.That(clocked.Decide(logEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(clocked.Decide(logEvent), Is.EqualTo(LogDecision.Exclude));

        clock.Current = start.AddSeconds(10);

        Assert.That(clocked.Decide(logEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(clock.CallCount, Is.EqualTo(3));
    }

    [Test]
    public void InterfaceFilters_RunAtLoggerSinkAndWrappedBoundaries()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        LogSink selectiveSink = logFactory.CreateMemorySink("selective", 0);
        LogSink fanoutSink = logFactory.CreateMemorySink("fanout", 0);
        PrefixFilter loggerFilter = new PrefixFilter("keep");
        PrefixFilter sinkFilter = new PrefixFilter("keep route");
        PrefixFilter wrappedFilter = new PrefixFilter("wrapped");
        ILoggerPipeline pipeline = logger;
        pipeline.AddInterfaceFilter(loggerFilter);
        selectiveSink.AddInterfaceFilter(sinkFilter);
        pipeline.AddSink(selectiveSink);
        pipeline.AddSink(fanoutSink);

        LogResult droppedByLogger = logger.WriteWithValues(LogLevel.Info, "drop", null);
        LogResult droppedByOneSink = logger.WriteWithValues(LogLevel.Info, "keep general", null);
        LogResult accepted = logger.WriteWithValues(LogLevel.Info, "keep route event", null);
        LogFilter adapter = logFactory.WrapFilter(wrappedFilter);
        LogEvent wrappedEvent = logFactory.CreateEvent(LogLevel.Info, "Application", "wrapped", "wrapped event", new LogField[0]);

        Assert.That(droppedByLogger.IsDropped, Is.True);
        Assert.That(droppedByOneSink.IsSuccess, Is.True);
        Assert.That(accepted.IsSuccess, Is.True);
        Assert.That(logFactory.GetMemorySinkEvents(selectiveSink), Has.Length.EqualTo(1));
        Assert.That(logFactory.GetMemorySinkEvents(fanoutSink), Has.Length.EqualTo(2));
        Assert.That(loggerFilter.DecisionCount, Is.EqualTo(3));
        Assert.That(sinkFilter.DecisionCount, Is.EqualTo(2));
        Assert.That(adapter.Decide(wrappedEvent), Is.EqualTo(LogDecision.Include));
        Assert.That(wrappedFilter.DecisionCount, Is.EqualTo(1));
    }

    [Test]
    public void LoggerCategoryAndSinkThresholds_ComposeBeforeFanoutRouting()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        Logger root = ((ILoggerFactory)factory).CreateNamedLoggerWithLevel("Application", LogLevel.Info);
        Logger worker = ((ILoggerContext)root).For("Worker.Job");
        LogSink auditSink = logFactory.CreateMemorySink("audit", 0);
        LogSink errorSink = logFactory.CreateMemorySink("errors", 0);
        errorSink.SetMinimumLevel(LogLevel.Error);
        ILoggerPipeline pipeline = root;
        pipeline.AddSink(auditSink);
        pipeline.AddSink(errorSink);

        LogResult belowLogger = root.WriteWithValues(LogLevel.Debug, "debug", null);
        LogResult info = root.WriteWithValues(LogLevel.Info, "info", null);
        LogResult error = root.WriteWithValues(LogLevel.Error, "error", null);
        pipeline.SetSinkMinimumLevel(LogLevel.Fatal);
        LogResult belowCommonSink = root.WriteWithValues(LogLevel.Error, "globally disabled", null);
        pipeline.ClearSinkMinimumLevel();
        LogResult warn = root.WriteWithValues(LogLevel.Warn, "warn", null);
        LogResult categoryResult = pipeline.SetCategoryLevel("Application.Worker", LogLevel.Error);
        LogResult belowCategory = worker.WriteWithValues(LogLevel.Warn, "worker warn", null);
        LogResult workerError = worker.WriteWithValues(LogLevel.Error, "worker error", null);

        Assert.That(belowLogger.IsDropped, Is.True);
        Assert.That(info.IsSuccess, Is.True);
        Assert.That(error.IsSuccess, Is.True);
        Assert.That(belowCommonSink.IsDropped, Is.True);
        Assert.That(warn.IsSuccess, Is.True);
        Assert.That(categoryResult.IsSuccess, Is.True);
        Assert.That(belowCategory.IsDropped, Is.True);
        Assert.That(workerError.IsSuccess, Is.True);
        Assert.That(logFactory.GetMemorySinkEvents(auditSink), Has.Length.EqualTo(4));
        Assert.That(logFactory.GetMemorySinkEvents(errorSink), Has.Length.EqualTo(2));
        Assert.That(logFactory.GetMemorySinkEvents(errorSink)[0].RenderedMessage, Is.EqualTo("error"));
        Assert.That(logFactory.GetMemorySinkEvents(errorSink)[1].LoggerName, Is.EqualTo("Application.Worker.Job"));
    }

    [Test]
    public void NestedSubPipelines_ApplyRoutingRulesAtEveryBranch()
    {
        LogFactory factory = new LogFactory();
        ILogFactory logFactory = factory;
        Logger root = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        LogSink directSink = logFactory.CreateMemorySink("direct", 0);
        LogSink outerSink = logFactory.CreateMemorySink("outer", 0);
        LogSink innerSink = logFactory.CreateMemorySink("inner", 0);
        LogSinkPipeline outer = logFactory.CreateSinkPipeline("outer-branch");
        LogSinkPipeline inner = logFactory.CreateSinkPipeline("inner-branch");
        outer.AddSink(outerSink);
        inner.AddSink(innerSink);
        LogSink innerRoute = outer.InstallSubPipeline(inner);
        innerRoute.SetMinimumLevel(LogLevel.Error);
        innerRoute.AddFilter(logFactory.CreateStructuredFieldEqualsFilter("Route", factory.CreateStringValue("inner")));
        ILoggerPipeline pipeline = root;
        pipeline.AddSink(directSink);
        LogSink outerRoute = pipeline.InstallSubPipeline(outer);
        outerRoute.AddFilter(logFactory.CreateFieldExistsFilter("Route"));

        root.WriteWithValues(LogLevel.Info, "plain", null);
        root.WithField("Route", "outer").WriteWithValues(LogLevel.Info, "outer", null);
        root.WithField("Route", "inner").WriteWithValues(LogLevel.Info, "inner info", null);
        root.WithField("Route", "inner").WriteWithValues(LogLevel.Error, "inner error", null);

        Assert.That(logFactory.GetMemorySinkEvents(directSink), Has.Length.EqualTo(4));
        Assert.That(logFactory.GetMemorySinkEvents(outerSink), Has.Length.EqualTo(3));
        Assert.That(logFactory.GetMemorySinkEvents(innerSink), Has.Length.EqualTo(1));
        Assert.That(logFactory.GetMemorySinkEvents(innerSink)[0].RenderedMessage, Is.EqualTo("inner error"));
        Assert.That(pipeline.GetSubPipelines(), Is.EqualTo(new LogSinkPipeline[] { outer }));
        Assert.That(outer.GetSubPipelines(), Is.EqualTo(new LogSinkPipeline[] { inner }));
    }

    private sealed class PrefixFilter : ILogFilter
    {
        public string Prefix;
        public int DecisionCount;

        public PrefixFilter(string prefix)
        {
            Prefix = prefix;
            DecisionCount = 0;
        }

        public LogDecision Decide(LogEvent? logEvent)
        {
            DecisionCount = DecisionCount + 1;
            if (logEvent == null)
            {
                return LogDecision.Neutral;
            }

            if (logEvent.RenderedMessage.StartsWith(Prefix))
            {
                return LogDecision.Include;
            }

            return LogDecision.Exclude;
        }
    }

    private sealed class MutableClock : ILogClock
    {
        public DateTimeOffset Current;
        public int CallCount;

        public MutableClock(DateTimeOffset current)
        {
            Current = current;
            CallCount = 0;
        }

        public DateTimeOffset UtcNow()
        {
            CallCount = CallCount + 1;
            return Current;
        }
    }
}
