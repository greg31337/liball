using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogFacadeTests
{
    [SetUp]
    public void SetUp()
    {
        Log.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        Log.Reset();
    }

    [Test]
    public void SetLogger_WritesThroughCurrentLogger()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger logger = Logger.RootWithLevel(LogLevel.Trace);
        logger.AddSink(sink);

        Log.SetLogger(logger);
        LogResult result = Log.Info("Processed {Count}", new object[] { 3 });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(Log.Current(), Is.SameAs(logger));
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(sink)[0].RenderedMessage, Is.EqualTo("Processed 3"));
        Assert.That(MemoryLogSink.Events(sink)[0].Fields[0].Name, Is.EqualTo("Count"));
        Assert.That(MemoryLogSink.Events(sink)[0].Fields[0].Value.IntegerValue, Is.EqualTo(3));
    }

    [Test]
    public void Configure_CreatesCurrentLoggerFromConfiguration()
    {
        LogSink sink = MemoryLogSink.Create();
        LogConfiguration configuration = LogConfiguration.Create()
            .Name("Application.Global")
            .MinimumLevel(LogLevel.Warn)
            .AddSink(sink);

        Logger logger = Log.Configure(configuration);
        LogResult debugResult = Log.Debug("debug");
        LogResult warnResult = Log.Warn("warn");

        Assert.That(logger, Is.SameAs(Log.Current()));
        Assert.That(Log.Current().Name, Is.EqualTo("Application.Global"));
        Assert.That(debugResult.IsDropped, Is.True);
        Assert.That(warnResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
    }

    [Test]
    public void ConfigureJson_UsesJsonConfigurationLoader()
    {
        string json = """
        {
          "name": "Application.JsonGlobal",
          "minimumLevel": "Debug",
          "fields": { "TenantId": "tenant-1" },
          "sinks": [ "memory" ]
        }
        """;

        Logger logger = Log.ConfigureJson(json);
        LogResult result = Log.Debug("debug");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logger.Name, Is.EqualTo("Application.JsonGlobal"));
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(logger.LastEvent.Fields[0].Value.TextValue, Is.EqualTo("tenant-1"));
    }

    [Test]
    public void ForAndForTypeName_CreateChildLoggersFromCurrentLogger()
    {
        LogSink sink = MemoryLogSink.Create();
        Log.Configure(LogConfiguration.Create().MinimumLevel(LogLevel.Trace).AddSink(sink));

        Logger child = Log.For("Application.Worker");
        Logger typed = Log.ForTypeName("Application.Job");
        child.Info("child");
        typed.Warn("typed");

        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(2));
        Assert.That(MemoryLogSink.Events(sink)[0].LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(MemoryLogSink.Events(sink)[1].LoggerName, Is.EqualTo("Application.Job"));
    }

    [Test]
    public void WithFieldAndBeginScope_AddContextToGlobalWrites()
    {
        LogSink sink = MemoryLogSink.Create();
        Log.Configure(LogConfiguration.Create().MinimumLevel(LogLevel.Trace).AddSink(sink));

        Logger bound = Log.WithField("TenantId", "tenant-1");
        bound.Info("bound");
        LogScope scope = Log.BeginScope("RequestId", "request-1");
        Log.Info("scoped");
        scope.Dispose();

        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(2));
        Assert.That(MemoryLogSink.Events(sink)[0].Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(MemoryLogSink.Events(sink)[0].Fields[0].Value.TextValue, Is.EqualTo("tenant-1"));
        Assert.That(MemoryLogSink.Events(sink)[1].Fields[0].Name, Is.EqualTo("RequestId"));
        Assert.That(MemoryLogSink.Events(sink)[1].Fields[0].Value.TextValue, Is.EqualTo("request-1"));
    }

    [Test]
    public void CategoryAndLifecycleMethods_DelegateToCurrentLogger()
    {
        LogSink sink = MemoryLogSink.Create();
        Log.Configure(LogConfiguration.Create().MinimumLevel(LogLevel.Trace).AddSink(sink));

        Log.SetCategoryLevel("Application", LogLevel.Error);
        LogResult warnResult = Log.For("Application.Worker").Warn("warn");
        LogResult errorResult = Log.For("Application.Worker").Error("error");
        LogPipelineHealth health = Log.Health();
        LogResult flushResult = Log.Flush();
        LogResult drainResult = Log.Drain();

        Assert.That(warnResult.IsDropped, Is.True);
        Assert.That(errorResult.IsSuccess, Is.True);
        Assert.That(health.Sinks.Length, Is.EqualTo(1));
        Assert.That(flushResult.IsSuccess, Is.True);
        Assert.That(drainResult.IsSuccess, Is.True);
    }

    [Test]
    public void ResetAndNoOp_ReplaceCurrentLogger()
    {
        Log.NoOp();
        LogResult disabledResult = Log.Fatal("fatal");

        Logger reset = Log.Reset();

        Assert.That(disabledResult.IsDropped, Is.True);
        Assert.That(reset.IsNoOp, Is.False);
        Assert.That(Log.Current().GetMinimumLevel(), Is.EqualTo(LogLevel.Info));
    }
}
