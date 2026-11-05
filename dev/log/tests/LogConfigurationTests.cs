using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogConfigurationTests
{
    [Test]
    public void CreateLogger_UsesConfiguredNameLevelClockAndSink()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);
        LogSink sink = MemoryLogSink.Create();
        Logger logger = LogConfiguration.Create()
            .Name("Application.Worker")
            .MinimumLevel(LogLevel.Debug)
            .Clock(LogClock.Fixed(timestamp))
            .AddSink(sink)
            .CreateLogger();

        LogResult debugResult = logger.Debug("debug");
        LogResult traceResult = logger.Trace("trace");

        Assert.That(logger.Name, Is.EqualTo("Application.Worker"));
        Assert.That(debugResult.IsSuccess, Is.True);
        Assert.That(traceResult.IsDropped, Is.True);
        Assert.That(logger.LastEvent, Is.Not.Null);
        Assert.That(logger.LastEvent!.Timestamp, Is.EqualTo(timestamp));
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
    }

    [Test]
    public void LevelSwitch_UsesSharedRuntimeSwitch()
    {
        LogLevelSwitch levelSwitch = LogLevelSwitch.Create(LogLevel.Error);
        LogSink sink = MemoryLogSink.Create();
        Logger logger = LogConfiguration.Create().LevelSwitch(levelSwitch).AddSink(sink).CreateLogger();

        LogResult firstResult = logger.Warn("warn");
        levelSwitch.SetLevel(LogLevel.Debug);
        LogResult secondResult = logger.Warn("warn");

        Assert.That(firstResult.IsDropped, Is.True);
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
    }

    [Test]
    public void AddFilterAndGlobalField_CreateFilteredLogger()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger logger = LogConfiguration.Create()
            .MinimumLevel(LogLevel.Trace)
            .AddSink(sink)
            .AddFilter(LogFilter.FieldExists("TenantId"))
            .AddGlobalField("TenantId", "tenant-1")
            .CreateLogger();

        LogResult result = logger.Info("message");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(sink)[0].Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(MemoryLogSink.Events(sink)[0].Fields[0].Value.TextValue, Is.EqualTo("tenant-1"));
    }

    [Test]
    public void AddEnricher_AddsStaticField()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger logger = LogConfiguration.Create()
            .MinimumLevel(LogLevel.Trace)
            .AddSink(sink)
            .AddEnricher(LogEnricher.Field("Service", "billing"))
            .CreateLogger();

        LogResult result = logger.Info("message");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Events(sink)[0].Fields[0].Name, Is.EqualTo("Service"));
        Assert.That(MemoryLogSink.Events(sink)[0].Fields[0].Value.TextValue, Is.EqualTo("billing"));
    }

    [Test]
    public void RuntimeMetadataAndActivity_CanBeDisabled()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger logger = LogConfiguration.Create()
            .MinimumLevel(LogLevel.Trace)
            .RuntimeMetadata(false)
            .Activity(false)
            .AddSink(sink)
            .CreateLogger();

        logger.Info("message");
        LogEvent logEvent = MemoryLogSink.Events(sink)[0];

        Assert.That(logEvent.ThreadId, Is.EqualTo(0));
        Assert.That(logEvent.ProcessId, Is.EqualTo(0));
        Assert.That(logEvent.MachineName, Is.EqualTo(""));
        Assert.That(logEvent.ApplicationName, Is.EqualTo(""));
        Assert.That(logger.ActivityEnricher.IsEnabled, Is.False);
    }

    [Test]
    public void CategoryLevel_ConfiguresMostSpecificLoggerLevels()
    {
        LogSink sink = MemoryLogSink.Create();
        Logger root = LogConfiguration.Create()
            .MinimumLevel(LogLevel.Trace)
            .CategoryLevel("Application", LogLevel.Error)
            .AddSink(sink)
            .CreateLogger();
        Logger application = root.For("Application.Worker");

        LogResult warnResult = application.Warn("warn");
        LogResult errorResult = application.Error("error");

        Assert.That(warnResult.IsDropped, Is.True);
        Assert.That(errorResult.IsSuccess, Is.True);
        Assert.That(MemoryLogSink.Count(sink), Is.EqualTo(1));
        Assert.That(MemoryLogSink.Events(sink)[0].Level, Is.EqualTo(LogLevel.Error));
    }

    [Test]
    public void SetMethods_CopyInputArrays()
    {
        LogSink first = MemoryLogSink.Create();
        LogSink second = MemoryLogSink.Named("second");
        LogFilter[] filters = new LogFilter[] { LogFilter.FieldExists("TenantId") };
        LogSink[] sinks = new LogSink[] { first };
        LogConfiguration configuration = LogConfiguration.Create().SetSinks(sinks).SetFilters(filters);

        sinks[0] = second;
        filters[0] = LogFilter.MinimumLevel(LogLevel.Fatal);
        Logger logger = configuration.CreateLogger();

        Assert.That(logger.Pipeline.Sinks[0], Is.SameAs(first));
        Assert.That(logger.FilterPipeline.Filters[0].Kind, Is.EqualTo(LogFilter.FieldExistsKind));
    }
}
