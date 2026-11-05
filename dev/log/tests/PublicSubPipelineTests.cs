using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicSubPipelineTests
{
    [Test]
    public void InstallSubPipeline_FansOutWithSinkSpecificLevelsAndFilters()
    {
        ILogFactory factory = new LogFactory();
        Logger logger = new LogFactory().CreateRootLoggerWithLevel(LogLevel.Trace);
        ILoggerPipeline loggerPipeline = logger;
        LogSink directSink = factory.CreateMemorySink("direct", 0);
        LogSink errorSink = factory.CreateMemorySink("errors", 0);
        LogSink tenantSink = factory.CreateMemorySink("tenants", 0);
        LogSinkPipeline errors = factory.CreateSinkPipeline("error-branch");
        LogSinkPipeline tenants = factory.CreateSinkPipeline("tenant-branch");
        errors.AddSink(errorSink);
        tenants.AddSink(tenantSink);
        loggerPipeline.AddSink(directSink);

        LogSink errorRoute = loggerPipeline.InstallSubPipeline(errors);
        LogSink tenantRoute = loggerPipeline.InstallSubPipeline(tenants);
        errorRoute.SetMinimumLevel(LogLevel.Error);
        tenantRoute.AddFilter(factory.CreateFieldExistsFilter("TenantId"));

        logger.WriteWithValues(LogLevel.Info, "plain", null);
        logger.WriteWithValues(LogLevel.Error, "failed", null);
        LogField tenantField = factory.CreateField("TenantId", "tenant-1");
        LogEvent tenantEvent = factory.CreateEvent(LogLevel.Info, "Application", "tenant", "tenant", new LogField[] { tenantField });
        logger.WriteEvent(tenantEvent);

        Assert.That(factory.GetMemorySinkEvents(directSink), Has.Length.EqualTo(3));
        Assert.That(factory.GetMemorySinkEvents(errorSink), Has.Length.EqualTo(1));
        Assert.That(factory.GetMemorySinkEvents(errorSink)[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(factory.GetMemorySinkEvents(tenantSink), Has.Length.EqualTo(1));
        Assert.That(factory.GetMemorySinkEvents(tenantSink)[0].RenderedMessage, Is.EqualTo("tenant"));
        Assert.That(loggerPipeline.GetSubPipelines(), Is.EqualTo(new LogSinkPipeline[] { errors, tenants }));
        Assert.That(loggerPipeline.GetSubPipelineSinks(), Is.EqualTo(new LogSink[] { errorRoute, tenantRoute }));
        Assert.That(errors.GetSinks(), Is.EqualTo(new LogSink[] { errorSink }));
        Assert.That(errors.IsEnabled(LogLevel.Error), Is.True);
    }

    [Test]
    public void SetAndClearSubPipelines_PreserveDirectSinksAndReturnDetachedSnapshots()
    {
        ILogFactory factory = new LogFactory();
        Logger logger = new LogFactory().CreateRootLoggerWithLevel(LogLevel.Trace);
        ILoggerPipeline loggerPipeline = logger;
        LogSink directSink = factory.CreateMemorySink("direct", 0);
        LogSink firstSink = factory.CreateMemorySink("first", 0);
        LogSink secondSink = factory.CreateMemorySink("second", 0);
        LogSink replacementSink = factory.CreateMemorySink("replacement", 0);
        LogSinkPipeline first = factory.CreateSinkPipeline("first-branch");
        LogSinkPipeline second = factory.CreateSinkPipeline("second-branch");
        LogSinkPipeline replacement = factory.CreateSinkPipeline("replacement-branch");
        first.AddSink(firstSink);
        second.AddSink(secondSink);
        replacement.AddSink(replacementSink);
        loggerPipeline.AddSink(directSink);

        loggerPipeline.SetSubPipelines(new LogSinkPipeline[] { first, second });
        LogSinkPipeline[] snapshot = loggerPipeline.GetSubPipelines();
        snapshot[0] = replacement;
        logger.WriteWithValues(LogLevel.Info, "before", null);

        Assert.That(loggerPipeline.GetSubPipelines(), Is.EqualTo(new LogSinkPipeline[] { first, second }));
        Assert.That(factory.GetMemorySinkEvents(directSink), Has.Length.EqualTo(1));
        Assert.That(factory.GetMemorySinkEvents(firstSink), Has.Length.EqualTo(1));
        Assert.That(factory.GetMemorySinkEvents(secondSink), Has.Length.EqualTo(1));

        loggerPipeline.SetSubPipelines(new LogSinkPipeline[] { replacement });
        logger.WriteWithValues(LogLevel.Info, "after replacement", null);

        Assert.That(loggerPipeline.GetSubPipelines(), Is.EqualTo(new LogSinkPipeline[] { replacement }));
        Assert.That(factory.GetMemorySinkEvents(directSink), Has.Length.EqualTo(2));
        Assert.That(factory.GetMemorySinkEvents(firstSink), Has.Length.EqualTo(1));
        Assert.That(factory.GetMemorySinkEvents(secondSink), Has.Length.EqualTo(1));
        Assert.That(factory.GetMemorySinkEvents(replacementSink), Has.Length.EqualTo(1));

        loggerPipeline.ClearSubPipelines();
        logger.WriteWithValues(LogLevel.Info, "after clear", null);

        Assert.That(loggerPipeline.GetSubPipelines(), Is.Empty);
        Assert.That(loggerPipeline.GetSubPipelineSinks(), Is.Empty);
        Assert.That(factory.GetMemorySinkEvents(directSink), Has.Length.EqualTo(3));
        Assert.That(factory.GetMemorySinkEvents(replacementSink), Has.Length.EqualTo(1));
    }

    [Test]
    public void NestedSubPipelines_AggregateRecursiveLifecycleOperations()
    {
        ILogFactory factory = new LogFactory();
        Logger logger = new LogFactory().CreateRootLoggerWithLevel(LogLevel.Trace);
        ILoggerPipeline loggerPipeline = logger;
        LifecycleSink successfulSink = new LifecycleSink("successful", false);
        LifecycleSink failingSink = new LifecycleSink("failing", true);
        LogSinkPipeline inner = factory.CreateSinkPipeline("inner");
        LogSinkPipeline outer = factory.CreateSinkPipeline("outer");
        inner.AddInterfaceSink(successfulSink);
        inner.AddInterfaceSink(failingSink);
        outer.InstallSubPipeline(inner);
        loggerPipeline.InstallSubPipeline(outer);

        logger.WriteWithValues(LogLevel.Info, "event", null);
        LogResult flushResult = loggerPipeline.Flush();
        LogResult drainResult = loggerPipeline.Drain();
        LogResult closeResult = loggerPipeline.Close();

        Assert.That(successfulSink.WriteCount, Is.EqualTo(1));
        Assert.That(failingSink.WriteCount, Is.EqualTo(1));
        Assert.That(flushResult.IsSuccess, Is.True);
        Assert.That(drainResult.IsSuccess, Is.True);
        Assert.That(closeResult.IsFailure, Is.True);
        Assert.That(successfulSink.FlushCount, Is.EqualTo(1));
        Assert.That(failingSink.FlushCount, Is.EqualTo(1));
        Assert.That(successfulSink.DrainCount, Is.GreaterThan(0));
        Assert.That(failingSink.DrainCount, Is.GreaterThan(0));
        Assert.That(successfulSink.CloseCount, Is.EqualTo(1));
        Assert.That(failingSink.CloseCount, Is.EqualTo(1));
        Assert.That(outer.Health().Name, Is.EqualTo("outer"));
        Assert.That(outer.Health().SinkCount, Is.EqualTo(1));
    }

    [Test]
    public void InstallSubPipeline_RejectsRecursiveComposition()
    {
        ILogFactory factory = new LogFactory();
        LogSinkPipeline parent = factory.CreateSinkPipeline("parent");
        LogSinkPipeline child = factory.CreateSinkPipeline("child");
        parent.InstallSubPipeline(child);

        LogSink rejectedRoute = child.InstallSubPipeline(parent);

        Assert.That(rejectedRoute.GetIsEnabled(), Is.False);
        Assert.That(rejectedRoute.GetLastResult().IsFailure, Is.True);
        Assert.That(rejectedRoute.GetLastResult().Code, Is.EqualTo("pipeline.child.recursive"));
        Assert.That(child.GetSubPipelines(), Is.Empty);
    }

    class LifecycleSink : ILogSink
    {
        readonly string name;
        readonly bool failClose;
        public int WriteCount;
        public int FlushCount;
        public int DrainCount;
        public int CloseCount;

        public LifecycleSink(string nameValue, bool failCloseValue)
        {
            name = nameValue;
            failClose = failCloseValue;
        }

        public string GetName()
        {
            return name;
        }

        public bool IsEnabledForLevel(LogLevel level)
        {
            return true;
        }

        public LogResult Write(LogEvent? logEvent)
        {
            WriteCount = WriteCount + 1;
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Flush()
        {
            FlushCount = FlushCount + 1;
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Drain()
        {
            DrainCount = DrainCount + 1;
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Close()
        {
            CloseCount = CloseCount + 1;
            if (failClose)
            {
                return new LogResult(false, true, false, false, "test.close", "close failed", 0, 1, 0);
            }

            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }
    }
}
