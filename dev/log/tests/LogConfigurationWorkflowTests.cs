using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogConfigurationWorkflowTests
{
    [Test]
    public void ProgrammaticConfiguration_AppliesEnvironmentOverridesBeforeCreatingLogger()
    {
        LogSink originalSink = MemoryLogSink.Named("original");
        LogConfiguration configuration = LogConfiguration.Create()
            .Name("Application.Programmatic")
            .MinimumLevel(LogLevel.Trace)
            .RuntimeMetadata(true)
            .AddSink(originalSink)
            .AddGlobalField("TenantId", "programmatic")
            .CategoryLevel("Application.Worker", LogLevel.Debug);
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["LOG_NAME"] = "Application.Environment";
        environment["LOG_LEVEL"] = "Warn";
        environment["LOG_RUNTIME_METADATA"] = "false";
        environment["LOG_SINKS"] = "memory,null";
        environment["LOG_FIELDS"] = "TenantId=environment;Region=west";
        environment["LOG_CATEGORY_LEVEL__Application__Environment__Application__Worker"] = "Error";

        configuration.ApplyEnvironmentValues(environment);
        Logger logger = configuration.CreateLogger();
        LogResult infoResult = logger.Info("info");
        LogResult warnResult = logger.Warn("warn");
        LogResult workerWarnResult = logger.For("Application.Worker").Warn("worker warn");
        LogResult workerErrorResult = logger.For("Application.Worker").Error("worker error");
        LogEvent[] events = MemoryLogSink.Events(configuration.Sinks[0]);

        Assert.That(configuration.LoggerName, Is.EqualTo("Application.Environment"));
        Assert.That(configuration.Sinks.Length, Is.EqualTo(2));
        Assert.That(configuration.Sinks[0], Is.Not.SameAs(originalSink));
        Assert.That(configuration.Sinks[0].IsMemorySink, Is.True);
        Assert.That(configuration.Sinks[1].IsNullSink, Is.True);
        Assert.That(logger.IncludeRuntimeMetadata, Is.False);
        Assert.That(infoResult.IsDropped, Is.True);
        Assert.That(warnResult.IsSuccess, Is.True);
        Assert.That(workerWarnResult.IsDropped, Is.True);
        Assert.That(workerErrorResult.IsSuccess, Is.True);
        Assert.That(events.Length, Is.EqualTo(2));
        Assert.That(events[0].Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(events[0].Fields[0].Value.TextValue, Is.EqualTo("environment"));
        Assert.That(events[0].Fields[1].Name, Is.EqualTo("Region"));
        Assert.That(events[0].Fields[1].Value.TextValue, Is.EqualTo("west"));
    }

    [Test]
    public void JsonConfiguration_CreatesUsableNestedAsyncBatchLogger()
    {
        string json = """
        {
          "name": "Application.Json",
          "minimumLevel": "Trace",
          "fields": {
            "TenantId": "tenant-json"
          },
          "filters": [
            { "kind": "fieldExists", "field": "TenantId" }
          ],
          "sinks": [
            {
              "kind": "async",
              "capacity": 4,
              "inner": {
                "kind": "batch",
                "batchSize": 4,
                "inner": {
                  "kind": "memory",
                  "name": "json-memory"
                }
              }
            }
          ]
        }
        """;

        LogConfiguration configuration = LogJsonConfigurationLoader.Load(json);
        Logger logger = configuration.CreateLogger();
        LogSink asyncSink = configuration.Sinks[0];
        LogSink batchSink = asyncSink.AsyncInnerSink!;
        LogSink memorySink = batchSink.BatchInnerSink!;
        LogResult writeResult = logger.InfoWithValues("processed {Count}", new object[] { 7 });
        LogResult flushResult = logger.Flush();
        LogEvent logEvent = MemoryLogSink.Events(memorySink)[0];

        Assert.That(writeResult.IsSuccess, Is.True);
        Assert.That(flushResult.IsSuccess, Is.True);
        Assert.That(asyncSink.AsyncQueueCapacity, Is.EqualTo(4));
        Assert.That(batchSink.BatchSize, Is.EqualTo(4));
        Assert.That(MemoryLogSink.Count(memorySink), Is.EqualTo(1));
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Json"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("processed 7"));
        Assert.That(FieldValue(logEvent.Fields, "TenantId").TextValue, Is.EqualTo("tenant-json"));
        Assert.That(FieldValue(logEvent.Fields, "Count").IntegerValue, Is.EqualTo(7));
    }

    [Test]
    public void Reload_AppliesCopiedEnvironmentOverridesAndPublishesUsableLogger()
    {
        string path = TempJsonPath("reload-env");
        File.WriteAllText(path, "{ \"name\": \"Application.Json\", \"minimumLevel\": \"Trace\", \"fields\": { \"TenantId\": \"json\" }, \"sinks\": [\"memory\"] }");
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["LOG_NAME"] = "Application.Environment";
        environment["LOG_LEVEL"] = "Error";
        environment["LOG_FIELDS"] = "TenantId=environment;Region=east";
        int configurationCallbackCount = 0;
        int loggerCallbackCount = 0;
        LogConfigurationReload reload = LogConfigurationReload.WatchJsonFile(path)
            .Environment(environment)
            .OnConfigurationReloaded(configuration => configurationCallbackCount = configurationCallbackCount + 1)
            .OnLoggerReloaded(logger => loggerCallbackCount = loggerCallbackCount + 1);

        environment["LOG_LEVEL"] = "Trace";
        LogResult reloadResult = reload.Reload();
        Logger logger = reload.LastLogger;
        LogResult warnResult = logger.Warn("warn");
        LogResult errorResult = logger.Error("error");
        LogEvent logEvent = MemoryLogSink.Events(reload.LastConfiguration.Sinks[0])[0];

        Assert.That(reloadResult.IsSuccess, Is.True);
        Assert.That(configurationCallbackCount, Is.EqualTo(1));
        Assert.That(loggerCallbackCount, Is.EqualTo(1));
        Assert.That(reload.LastConfiguration.LoggerName, Is.EqualTo("Application.Environment"));
        Assert.That(logger.GetMinimumLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(warnResult.IsDropped, Is.True);
        Assert.That(errorResult.IsSuccess, Is.True);
        Assert.That(logEvent.LoggerName, Is.EqualTo("Application.Environment"));
        Assert.That(logEvent.Fields[0].Value.TextValue, Is.EqualTo("environment"));
        Assert.That(logEvent.Fields[1].Value.TextValue, Is.EqualTo("east"));
    }

    [Test]
    public void Reload_ReplacesLoggerWhenJsonFileChanges()
    {
        string path = TempJsonPath("reload-json");
        File.WriteAllText(path, "{ \"name\": \"Application.First\", \"minimumLevel\": \"Info\", \"sinks\": [\"memory\"] }");
        LogConfigurationReload reload = LogConfigurationReload.WatchJsonFile(path);

        LogResult firstReload = reload.Reload();
        Logger firstLogger = reload.LastLogger;
        File.WriteAllText(path, "{ \"name\": \"Application.Second\", \"minimumLevel\": \"Debug\", \"fields\": { \"Service\": \"billing\" }, \"sinks\": [\"memory\"] }");
        LogResult secondReload = reload.Reload();
        Logger secondLogger = reload.LastLogger;
        LogResult debugResult = secondLogger.Debug("debug");
        LogEvent logEvent = MemoryLogSink.Events(reload.LastConfiguration.Sinks[0])[0];

        Assert.That(firstReload.IsSuccess, Is.True);
        Assert.That(secondReload.IsSuccess, Is.True);
        Assert.That(firstLogger.Name, Is.EqualTo("Application.First"));
        Assert.That(secondLogger, Is.Not.SameAs(firstLogger));
        Assert.That(secondLogger.Name, Is.EqualTo("Application.Second"));
        Assert.That(secondLogger.GetMinimumLevel(), Is.EqualTo(LogLevel.Debug));
        Assert.That(debugResult.IsSuccess, Is.True);
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("Service"));
        Assert.That(logEvent.Fields[0].Value.TextValue, Is.EqualTo("billing"));
    }

    static string TempJsonPath(string name)
    {
        return Path.Combine(Path.GetTempPath(), "log-configuration-workflow-" + name + "-" + Guid.NewGuid().ToString("N") + ".json");
    }

    static LogValue FieldValue(LogField[] fields, string name)
    {
        foreach (LogField field in fields)
        {
            if (field != null && field.Name == name)
            {
                return field.Value;
            }
        }

        return LogValue.Null();
    }
}
