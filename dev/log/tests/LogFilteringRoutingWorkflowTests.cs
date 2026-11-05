using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogFilteringRoutingWorkflowTests
{
    [Test]
    public void LoggerFilter_DropsEventBeforeAnySinkReceivesIt()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink firstSink = MemoryLogSink.Named("first");
        LogSink secondSink = MemoryLogSink.Named("second");

        logger.AddSink(firstSink);
        logger.AddSink(secondSink);
        logger.AddFilter(LogFilter.FieldExists("TenantId"));

        Logger tenantLogger = logger.ForField("TenantId", "tenant-1");

        LogResult missingResult = logger.Info("missing tenant");
        LogResult presentResult = tenantLogger.Info("present tenant");

        Assert.That(missingResult.IsDropped, Is.True);
        Assert.That(presentResult.IsSuccess, Is.True);
        Assert.That(logger.WrittenCount, Is.EqualTo(0));
        Assert.That(tenantLogger.WrittenCount, Is.EqualTo(1));
        Assert.That(MemoryLogSink.Count(firstSink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Count(secondSink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(firstSink)[0].RenderedMessage, Is.EqualTo("present tenant"));
        Assert.That(MemoryLogSink.Events(secondSink)[0].RenderedMessage, Is.EqualTo("present tenant"));
    }

    [Test]
    public void SamplingFilter_UsesOneCounterAcrossChildLoggersAndFanout()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger worker = root.For("Application.Worker");
        Logger scheduler = root.For("Application.Scheduler");
        LogSink firstSink = MemoryLogSink.Named("first");
        LogSink secondSink = MemoryLogSink.Named("second");

        root.AddSink(firstSink);
        root.AddSink(secondSink);
        root.AddFilter(LogFilter.SampleEvery(2));

        LogResult firstResult = worker.Info("first");
        LogResult secondResult = scheduler.Info("second");
        LogResult thirdResult = worker.Info("third");
        LogResult fourthResult = scheduler.Info("fourth");

        Assert.That(firstResult.IsDropped, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(thirdResult.IsDropped, Is.True);
        Assert.That(fourthResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(firstSink), Is.EqualTo(2));
        Assert.That(MemoryLogSink.Count(secondSink), Is.EqualTo(2));
        Assert.That(MemoryLogSink.Events(firstSink)[0].RenderedMessage, Is.EqualTo("second"));
        Assert.That(MemoryLogSink.Events(firstSink)[1].RenderedMessage, Is.EqualTo("fourth"));
        Assert.That(MemoryLogSink.Events(secondSink)[0].LoggerName, Is.EqualTo("Application.Scheduler"));
    }

    [Test]
    public void CategoryRouting_UsesMostSpecificLevelBeforeWritingEvents()
    {
        Logger root = Logger.RootWithLevel(LogLevel.Trace);
        Logger application = root.For("Application.Service");
        Logger worker = root.For("Application.Worker.Job");
        Logger other = root.For("Other");
        LogSink sink = MemoryLogSink.Create();

        root.AddSink(sink);
        root.SetCategoryLevel("Application", LogLevel.Warn);
        root.SetCategoryLevel("Application.Worker", LogLevel.Error);

        LogResult applicationInfo = application.Info("application info");
        LogResult applicationWarn = application.Warn("application warn");
        LogResult workerWarn = worker.Warn("worker warn");
        LogResult workerError = worker.Error("worker error");
        LogResult otherDebug = other.Debug("other debug");
        LogEvent[] events = MemoryLogSink.Events(sink);

        Assert.That(applicationInfo.IsDropped, Is.True);
        Assert.That(applicationWarn.IsSuccess, Is.True);
        Assert.That(workerWarn.IsDropped, Is.True);
        Assert.That(workerError.IsSuccess, Is.True);
        Assert.That(otherDebug.IsSuccess, Is.True);
        Assert.That(events.Length, Is.EqualTo(3));
        Assert.That(events[0].LoggerName, Is.EqualTo("Application.Service"));
        Assert.That(events[0].Level, Is.EqualTo(LogLevel.Warn));
        Assert.That(events[1].LoggerName, Is.EqualTo("Application.Worker.Job"));
        Assert.That(events[1].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(events[2].LoggerName, Is.EqualTo("Other"));
        Assert.That(events[2].Level, Is.EqualTo(LogLevel.Debug));
    }

    [Test]
    public void PerSinkThresholdsAndFilters_RouteEachAcceptedEventIndependently()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink auditSink = MemoryLogSink.Named("audit");
        LogSink errorSink = MemoryLogSink.Named("errors");
        LogSink tenantSink = MemoryLogSink.Named("tenant");

        errorSink.SetMinimumLevel(LogLevel.Error);
        tenantSink.AddFilter(LogFilter.FieldEquals("TenantId", "tenant-1"));
        logger.AddSink(auditSink);
        logger.AddSink(errorSink);
        logger.AddSink(tenantSink);

        LogResult tenantInfo = logger.ForField("TenantId", "tenant-1").Info("tenant info");
        LogResult otherInfo = logger.ForField("TenantId", "tenant-2").Info("other info");
        LogResult error = logger.Error("error");

        Assert.That(tenantInfo.IsSuccess, Is.True);
        Assert.That(otherInfo.IsSuccess, Is.True);
        Assert.That(error.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(auditSink), Is.EqualTo(3));
        Assert.That(MemoryLogSink.Count(errorSink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Count(tenantSink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(auditSink)[0].RenderedMessage, Is.EqualTo("tenant info"));
        Assert.That(MemoryLogSink.Events(auditSink)[1].RenderedMessage, Is.EqualTo("other info"));
        Assert.That(MemoryLogSink.Events(auditSink)[2].RenderedMessage, Is.EqualTo("error"));
        Assert.That(MemoryLogSink.Events(errorSink)[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(MemoryLogSink.Events(tenantSink)[0].Fields[0].Value.TextValue, Is.EqualTo("tenant-1"));
    }

    [Test]
    public void GlobalSinkThreshold_DisablesWritesBeforePerSinkThresholdsCanAccept()
    {
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        LogSink sink = MemoryLogSink.Create();

        sink.SetMinimumLevel(LogLevel.Trace);
        logger.AddSink(sink);
        logger.SetSinkMinimumLevel(LogLevel.Error);

        LogResult infoResult = logger.Info("info");
        LogResult errorResult = logger.Error("error");

        Assert.That(infoResult.IsDropped, Is.True);
        Assert.That(errorResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(sink)[0].RenderedMessage, Is.EqualTo("error"));
    }
}
