using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogJsonConfigurationLoaderTests
{
    [Test]
    public void Load_BuildsConfigurationFromJson()
    {
        string json = """
        {
          "name": "Application.Worker",
          "minimumLevel": "Debug",
          "runtimeMetadata": false,
          "activity": false,
          "fields": {
            "TenantId": "tenant-1",
            "Retries": 3,
            "Enabled": true,
            "Tags": ["blue", "green"],
            "Limits": { "Max": 10 }
          },
          "categoryLevels": {
            "Application.Noisy": "Error"
          },
          "filters": [
            { "kind": "fieldExists", "field": "TenantId" }
          ],
          "enrichers": [
            { "kind": "field", "name": "Service", "value": "billing" },
            "runtime"
          ],
          "sinks": [
            { "kind": "memory", "name": "capture", "minimumLevel": "Info" }
          ]
        }
        """;

        LogConfiguration configuration = LogJsonConfigurationLoader.Load(json);
        Logger logger = configuration.CreateLogger();
        LogResult result = logger.Info("processed");
        LogEvent logEvent = MemoryLogSink.Events(configuration.Sinks[0])[0];

        Assert.That(configuration.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(configuration.ConfiguredLevelSwitch.GetLevel(), Is.EqualTo(LogLevel.Debug));
        Assert.That(configuration.IncludeRuntimeMetadata, Is.False);
        Assert.That(configuration.IncludeActivity, Is.False);
        Assert.That(configuration.CategoryLevelSwitches["Application.Noisy"].GetLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(configuration.Sinks[0].Name, Is.EqualTo("capture"));
        Assert.That(configuration.Sinks[0].HasMinimumLevel, Is.True);
        Assert.That(configuration.Filters[0].Kind, Is.EqualTo(LogFilter.FieldExistsKind));
        Assert.That(configuration.Enrichers.Length, Is.EqualTo(2));
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(0));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("TenantId"));
        Assert.That(logEvent.Fields[1].Value.IntegerValue, Is.EqualTo(3));
        Assert.That(logEvent.Fields[2].Value.BoolValue, Is.True);
        Assert.That(logEvent.Fields[3].Value.Kind, Is.EqualTo(LogValueKind.Array));
        Assert.That(logEvent.Fields[4].Value.Kind, Is.EqualTo(LogValueKind.Dictionary));
    }

    [Test]
    public void Load_ConfiguresFileSinkFormatterAndRollingPolicy()
    {
        string json = """
        {
          "sinks": [
            {
              "kind": "file",
              "path": "/tmp/app.log",
              "formatter": "json",
              "append": false,
              "flushOnWrite": true,
              "rolling": {
                "interval": "day",
                "maximumFileSizeBytes": 2048
              }
            }
          ]
        }
        """;

        LogConfiguration configuration = LogJsonConfigurationLoader.Load(json);
        LogSink sink = configuration.Sinks[0];

        Assert.That(sink.IsFileSink, Is.True);
        Assert.That(sink.FilePath, Is.EqualTo("/tmp/app.log"));
        Assert.That(sink.SinkFormatter.Name, Is.EqualTo(LogFormatter.JsonName));
        Assert.That(sink.FileAppend, Is.False);
        Assert.That(sink.FlushOnWrite, Is.True);
        Assert.That(sink.HasRollingPolicy, Is.True);
        Assert.That(sink.RollingPolicy.Interval, Is.EqualTo(LogRollingInterval.Day));
        Assert.That(sink.RollingPolicy.MaximumFileSizeBytes, Is.EqualTo(2048));
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(0));
    }

    [Test]
    public void Load_ConfiguresWrapperSinks()
    {
        string json = """
        {
          "sinks": [
            {
              "kind": "async",
              "capacity": 7,
              "backpressure": "dropOldest",
              "inner": {
                "kind": "batch",
                "batchSize": 2,
                "inner": {
                  "kind": "circuitBreaker",
                  "failureThreshold": 4,
                  "inner": "memory"
                }
              }
            }
          ]
        }
        """;

        LogConfiguration configuration = LogJsonConfigurationLoader.Load(json);
        LogSink asyncSink = configuration.Sinks[0];
        LogSink batchSink = asyncSink.AsyncInnerSink!;
        LogSink circuitSink = batchSink.BatchInnerSink!;

        Assert.That(asyncSink.IsAsyncSink, Is.True);
        Assert.That(asyncSink.AsyncQueueCapacity, Is.EqualTo(7));
        Assert.That(asyncSink.AsyncBackpressureMode, Is.EqualTo(LogBackpressureMode.DropOldest));
        Assert.That(batchSink.IsBatchSink, Is.True);
        Assert.That(batchSink.BatchSize, Is.EqualTo(2));
        Assert.That(circuitSink.IsCircuitBreakerSink, Is.True);
        Assert.That(circuitSink.CircuitBreakerFailureThreshold, Is.EqualTo(4));
        Assert.That(circuitSink.CircuitBreakerInnerSink!.IsMemorySink, Is.True);
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(0));
    }

    [Test]
    public void Load_ConfiguresFiltersAndSinkFilters()
    {
        string json = """
        {
          "sinks": [
            {
              "kind": "memory",
              "filters": [
                { "kind": "minimumLevel", "level": "Warn" },
                { "kind": "fieldEquals", "field": "TenantId", "value": "tenant-1" }
              ]
            }
          ],
          "filters": [
            { "kind": "categoryPrefix", "prefix": "Application", "level": "Info" },
            { "kind": "sampleEvery", "count": 3 },
            { "kind": "rateLimit", "count": 2, "windowMilliseconds": 5000 }
          ]
        }
        """;

        LogConfiguration configuration = LogJsonConfigurationLoader.Load(json);

        Assert.That(configuration.Filters[0].Kind, Is.EqualTo(LogFilter.CategoryPrefixKind));
        Assert.That(configuration.Filters[1].Kind, Is.EqualTo(LogFilter.SampleEveryKind));
        Assert.That(configuration.Filters[1].Count, Is.EqualTo(3));
        Assert.That(configuration.Filters[2].Kind, Is.EqualTo(LogFilter.RateLimitKind));
        Assert.That(configuration.Filters[2].Window, Is.EqualTo(TimeSpan.FromMilliseconds(5000)));
        Assert.That(configuration.Sinks[0].FilterPipeline.Filters[0].Kind, Is.EqualTo(LogFilter.MinimumLevelKind));
        Assert.That(configuration.Sinks[0].FilterPipeline.Filters[1].Kind, Is.EqualTo(LogFilter.FieldEqualsKind));
        Assert.That(configuration.Sinks[0].FilterPipeline.Filters[1].Value.TextValue, Is.EqualTo("tenant-1"));
    }

    [Test]
    public void LoadFile_ReadsJsonConfigurationFromDisk()
    {
        string path = Path.Combine(Path.GetTempPath(), "log-json-configuration-loader-test.json");
        File.WriteAllText(path, "{ \"name\": \"Application.File\", \"sinks\": [\"memory\"] }");

        LogConfiguration configuration = LogJsonConfigurationLoader.LoadFile(path);

        Assert.That(configuration.LoggerName, Is.EqualTo("Application.File"));
        Assert.That(configuration.Sinks[0].IsMemorySink, Is.True);
        Assert.That(configuration.LastDiagnostics.Length, Is.EqualTo(0));
    }

    [Test]
    public void Load_PopulatesValidationDiagnostics()
    {
        string json = """
        {
          "filters": [
            { "kind": "fieldExists", "field": "" }
          ],
          "enrichers": [
            { "kind": "unknown" }
          ],
          "sinks": [
            { "kind": "file", "path": "" }
          ]
        }
        """;

        LogConfiguration configuration = LogJsonConfigurationLoader.Load(json);
        LogResult result = configuration.ValidateResult();

        Assert.That(ContainsCode(configuration.LastDiagnostics, "configuration.filter.field.invalid"), Is.True);
        Assert.That(ContainsCode(configuration.LastDiagnostics, "configuration.enricher.kind.unknown"), Is.True);
        Assert.That(ContainsCode(configuration.LastDiagnostics, "sink.file.path.missing"), Is.True);
        Assert.That(result.IsFailure, Is.True);
    }

    static bool ContainsCode(LogConfigurationDiagnostic[] diagnostics, string code)
    {
        foreach (LogConfigurationDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic != null && diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
