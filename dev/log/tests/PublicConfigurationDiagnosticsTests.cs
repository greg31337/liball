using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicConfigurationDiagnosticsTests
{
    [Test]
    public void ProgrammaticConfiguration_SupportsRuntimeCategoryChangesAndHealthSnapshots()
    {
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;
        LogSink sink = componentFactory.CreateMemorySink("configured-memory", 0);
        LogConfiguration configuration = new LogConfiguration()
            .Name("Application")
            .MinimumLevel(LogLevel.Warn)
            .AddSink(sink)
            .AddGlobalField("Tenant", "north")
            .CategoryLevel("Application.Worker", LogLevel.Debug);

        Assert.That(configuration.Validate(), Is.Empty);
        Assert.That(configuration.ValidateResult().IsSuccess, Is.True);
        Assert.That(configuration.IsValid(), Is.True);

        Logger logger = configuration.CreateLogger();
        Logger worker = ((ILoggerContext)logger).For("Worker");
        LogResult rootInfo = logger.Info("root info");
        LogResult firstDebug = worker.Debug("first debug");
        LogResult categoryChange = ((ILoggerPipeline)logger).SetCategoryLevel("Application.Worker", LogLevel.Error);
        LogResult workerWarning = worker.Warn("worker warning");
        LogResult workerError = worker.Error("worker error");
        LogEvent[] events = componentFactory.GetMemorySinkEvents(sink);
        LogPipelineHealth health = ((ILoggerPipeline)worker).Health();

        Assert.That(rootInfo.IsDropped, Is.True);
        Assert.That(firstDebug.IsSuccess, Is.True);
        Assert.That(categoryChange.IsSuccess, Is.True);
        Assert.That(workerWarning.IsDropped, Is.True);
        Assert.That(workerError.IsSuccess, Is.True);
        Assert.That(events, Has.Length.EqualTo(2));
        Assert.That(events[0].LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(FieldValue(events[0].Fields, "Tenant").TextValue, Is.EqualTo("north"));
        Assert.That(events[1].RenderedMessage, Is.EqualTo("worker error"));
        Assert.That(health.Name, Is.EqualTo("Application.Worker"));
        Assert.That(health.IsHealthy, Is.True);
        Assert.That(health.SinkCount, Is.EqualTo(1));
        Assert.That(health.WrittenCount, Is.EqualTo(2));
        Assert.That(health.DroppedCount, Is.EqualTo(1));
        Assert.That(health.Sinks[0].Name, Is.EqualTo("configured-memory"));
        Assert.That(health.Sinks[0].WrittenCount, Is.EqualTo(2));
    }

    [Test]
    public void JsonLoadingAndExplicitEnvironmentOverrides_ProduceUsableConfiguration()
    {
        string json = """
        {
          "name": "Application.Json",
          "minimumLevel": "Warn",
          "fields": { "Tenant": "json" },
          "categoryLevels": { "Application.Json.Worker": "Info" },
          "sinks": [ { "kind": "memory", "name": "json-memory" } ]
        }
        """;
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;
        LogSink observer = componentFactory.CreateMemorySink("observer", 0);
        Dictionary<string, string> environment = new Dictionary<string, string>();
        environment["LOG_NAME"] = "Application.Environment";
        environment["LOG_LEVEL"] = "Error";
        environment["LOG_FIELDS"] = "Tenant=environment;Region=west";
        environment["LOG_CATEGORY_LEVELS"] = "Application.Environment.Worker=Debug";

        LogConfiguration configuration = componentFactory.LoadJson(json);
        configuration.AddSink(observer).ApplyEnvironmentValues(environment);
        Logger logger = configuration.CreateLogger();
        Logger worker = ((ILoggerContext)logger).For("Worker");
        LogResult rootWarning = logger.Warn("root warning");
        LogResult workerDebug = worker.Debug("worker debug");
        LogEvent observed = componentFactory.GetMemorySinkEvents(observer)[0];
        LogPipelineHealth health = ((ILoggerPipeline)worker).Health();

        Assert.That(configuration.Validate(), Is.Empty);
        Assert.That(rootWarning.IsDropped, Is.True);
        Assert.That(workerDebug.IsSuccess, Is.True);
        Assert.That(observed.LoggerName, Is.EqualTo("Application.Environment.Worker"));
        Assert.That(FieldValue(observed.Fields, "Tenant").TextValue, Is.EqualTo("environment"));
        Assert.That(FieldValue(observed.Fields, "Region").TextValue, Is.EqualTo("west"));
        Assert.That(health.SinkCount, Is.EqualTo(2));
        Assert.That(health.Sinks[0].Name, Is.EqualTo("json-memory"));
        Assert.That(health.Sinks[1].Name, Is.EqualTo("observer"));
    }

    [Test]
    public void ValidationDiagnosticsAndNoSinkLogger_AreSafeAndObservable()
    {
        LogConfiguration noSinkConfiguration = new LogConfiguration();

        LogConfigurationDiagnostic[] warnings = noSinkConfiguration.Validate();
        LogResult warningResult = noSinkConfiguration.ValidateResult();
        Logger noSinkLogger = noSinkConfiguration.CreateLogger();
        LogResult writeResult = noSinkLogger.Info("safe without a sink");
        LogPipelineHealth noSinkHealth = ((ILoggerPipeline)noSinkLogger).Health();

        Assert.That(warnings, Has.Length.EqualTo(1));
        Assert.That(warnings[0].Severity, Is.EqualTo("warning"));
        Assert.That(warnings[0].Code, Is.EqualTo("configuration.sinks.empty"));
        Assert.That(warnings[0].Path, Is.EqualTo("sinks"));
        Assert.That(warnings[0].IsWarning, Is.True);
        Assert.That(warningResult.IsSuccess, Is.True);
        Assert.That(warningResult.IsDiagnostic, Is.True);
        Assert.That(warningResult.DiagnosticCount, Is.EqualTo(1));
        Assert.That(noSinkConfiguration.IsValid(), Is.True);
        Assert.That(writeResult.IsSuccess, Is.True);
        Assert.That(noSinkHealth.IsHealthy, Is.True);
        Assert.That(noSinkHealth.SinkCount, Is.Zero);
        Assert.That(noSinkHealth.WrittenCount, Is.EqualTo(1));

        LogFactory factory = new LogFactory();
        LogConfiguration invalid = new LogConfiguration()
            .Name("bad name")
            .AddSink(((ILogFactory)factory).CreateMemorySink("memory", 0));
        LogConfigurationDiagnostic[] errors = invalid.Validate();
        LogResult errorResult = invalid.ValidateResult();

        Assert.That(errors, Has.Length.EqualTo(1));
        Assert.That(errors[0].Severity, Is.EqualTo("error"));
        Assert.That(errors[0].Code, Is.EqualTo("configuration.logger.name.invalid"));
        Assert.That(errors[0].Path, Is.EqualTo("loggerName"));
        Assert.That(errors[0].IsError, Is.True);
        Assert.That(errorResult.IsFailure, Is.True);
        Assert.That(errorResult.FailureCount, Is.EqualTo(1));
        Assert.That(invalid.IsValid(), Is.False);
    }

    [Test]
    public void FileWatcher_StartStopAndManualReload_InvokePublicCallbacks()
    {
        string path = TempJsonPath("watch");
        File.WriteAllText(path, "{ \"name\": \"Application.Before\", \"minimumLevel\": \"Info\", \"sinks\": [\"memory\"] }");
        LogFactory factory = new LogFactory();
        int configurationCallbackCount = 0;
        int loggerCallbackCount = 0;
        string callbackConfigurationName = "";
        string callbackLoggerName = "";
        LogConfigurationReload reload = ((ILogFactory)factory).WatchJsonFile(path)
            .OnConfigurationReloaded(configuration =>
            {
                configurationCallbackCount = configurationCallbackCount + 1;
                callbackConfigurationName = configuration.CreateLogger().GetName();
            })
            .OnLoggerReloaded(logger =>
            {
                loggerCallbackCount = loggerCallbackCount + 1;
                callbackLoggerName = logger.GetName();
            });

        LogResult startResult = reload.Start();

        Assert.That(startResult.IsSuccess, Is.True);
        Assert.That(reload.GetFilePath(), Is.EqualTo(path));
        Assert.That(reload.GetIsWatching(), Is.True);
        Assert.That(reload.GetReloadCount(), Is.EqualTo(1));
        Assert.That(configurationCallbackCount, Is.EqualTo(1));
        Assert.That(loggerCallbackCount, Is.EqualTo(1));
        Assert.That(callbackConfigurationName, Is.EqualTo("Application.Before"));
        Assert.That(callbackLoggerName, Is.EqualTo("Application.Before"));

        LogResult stopResult = reload.Stop();
        File.WriteAllText(path, "{ \"name\": \"Application.After\", \"minimumLevel\": \"Error\", \"sinks\": [\"memory\"] }");
        LogResult reloadResult = reload.Reload();

        Assert.That(stopResult.IsSuccess, Is.True);
        Assert.That(reload.GetIsWatching(), Is.False);
        Assert.That(reloadResult.IsSuccess, Is.True);
        Assert.That(reload.GetReloadCount(), Is.EqualTo(2));
        Assert.That(reload.GetFailureCount(), Is.Zero);
        Assert.That(reload.GetLastResult(), Is.SameAs(reloadResult));
        Assert.That(reload.GetLastLogger().GetName(), Is.EqualTo("Application.After"));
        Assert.That(reload.GetLastLogger().GetMinimumLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(reload.GetLastConfiguration().CreateLogger().GetName(), Is.EqualTo("Application.After"));
        Assert.That(configurationCallbackCount, Is.EqualTo(2));
        Assert.That(loggerCallbackCount, Is.EqualTo(2));
        Assert.That(callbackConfigurationName, Is.EqualTo("Application.After"));
        Assert.That(callbackLoggerName, Is.EqualTo("Application.After"));

        reload.Close();
        reload.Dispose();
    }

    [Test]
    public void Redaction_ReplacesSelectedFieldsWithoutMutatingInputs()
    {
        ILogRedaction redaction = new LogRedaction();
        LogField[] fields = new LogField[]
        {
            new LogField("Password", LogValue.String("secret")),
            new LogField("field_message_1", LogValue.String("reserved secret")),
            new LogField("User", LogValue.String("Ada"))
        };
        LogFactory factory = new LogFactory();
        LogEvent originalEvent = ((ILogFactory)factory).CreateEvent(LogLevel.Info, "Application", "login", "login", fields);

        LogValue marker = redaction.CreateRedactedValue();
        LogField[] redactedFields = redaction.RedactFields(fields, new string[] { "password", "message" });
        LogEvent redactedEvent = redaction.RedactEvent(originalEvent, new string[] { "PASSWORD", "message" });

        Assert.That(marker.Kind, Is.EqualTo(LogValueKind.String));
        Assert.That(marker.TextValue, Is.EqualTo("[redacted]"));
        Assert.That(redactedFields[0].Value.TextValue, Is.EqualTo(marker.TextValue));
        Assert.That(redactedFields[1].Value.TextValue, Is.EqualTo(marker.TextValue));
        Assert.That(redactedFields[2].Value.TextValue, Is.EqualTo("Ada"));
        Assert.That(fields[0].Value.TextValue, Is.EqualTo("secret"));
        Assert.That(fields[1].Value.TextValue, Is.EqualTo("reserved secret"));
        Assert.That(redactedEvent, Is.Not.SameAs(originalEvent));
        Assert.That(redactedEvent.RenderedMessage, Is.EqualTo(originalEvent.RenderedMessage));
        Assert.That(redactedEvent.Fields[0].Value.TextValue, Is.EqualTo(marker.TextValue));
        Assert.That(originalEvent.Fields[0].Value.TextValue, Is.EqualTo("secret"));
    }

    [Test]
    public void SelfDiagnostics_CaptureFailuresAndProtectAgainstReentrantLogging()
    {
        ILogDiagnostics diagnostics = new LogDiagnostics();
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        LogSink failingSink = componentFactory.CreateDelegateSink(
            "failure",
            logEvent => new LogResult(false, true, false, false, "sink.failure", "write failed", 0, 1, 0));
        ((ILoggerPipeline)logger).AddSink(failingSink);
        int callbackCount = 0;

        diagnostics.Enable(message =>
        {
            callbackCount = callbackCount + 1;
            logger.Error("nested diagnostic write");
        });

        try
        {
            LogResult result = logger.Error("outer write");
            string[] entries = diagnostics.GetEntries();

            Assert.That(result.IsFailure, Is.True);
            Assert.That(diagnostics.IsEnabled(), Is.True);
            Assert.That(entries, Is.Not.Empty);
            Assert.That(entries[0], Does.Contain("sink.failure"));
            Assert.That(diagnostics.GetWrittenCount(), Is.EqualTo(entries.Length));
            Assert.That(callbackCount, Is.EqualTo(entries.Length));
            Assert.That(diagnostics.GetDroppedReentrantCount(), Is.GreaterThan(0));

            entries[0] = "changed";
            Assert.That(diagnostics.GetEntries()[0], Does.Contain("sink.failure"));
        }
        finally
        {
            diagnostics.Disable();
            diagnostics.Clear();
        }

        Assert.That(diagnostics.IsEnabled(), Is.False);
        Assert.That(diagnostics.GetWrittenCount(), Is.Zero);
        Assert.That(diagnostics.GetDroppedReentrantCount(), Is.Zero);
        Assert.That(diagnostics.GetEntries(), Is.Empty);
    }

    static string TempJsonPath(string name)
    {
        return Path.Combine(Path.GetTempPath(), "log-public-configuration-" + name + "-" + Guid.NewGuid().ToString("N") + ".json");
    }

    static LogValue FieldValue(LogField[] fields, string name)
    {
        foreach (LogField field in fields)
        {
            if (field.Name == name)
            {
                return field.Value;
            }
        }

        return LogValue.Null();
    }
}
