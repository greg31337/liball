using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicExternalConsumerCoverageTests
{
    [Test]
    public void PublicConstructorsAndConcreteTypes_ProvideEveryPublicInterface()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 27, 1, 2, 3, TimeSpan.Zero);
        LogFactory factory = new LogFactory();
        LogConfiguration configuration = new LogConfiguration();
        LogDiagnostics diagnostics = new LogDiagnostics();
        LogLevelSwitch levelSwitch = new LogLevelSwitch();
        LogRedaction redaction = new LogRedaction();
        LogEventBuilder builder = new LogEventBuilder(new ExternalClock(timestamp));
        LogValue value = new LogValue(LogValueKind.String, "value", false, 0, 0m, 0d, timestamp, TimeSpan.Zero, null, new LogValue[0], new Dictionary<string, LogValue>(), "");
        LogField field = new LogField("Field", value);
        LogEvent logEvent = new LogEvent(timestamp, LogLevel.Info, "Application", 1, "Event", "message", "message", new LogField[] { field }, null, "", "", "", 1, 2, "machine", "application", "file.cs", "Run", 3);
        LogResult result = new LogResult(true, false, false, false, "", "", 0, 0, 0);
        LogRollingPolicy rolling = new LogRollingPolicy();
        LogRetentionPolicy retention = new LogRetentionPolicy();
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        LogSink sink = ((ILogFactory)factory).CreateMemorySink("memory", 0);
        LogSinkPipeline sinkPipeline = ((ILogFactory)factory).CreateSinkPipeline("branch");
        LogFormatter formatter = ((ILogFactory)factory).CreateTextFormatter();
        LogConfigurationReload reload = ((ILogFactory)factory).WatchJsonFile("configuration.json");
        LogDuration duration = ((ILoggerDuration)logger).BeginDuration(LogLevel.Info, "duration", null, "Elapsed", TimeProvider.System);
        LogScope scope = ((ILoggerContext)logger).BeginScope(new LogField[0]);

        ILogClock clockContract = new ExternalClock(timestamp);
        ILogConfiguration configurationContract = configuration;
        ILogConfigurationReload reloadContract = reload;
        ILogDiagnostics diagnosticsContract = diagnostics;
        ILogDuration durationContract = duration;
        ILogEnricher enricherContract = new ExternalEnricher();
        ILogEventBuilder builderContract = builder;
        ILogFactory factoryContract = factory;
        ILogFieldGroupFactory fieldGroupFactoryContract = factory;
        ILogFilter filterContract = new ExternalFilter(LogDecision.Include);
        ILogFormatter formatterContract = new ExternalFormatter();
        ILogFormatterConfiguration formatterConfigurationContract = formatter;
        ILogLevelSwitch levelSwitchContract = levelSwitch;
        ILogRedaction redactionContract = redaction;
        ILogScope scopeContract = scope;
        ILogSink sinkContract = sink;
        ILogSinkConfiguration sinkConfigurationContract = sink;
        ILogSinkPipeline sinkPipelineContract = sinkPipeline;
        ILogSinkState sinkStateContract = sink;
        ILogValueFactory valueFactoryContract = factory;
        ILogger loggerContract = logger;
        ILoggerContext loggerContextContract = logger;
        ILoggerDuration loggerDurationContract = logger;
        ILoggerFactory loggerFactoryContract = factory;
        ILoggerLevels loggerLevelsContract = logger;
        ILoggerPipeline loggerPipelineContract = logger;
        ILoggerState loggerStateContract = logger;

        object[] contracts = new object[]
        {
            clockContract,
            configurationContract,
            reloadContract,
            diagnosticsContract,
            durationContract,
            enricherContract,
            builderContract,
            factoryContract,
            fieldGroupFactoryContract,
            filterContract,
            formatterContract,
            formatterConfigurationContract,
            levelSwitchContract,
            redactionContract,
            scopeContract,
            sinkContract,
            sinkConfigurationContract,
            sinkPipelineContract,
            sinkStateContract,
            valueFactoryContract,
            loggerContract,
            loggerContextContract,
            loggerDurationContract,
            loggerFactoryContract,
            loggerLevelsContract,
            loggerPipelineContract,
            loggerStateContract
        };

        Assert.That(contracts, Has.Length.EqualTo(27));
        Assert.That(contracts, Has.None.Null);
        Assert.That(value.Kind, Is.EqualTo(LogValueKind.String));
        Assert.That(field.Value, Is.SameAs(value));
        Assert.That(logEvent.Fields[0].Name, Is.EqualTo("Field"));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(rolling.Interval, Is.EqualTo(LogRollingInterval.None));
        Assert.That(retention.MaximumFileCount, Is.Zero);
        Assert.That(builderContract.Build().Timestamp, Is.EqualTo(timestamp));

        scopeContract.Dispose();
        durationContract.Complete();
        diagnosticsContract.Disable();
        reloadContract.Dispose();
    }

    [Test]
    public void FactoryValueAndFieldContracts_ExerciseEveryCreationOperation()
    {
        LogFactory concreteFactory = new LogFactory();
        ILogFactory factory = concreteFactory;
        ILogValueFactory values = concreteFactory;
        ILogFieldGroupFactory groups = concreteFactory;
        ILoggerFactory loggers = concreteFactory;
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 27, 2, 0, 0, TimeSpan.Zero);
        TimeSpan duration = TimeSpan.FromSeconds(3);
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        dictionary["Item"] = 1;

        LogValue[] createdValues = new LogValue[]
        {
            factory.CreateValue("general"),
            values.CreateValueWithLimits("limited", 3, 2, 1),
            values.CreateNullValue(),
            values.CreateStringValue("text"),
            values.CreateStringValueWithLimit("text", 2),
            values.CreateBoolValue(true),
            values.CreateIntValue(1),
            values.CreateLongValue(2L),
            values.CreateDecimalValue(3m),
            values.CreateDoubleValue(4d),
            values.CreateDateTimeValue(timestamp),
            values.CreateDurationValue(duration),
            values.CreateExceptionValue(exceptionValue),
            values.CreateExceptionValueWithLimit(exceptionValue, 20),
            values.CreateObjectTextValue(exceptionValue),
            values.CreateObjectTextValueWithLimit(exceptionValue, 20),
            values.CreateArrayValue(new object?[] { 1, "two" }),
            values.CreateArrayValueWithLimits(new object?[] { "one", "two" }, 10, 1, 2),
            values.CreateDictionaryValue(dictionary),
            values.CreateDictionaryValueWithLimits(dictionary, 10, 1, 2),
            values.CreateRawJsonValue("{\"ok\":true}"),
            values.CreateRawJsonValueWithLimits("[1]", 10, 2)
        };

        LogField[] createdFields = new LogField[]
        {
            factory.CreateField("General", 1),
            values.CreateFieldWithLimits("Limited", "value", 3, 2, 1),
            values.CreateNullField("Null"),
            values.CreateStringField("String", "text"),
            values.CreateStringFieldWithLimit("LimitedString", "text", 2),
            values.CreateBoolField("Bool", true),
            values.CreateIntField("Int", 1),
            values.CreateLongField("Long", 2L),
            values.CreateDecimalField("Decimal", 3m),
            values.CreateDoubleField("Double", 4d),
            values.CreateDateTimeField("DateTime", timestamp),
            values.CreateDurationField("Duration", duration),
            values.CreateExceptionField("Exception", exceptionValue),
            values.CreateExceptionFieldWithLimit("LimitedException", exceptionValue, 20),
            values.CreateObjectTextField("Object", exceptionValue),
            values.CreateObjectTextFieldWithLimit("LimitedObject", exceptionValue, 20),
            values.CreateArrayField("Array", new object?[] { 1, "two" }),
            values.CreateArrayFieldWithLimits("LimitedArray", new object?[] { 1, 2 }, 10, 1, 2),
            values.CreateDictionaryField("Dictionary", dictionary),
            values.CreateDictionaryFieldWithLimits("LimitedDictionary", dictionary, 10, 1, 2),
            values.CreateRawJsonField("Raw", "{\"ok\":true}"),
            values.CreateRawJsonFieldWithLimits("LimitedRaw", "[1]", 10, 2)
        };
        LogField group = groups.CreateFieldGroup("Group", new LogField[] { createdFields[0] });
        LogField fieldNamespace = groups.CreateFieldNamespace("Company.Service", new LogField[] { createdFields[1] });
        LogLevelSwitch sharedSwitch = new LogLevelSwitch();
        Logger[] createdLoggers = new Logger[]
        {
            loggers.CreateRootLogger(),
            loggers.CreateRootLoggerWithLevel(LogLevel.Debug),
            loggers.CreateNamedLogger("Application"),
            loggers.CreateNamedLoggerWithLevel("Application.Worker", LogLevel.Warn),
            loggers.CreateRootLoggerWithSwitch(sharedSwitch),
            loggers.CreateNamedLoggerWithSwitch("Application.Shared", sharedSwitch),
            loggers.CreateNoOpLogger()
        };
        LogEvent createdEvent = factory.CreateEvent(LogLevel.Info, "Application", "template", "rendered", new LogField[] { createdFields[0] });

        Assert.That(createdValues, Has.Length.EqualTo(22));
        Assert.That(createdValues, Has.All.Not.Null);
        Assert.That(createdFields, Has.Length.EqualTo(22));
        Assert.That(createdFields, Has.All.Matches<LogField>(field => field.IsValid));
        Assert.That(group.Value.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(fieldNamespace.Value.Kind, Is.EqualTo(LogValueKind.Dictionary));
        Assert.That(createdLoggers, Has.Length.EqualTo(7));
        Assert.That(createdLoggers[6].GetIsNoOp(), Is.True);
        Assert.That(createdEvent.RenderedMessage, Is.EqualTo("rendered"));
    }

    [Test]
    public void ComponentFactoryContract_ExercisesFormattersFiltersSinksPoliciesAndConfigurationFiles()
    {
        string directory = NewTempDirectory("components");
        string logPath = Path.Combine(directory, "events.log");
        string jsonPath = Path.Combine(directory, "log.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(jsonPath, "{ \"name\": \"Application.File\", \"minimumLevel\": \"Debug\", \"sinks\": [\"memory\"] }");
        LogFactory concreteFactory = new LogFactory();
        ILogFactory factory = concreteFactory;
        LogEvent logEvent = factory.CreateEvent(LogLevel.Info, "Application", "message", "message", new LogField[] { factory.CreateField("Required", 1) });
        ExternalClock clock = new ExternalClock(DateTimeOffset.UnixEpoch);
        ExternalFilter externalFilter = new ExternalFilter(LogDecision.Include);
        ExternalFormatter externalFormatter = new ExternalFormatter();
        ExternalSink externalSink = new ExternalSink("external");

        LogFormatter[] formatters = new LogFormatter[]
        {
            factory.CreateTextFormatter(),
            factory.CreateCompactJsonFormatter(),
            factory.CreateFullJsonFormatter(),
            factory.CreatePlainConsoleFormatter(),
            factory.CreateColorConsoleFormatter(),
            factory.CreateKeyValueFormatter(),
            factory.CreateTemplateFormatter(),
            factory.CreateOutputTemplateFormatter("{Message}"),
            factory.CreateFullExceptionFormatter(),
            factory.CreateSummaryExceptionFormatter(),
            factory.CreateNamedFormatter("text"),
            factory.CreateCustomFormatter("custom", value => value.RenderedMessage),
            factory.WrapFormatter(externalFormatter)
        };
        LogFilter[] filters = new LogFilter[]
        {
            factory.WrapFilter(externalFilter),
            factory.CreateMinimumLevelFilter(LogLevel.Info),
            factory.CreateSamplingFilter(1),
            factory.CreateRateLimitFilter(10),
            factory.CreateWindowedRateLimitFilter(10, TimeSpan.FromSeconds(2)),
            factory.CreateClockedRateLimitFilter(10, TimeSpan.FromSeconds(2), clock),
            factory.CreateCategoryFilter("Application", LogLevel.Info),
            factory.CreateFieldExistsFilter("Required"),
            factory.CreateFieldEqualsFilter("Required", 1),
            factory.CreateStructuredFieldEqualsFilter("Required", new LogFactory().CreateIntValue(1))
        };
        StringWriter textWriter = new StringWriter();
        StringWriter consoleOutput = new StringWriter();
        StringWriter consoleError = new StringWriter();
        int diagnosticWrites = 0;
        int eventSourceWrites = 0;
        LogSink memoryInner = factory.CreateMemorySink("inner", 0);
        LogSink[] sinks = new LogSink[]
        {
            factory.WrapSink(externalSink),
            factory.CreateNullSink("null"),
            factory.CreateMemorySink("memory", 0),
            factory.CreateTextWriterSink(textWriter, externalFormatter, true),
            factory.CreateConsoleSink(consoleOutput, consoleError, externalFormatter, false),
            factory.CreateDebugSink(message => diagnosticWrites = diagnosticWrites + 1, externalFormatter, true),
            factory.CreateTraceSink(message => diagnosticWrites = diagnosticWrites + 1, externalFormatter, true),
            factory.CreateFileSink(logPath, externalFormatter, false, null, null, true),
            factory.CreateEventSourceSink("External.Consumer", externalFormatter, entry => eventSourceWrites = eventSourceWrites + 1),
            factory.CreateDelegateSink("delegate", value => new LogResult(true, false, false, false, "", "", 0, 0, 0)),
            factory.CreateAsyncSink("async", memoryInner, 4, LogBackpressureMode.DropNewest),
            factory.CreateBatchSink("batch", memoryInner, 2),
            factory.CreateCircuitBreakerSink("circuit", memoryInner, 2)
        };

        try
        {
            foreach (LogFormatter formatter in formatters)
            {
                Assert.That(((ILogFormatter)formatter).Format(logEvent), Is.Not.Null);
            }

            foreach (LogFilter filter in filters)
            {
                Assert.That(((ILogFilter)filter).Decide(logEvent), Is.Not.EqualTo(LogDecision.Exclude));
            }

            foreach (LogSink sink in sinks)
            {
                Assert.That(((ILogSink)sink).Write(logEvent).IsFailure, Is.False);
            }

            Assert.That(factory.GetMemorySinkEvents(sinks[2]), Has.Length.EqualTo(1));
            factory.ClearMemorySink(sinks[2]);
            Assert.That(factory.GetMemorySinkEvents(sinks[2]), Is.Empty);
            Assert.That(factory.GetAsyncSinkEvents(sinks[10]), Has.Length.EqualTo(1));
            Assert.That(factory.GetBatchSinkEvents(sinks[11]), Has.Length.EqualTo(1));
            factory.SetSinkMinimumLevel(sinks[2], LogLevel.Error);
            Assert.That(sinks[2].IsEnabledForLevel(LogLevel.Info), Is.False);
            factory.ClearSinkMinimumLevel(sinks[2]);
            factory.AddSinkFilter(sinks[2], externalFilter);
            Assert.That(sinks[2].IsEnabledForLevel(LogLevel.Info), Is.True);

            LogRollingPolicy rolling = factory.CreateRollingPolicy(LogRollingInterval.Day, 1024);
            LogRetentionPolicy retention = factory.CreateRetentionPolicy(3, TimeSpan.FromDays(2));
            Assert.That(rolling.Interval, Is.EqualTo(LogRollingInterval.Day));
            Assert.That(retention.MaximumFileCount, Is.EqualTo(3));

            LogConfiguration json = factory.LoadJson("{ \"name\": \"Application.Json\", \"sinks\": [\"memory\"] }");
            LogConfiguration jsonFile = factory.LoadJsonFile(jsonPath);
            LogConfiguration jsonEnvironment = factory.LoadJsonWithEnvironment("{ \"sinks\": [\"memory\"] }");
            LogConfiguration jsonFileEnvironment = factory.LoadJsonFileWithEnvironment(jsonPath);
            ILogConfigurationReload watcher = factory.WatchJsonFile(jsonPath);
            ILogConfigurationReload environmentWatcher = factory.WatchJsonFileWithEnvironment(jsonPath);

            Assert.That(json.CreateLogger().GetName(), Is.EqualTo("Application.Json"));
            Assert.That(jsonFile.CreateLogger().GetName(), Is.EqualTo("Application.File"));
            Assert.That(jsonEnvironment.IsValid(), Is.True);
            Assert.That(jsonFileEnvironment.IsValid(), Is.True);
            Assert.That(watcher.GetFilePath(), Is.EqualTo(jsonPath));
            Assert.That(environmentWatcher.GetFilePath(), Is.EqualTo(jsonPath));
            watcher.Dispose();
            environmentWatcher.Dispose();
            Assert.That(diagnosticWrites, Is.EqualTo(2));
            Assert.That(eventSourceWrites, Is.EqualTo(1));
        }
        finally
        {
            foreach (LogSink sink in sinks)
            {
                ((ILogSink)sink).Close();
            }

            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ConfigurationContract_ExercisesEveryMutationValidationAndConstructionOperation()
    {
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 27, 3, 0, 0, TimeSpan.Zero);
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        ILogConfiguration scratch = new LogConfiguration();
        Assert.That(scratch.LevelSwitch(null!), Is.Not.Null);
        Assert.That(scratch.Clock(null!), Is.Not.Null);
        Assert.That(scratch.SetInterfaceSinks(null!), Is.Not.Null);
        Assert.That(scratch.SetInterfaceFilters(null!), Is.Not.Null);
        Assert.That(scratch.SetInterfaceEnrichers(null!), Is.Not.Null);
        Assert.That(scratch.ApplyEnvironment(), Is.SameAs(scratch));

        LogConfiguration concreteConfiguration = new LogConfiguration();
        ILogConfiguration configuration = concreteConfiguration;
        LogLevelSwitch levelSwitch = new LogLevelSwitch();
        levelSwitch.SetLevel(LogLevel.Trace);
        ExternalClock clock = new ExternalClock(timestamp);
        ExternalSink externalSink = new ExternalSink("external");
        ExternalFilter externalFilter = new ExternalFilter(LogDecision.Include);
        ExternalEnricher externalEnricher = new ExternalEnricher();
        LogSink firstSink = components.CreateMemorySink("first", 0);
        LogSink finalSink = components.CreateMemorySink("final", 0);
        LogFilter firstFilter = components.CreateMinimumLevelFilter(LogLevel.Trace);
        LogField firstField = components.CreateField("First", 1);
        LogField globalField = components.CreateField("Global", "value");

        Assert.That(configuration.Name("Application"), Is.SameAs(concreteConfiguration));
        Assert.That(configuration.MinimumLevel(LogLevel.Debug), Is.SameAs(concreteConfiguration));
        Assert.That(configuration.LevelSwitch(levelSwitch), Is.SameAs(concreteConfiguration));
        Assert.That(configuration.Clock(clock), Is.SameAs(concreteConfiguration));
        configuration.AddSink(firstSink);
        configuration.AddInterfaceSink(externalSink);
        configuration.SetSinks(new LogSink[] { firstSink });
        configuration.SetInterfaceSinks(new ILogSink[] { externalSink });
        configuration.ClearSinks();
        configuration.AddSink(finalSink);
        configuration.AddFilter(firstFilter);
        configuration.AddInterfaceFilter(externalFilter);
        configuration.SetFilters(new LogFilter[] { firstFilter });
        configuration.SetInterfaceFilters(new ILogFilter[] { externalFilter });
        configuration.ClearFilters();
        configuration.AddInterfaceFilter(externalFilter);
        configuration.AddInterfaceEnricher(externalEnricher);
        configuration.SetInterfaceEnrichers(new ILogEnricher[] { externalEnricher });
        configuration.ClearEnrichers();
        configuration.AddInterfaceEnricher(externalEnricher);
        configuration.AddGlobalField("Temporary", 1);
        configuration.AddGlobalLogField(firstField);
        configuration.SetGlobalFields(new LogField[] { firstField });
        configuration.ClearGlobalFields();
        configuration.AddGlobalLogField(globalField);
        configuration.RuntimeMetadata(false);
        configuration.Activity(false);
        configuration.CategoryLevel("bad category", LogLevel.Error);
        configuration.CategoryLevel("Application.Worker", LogLevel.Debug);
        configuration.ApplyEnvironmentValues(new Dictionary<string, string>());

        LogConfigurationDiagnostic[] diagnostics = configuration.Validate();
        LogResult validation = configuration.ValidateResult();
        bool isValid = configuration.IsValid();
        Logger logger = configuration.CreateLogger();
        Logger worker = ((ILoggerContext)logger).For("Worker");
        LogResult writeResult = worker.WriteWithValues(LogLevel.Debug, "processed", null);
        LogEvent captured = components.GetMemorySinkEvents(finalSink)[0];

        Assert.That(diagnostics, Is.Empty);
        Assert.That(validation.IsSuccess, Is.True);
        Assert.That(isValid, Is.True);
        Assert.That(writeResult.IsSuccess, Is.True);
        Assert.That(captured.Timestamp, Is.EqualTo(timestamp));
        Assert.That(captured.LoggerName, Is.EqualTo("Application.Worker"));
        Assert.That(FieldValue(captured.Fields, "Global").TextValue, Is.EqualTo("value"));
        Assert.That(FieldValue(captured.Fields, "External").TextValue, Is.EqualTo("enriched"));
    }

    [Test]
    public void LoggerContracts_ExerciseEveryOperationFamilyThroughPublicInterfaces()
    {
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        Logger logger = ((ILoggerFactory)factory).CreateNamedLoggerWithLevel("Application", LogLevel.Trace);
        ILogger writer = logger;
        ILoggerLevels levels = logger;
        ILoggerContext context = logger;
        ILoggerPipeline pipeline = logger;
        ILoggerState state = logger;
        ILoggerDuration durations = logger;
        LogSink memory = components.CreateMemorySink("memory", 0);
        ExternalSink externalSink = new ExternalSink("external");
        ExternalFilter externalFilter = new ExternalFilter(LogDecision.Include);
        ExternalEnricher externalEnricher = new ExternalEnricher();
        LogFilter concreteFilter = components.CreateMinimumLevelFilter(LogLevel.Trace);

        pipeline.AddEnricher(externalEnricher);
        pipeline.SetEnrichers(new ILogEnricher[] { externalEnricher });
        pipeline.ClearEnrichers();
        pipeline.AddEnricher(externalEnricher);
        pipeline.AddFilter(concreteFilter);
        pipeline.AddInterfaceFilter(externalFilter);
        pipeline.SetFilters(new LogFilter[] { concreteFilter });
        pipeline.SetInterfaceFilters(new ILogFilter[] { externalFilter });
        pipeline.ClearFilters();
        pipeline.AddInterfaceFilter(externalFilter);
        pipeline.AddSink(memory);
        pipeline.AddInterfaceSink(externalSink);
        pipeline.SetSinks(new LogSink[] { memory });
        pipeline.SetInterfaceSinks(new ILogSink[] { externalSink });
        pipeline.ClearSinks();
        pipeline.AddSink(memory);

        LogSinkPipeline firstBranch = components.CreateSinkPipeline("first");
        LogSinkPipeline secondBranch = components.CreateSinkPipeline("second");
        firstBranch.AddSink(components.CreateNullSink("first-null"));
        secondBranch.AddSink(components.CreateNullSink("second-null"));
        LogSink route = pipeline.InstallSubPipeline(firstBranch);
        Assert.That(route.GetName(), Is.EqualTo("first"));
        pipeline.SetSubPipelines(new LogSinkPipeline[] { secondBranch });
        Assert.That(pipeline.GetSubPipelines(), Is.EqualTo(new LogSinkPipeline[] { secondBranch }));
        Assert.That(pipeline.GetSubPipelineSinks(), Has.Length.EqualTo(1));
        pipeline.ClearSubPipelines();
        pipeline.SetShutdownTimeout(TimeSpan.FromSeconds(1));
        pipeline.SetSinkMinimumLevel(LogLevel.Trace);
        pipeline.ClearSinkMinimumLevel();
        Assert.That(pipeline.SetCategoryLevel("Application.Worker", LogLevel.Debug).IsSuccess, Is.True);
        Assert.That(pipeline.SetCategoryLevel("bad category", LogLevel.Debug).IsFailure, Is.True);

        Assert.That(writer.IsEnabled(LogLevel.Info), Is.True);
        Assert.That(writer.WriteWithValues(LogLevel.Info, "general {Value}", new object?[] { 1 }).IsSuccess, Is.True);
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        Assert.That(writer.WriteWithExceptionAndValues(LogLevel.Error, exceptionValue, "exception", null).IsSuccess, Is.True);
        LogEvent explicitEvent = components.CreateEvent(LogLevel.Warn, "Application", "explicit", "explicit", new LogField[0]);
        Assert.That(writer.WriteEvent(explicitEvent).IsSuccess, Is.True);
        Assert.That(writer.WriteEvent(null).IsFailure, Is.True);

        ExerciseEveryLevelOperation(levels, exceptionValue);
        ExerciseEveryContextOperation(context, components);
        LogDuration duration = durations.BeginDuration(LogLevel.Info, "duration", null, "Elapsed", TimeProvider.System);
        Assert.That(((ILogDuration)duration).Complete().IsSuccess, Is.True);

        Assert.That(state.GetName(), Is.EqualTo("Application"));
        Assert.That(state.GetIsNoOp(), Is.False);
        Assert.That(state.GetIsValid(), Is.True);
        Assert.That(state.GetErrorMessage(), Is.Empty);
        Assert.That(state.GetLastEvent(), Is.Not.Null);
        Assert.That(state.GetLastWriteResult(), Is.Not.Null);
        Assert.That(state.GetWrittenCount(), Is.GreaterThan(0));
        Assert.That(state.GetDroppedCount(), Is.GreaterThanOrEqualTo(0));
        Assert.That(state.GetFailureCount(), Is.GreaterThanOrEqualTo(1));
        Assert.That(state.GetDiagnosticCount(), Is.GreaterThanOrEqualTo(0));
        Assert.That(state.GetMinimumLevel(), Is.EqualTo(LogLevel.Trace));
        state.ClearDroppedCount();
        state.ClearFailureCount();
        state.ClearDiagnosticCount();
        state.ClearCounters();

        Assert.That(pipeline.Health().SinkCount, Is.EqualTo(1));
        Assert.That(pipeline.Flush().IsSuccess, Is.True);
        Assert.That(pipeline.Drain().IsSuccess, Is.True);
        Assert.That(pipeline.Close().IsSuccess, Is.True);
    }

    [Test]
    public void SinkAndSinkPipelineContracts_ExerciseConfigurationStateRoutingAndLifecycle()
    {
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        LogEvent logEvent = components.CreateEvent(LogLevel.Info, "Application", "message", "message", new LogField[] { components.CreateField("Required", true) });
        LogSink concreteSink = components.CreateMemorySink("memory", 0);
        ILogSink sink = concreteSink;
        ILogSinkState state = concreteSink;
        ILogSinkConfiguration configuration = concreteSink;
        ExternalFilter externalFilter = new ExternalFilter(LogDecision.Include);
        LogFilter concreteFilter = components.CreateFieldExistsFilter("Required");

        configuration.SetMinimumLevel(LogLevel.Warn);
        Assert.That(sink.IsEnabledForLevel(LogLevel.Info), Is.False);
        configuration.ClearMinimumLevel();
        configuration.AddFilter(concreteFilter);
        configuration.AddInterfaceFilter(externalFilter);
        configuration.SetFilters(new LogFilter[] { concreteFilter });
        configuration.SetInterfaceFilters(new ILogFilter[] { externalFilter });
        configuration.ClearFilters();
        configuration.AddInterfaceFilter(externalFilter);
        Assert.That(configuration.IsSubPipelineEnabled(LogLevel.Info), Is.True);
        configuration.ClearSubPipeline();

        Assert.That(sink.GetName(), Is.EqualTo("memory"));
        Assert.That(sink.Write(null).IsFailure, Is.True);
        Assert.That(sink.Write(logEvent).IsSuccess, Is.True);
        Assert.That(state.GetIsEnabled(), Is.True);
        Assert.That(state.GetLastEvent(), Is.SameAs(logEvent));
        Assert.That(state.GetWrittenCount(), Is.EqualTo(1));
        Assert.That(state.GetDroppedCount(), Is.Zero);
        Assert.That(state.GetFailureCount(), Is.EqualTo(1));
        Assert.That(state.GetDiagnosticCount(), Is.Zero);
        Assert.That(state.GetLastResult().IsSuccess, Is.True);
        Assert.That(state.GetIsClosed(), Is.False);
        state.ClearDroppedCount();
        state.ClearFailureCount();
        state.ClearDiagnosticCount();
        state.ClearCounters();
        Assert.That(sink.Flush().IsSuccess, Is.True);
        Assert.That(sink.Drain().IsSuccess, Is.True);

        ILogSinkPipeline pipeline = components.CreateSinkPipeline("public-branch");
        LogSink first = components.CreateMemorySink("first", 0);
        ExternalSink externalSink = new ExternalSink("external");
        pipeline.AddSink(first);
        pipeline.AddInterfaceSink(externalSink);
        pipeline.SetSinks(new LogSink[] { first });
        pipeline.SetInterfaceSinks(new ILogSink[] { externalSink });
        pipeline.ClearSinks();
        pipeline.AddSink(first);
        Assert.That(pipeline.GetName(), Is.EqualTo("public-branch"));
        Assert.That(pipeline.GetSinks(), Is.EqualTo(new LogSink[] { first }));
        LogSinkPipeline child = components.CreateSinkPipeline("child");
        child.AddSink(components.CreateNullSink("child-null"));
        LogSink childRoute = pipeline.InstallSubPipeline(child);
        Assert.That(pipeline.GetSubPipelines(), Is.EqualTo(new LogSinkPipeline[] { child }));
        Assert.That(pipeline.GetSubPipelineSinks(), Is.EqualTo(new LogSink[] { childRoute }));
        pipeline.SetSubPipelines(new LogSinkPipeline[] { child });
        pipeline.ClearSubPipelines();
        Assert.That(pipeline.IsEnabled(LogLevel.Info), Is.True);
        pipeline.SetShutdownTimeout(TimeSpan.FromSeconds(1));
        Assert.That(pipeline.Health().Name, Is.EqualTo("public-branch"));
        Assert.That(pipeline.Flush().IsSuccess, Is.True);
        Assert.That(pipeline.Drain().IsSuccess, Is.True);
        Assert.That(pipeline.Close().IsSuccess, Is.True);

        Assert.That(sink.Close().IsSuccess, Is.True);
        Assert.That(state.GetIsClosed(), Is.True);
        Assert.That(sink.Write(logEvent).Code, Is.EqualTo("sink.closed"));
    }

    [Test]
    public void BuilderFormatterRedactionDiagnosticsDurationAndReloadContracts_ExerciseEveryOperation()
    {
        string directory = NewTempDirectory("services");
        string jsonPath = Path.Combine(directory, "log.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(jsonPath, "{ \"name\": \"Application.Reload\", \"minimumLevel\": \"Debug\", \"sinks\": [\"memory\"] }");
        DateTimeOffset timestamp = new DateTimeOffset(2026, 8, 27, 4, 0, 0, TimeSpan.Zero);
        LogFactory factory = new LogFactory();
        ILogFactory components = factory;
        ILogEventBuilder builder = new LogEventBuilder(new ExternalClock(timestamp));
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        LogField password = components.CreateField("Password", "secret");

        builder.SetLevel(LogLevel.Error)
            .SetLoggerName("Application.Worker")
            .SetEvent(7, "Failed")
            .SetMessageTemplateAndValues("Failed {Password}", new object?[] { "secret" })
            .SetException(exceptionValue)
            .AddField(components.CreateField("Extra", 1))
            .AddField(null)
            .AddFields(new LogField[] { password })
            .AddFields(null)
            .SetTraceContext("trace", "span", "parent")
            .SetRuntimeMetadata(11, 22, "machine", "application")
            .SetRuntimeMetadataEnabled(false)
            .SetSourceHints("Worker.cs", "Run", 33)
            .AddRedactedFieldName("Password")
            .AddRedactedFieldName(null)
            .SetRedactedFieldNames(new string[] { "Password" });
        LogEvent built = builder.Build();

        Assert.That(built.Timestamp, Is.EqualTo(timestamp));
        Assert.That(built.Level, Is.EqualTo(LogLevel.Error));
        Assert.That(built.EventId, Is.EqualTo(7));
        Assert.That(built.ExceptionValue, Is.SameAs(exceptionValue));
        Assert.That(FieldValue(built.Fields, "Password").TextValue, Is.EqualTo("[redacted]"));
        Assert.That(built.ThreadId, Is.EqualTo(11));

        LogFormatter concreteFormatter = components.CreateNamedFormatter("parent");
        ILogFormatter formatter = concreteFormatter;
        ILogFormatterConfiguration formatterConfiguration = concreteFormatter;
        LogFormatter child = components.CreateCustomFormatter("child", logEvent => "child:" + logEvent.RenderedMessage);
        formatterConfiguration.SetFormatAction(logEvent => "parent:" + logEvent.RenderedMessage);
        Assert.That(formatter.Format(built), Does.StartWith("parent:"));
        formatterConfiguration.ClearFormatAction();
        formatterConfiguration.SetTimestampMode(LogTimestampMode.Local);
        Assert.That(formatterConfiguration.GetTimestampMode(), Is.EqualTo(LogTimestampMode.Local));
        formatterConfiguration.SetTimestampMode(LogTimestampMode.Utc);
        formatterConfiguration.SetFieldOrder(LogFieldOrder.Name);
        Assert.That(formatterConfiguration.GetFieldOrder(), Is.EqualTo(LogFieldOrder.Name));
        formatterConfiguration.SetFieldOrder(LogFieldOrder.Captured);
        Assert.That(formatterConfiguration.FormatLine(built), Is.Not.Null);
        formatterConfiguration.AddFormatter(child);
        Assert.That(formatterConfiguration.CanFormat("child"), Is.True);
        Assert.That(formatterConfiguration.FindFormatter("child"), Is.SameAs(child));
        Assert.That(formatterConfiguration.Dispatch("child", built), Does.StartWith("child:"));
        Assert.That(formatterConfiguration.DispatchLine("child", built), Does.StartWith("child:"));
        formatterConfiguration.SetFormatters(new LogFormatter[] { child });
        formatterConfiguration.ClearFormatters();
        Assert.That(formatterConfiguration.CanFormat("child"), Is.False);

        ILogRedaction redaction = new LogRedaction();
        LogValue redactedMarker = redaction.CreateRedactedValue();
        LogField[] redactedFields = redaction.RedactFields(new LogField[] { password }, new string[] { "password" });
        LogEvent redactedEvent = redaction.RedactEvent(built, new string[] { "password" });
        Assert.That(redactedMarker.TextValue, Is.EqualTo("[redacted]"));
        Assert.That(redactedFields[0].Value.TextValue, Is.EqualTo("[redacted]"));
        Assert.That(FieldValue(redactedEvent.Fields, "Password").TextValue, Is.EqualTo("[redacted]"));

        ILogDiagnostics diagnostics = new LogDiagnostics();
        int forwardedDiagnostics = 0;
        diagnostics.EnableMemory();
        Assert.That(diagnostics.IsEnabled(), Is.True);
        LogSink failing = components.CreateDelegateSink("failing", logEvent => new LogResult(false, true, false, false, "sink.failed", "failed", 0, 1, 0));
        failing.Write(built);
        Assert.That(diagnostics.GetWrittenCount(), Is.EqualTo(1));
        Assert.That(diagnostics.GetEntries(), Has.Length.EqualTo(1));
        Assert.That(diagnostics.GetDroppedReentrantCount(), Is.Zero);
        diagnostics.Enable(message => forwardedDiagnostics = forwardedDiagnostics + 1);
        failing.Write(built);
        Assert.That(forwardedDiagnostics, Is.EqualTo(1));
        diagnostics.Disable();
        diagnostics.Clear();
        Assert.That(diagnostics.IsEnabled(), Is.False);
        Assert.That(diagnostics.GetEntries(), Is.Empty);

        Logger durationLogger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        durationLogger.AddSink(components.CreateMemorySink("duration", 0));
        ILogDuration duration = ((ILoggerDuration)durationLogger).BeginDuration(LogLevel.Info, "elapsed", null, "Elapsed", TimeProvider.System);
        Assert.That(duration.GetIsCompleted(), Is.False);
        Assert.That(duration.GetFieldName(), Is.EqualTo("Elapsed"));
        Assert.That(duration.GetElapsed(), Is.EqualTo(TimeSpan.Zero));
        LogResult durationResult = duration.Complete();
        Assert.That(duration.GetIsCompleted(), Is.True);
        Assert.That(duration.GetElapsed(), Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        Assert.That(duration.GetResult(), Is.SameAs(durationResult));

        int configurationCallbacks = 0;
        int loggerCallbacks = 0;
        ILogConfigurationReload reload = components.WatchJsonFileWithEnvironment(jsonPath);
        reload.Environment(null!);
        reload.Environment(new Dictionary<string, string>());
        reload.OnConfigurationReloaded(configuration => configurationCallbacks = configurationCallbacks + 1);
        reload.OnLoggerReloaded(logger => loggerCallbacks = loggerCallbacks + 1);

        try
        {
            Assert.That(reload.GetFilePath(), Is.EqualTo(jsonPath));
            Assert.That(reload.GetIsWatching(), Is.False);
            Assert.That(reload.GetReloadCount(), Is.Zero);
            Assert.That(reload.GetFailureCount(), Is.Zero);
            Assert.That(reload.GetLastResult().IsSuccess, Is.True);
            Assert.That(reload.GetLastConfiguration(), Is.Not.Null);
            Assert.That(reload.GetLastLogger(), Is.Not.Null);
            Assert.That(reload.Start().IsSuccess, Is.True);
            Assert.That(reload.GetIsWatching(), Is.True);
            Assert.That(reload.Stop().IsSuccess, Is.True);
            Assert.That(reload.GetIsWatching(), Is.False);
            Assert.That(reload.Reload().IsSuccess, Is.True);
            Assert.That(reload.GetReloadCount(), Is.EqualTo(2));
            Assert.That(configurationCallbacks, Is.EqualTo(2));
            Assert.That(loggerCallbacks, Is.EqualTo(2));
            reload.Close();
            reload.Dispose();
        }
        finally
        {
            reload.Dispose();
            diagnostics.Disable();
            diagnostics.Clear();
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PublicNullAndEmptyBoundaries_ReturnStableSafeOutcomes()
    {
        LogFactory concreteFactory = new LogFactory();
        ILogFactory factory = concreteFactory;
        LogEvent logEvent = factory.CreateEvent(LogLevel.Info, "Application", "message", "message", new LogField[0]);
        LogFormatter nullFormatter = factory.WrapFormatter(null);
        LogFilter nullFilter = factory.WrapFilter(null!);
        LogSink nullInterfaceSink = factory.WrapSink(null!);

        Assert.That(((ILogFormatter)nullFormatter).Format(null), Is.Empty);
        Assert.That(((ILogFormatter)nullFormatter).Format(logEvent), Is.Not.Empty);
        Assert.That(((ILogFilter)nullFilter).Decide(null), Is.EqualTo(LogDecision.Neutral));
        Assert.That(((ILogFilter)nullFilter).Decide(logEvent), Is.EqualTo(LogDecision.Neutral));
        Assert.That(((ILogSinkState)nullInterfaceSink).GetIsEnabled(), Is.False);

        ILogConfiguration configuration = new LogConfiguration();
        configuration.SetSinks(null!);
        configuration.SetFilters(null!);
        configuration.SetGlobalFields(null!);
        configuration.SetInterfaceSinks(null!);
        configuration.SetInterfaceFilters(null!);
        configuration.SetInterfaceEnrichers(null!);
        configuration.AddSink(nullInterfaceSink);
        Assert.That(configuration.IsValid(), Is.False);
        Assert.That(configuration.ValidateResult().IsFailure, Is.True);

        Logger logger = ((ILoggerFactory)concreteFactory).CreateRootLoggerWithLevel(LogLevel.Trace);
        ILoggerPipeline loggerPipeline = logger;
        loggerPipeline.AddEnricher(null!);
        loggerPipeline.SetEnrichers(null!);
        loggerPipeline.SetFilters(null!);
        loggerPipeline.SetInterfaceFilters(null!);
        loggerPipeline.SetSinks(null!);
        loggerPipeline.SetInterfaceSinks(null!);
        loggerPipeline.SetSubPipelines(null!);
        Assert.That(logger.WriteWithValues(LogLevel.Info, null, null).IsSuccess, Is.True);
        LogDuration duration = ((ILoggerDuration)logger).BeginDuration(LogLevel.Info, null, null, null, null);
        Assert.That(((ILogDuration)duration).GetFieldName(), Is.EqualTo("Duration"));
        Assert.That(((ILogDuration)duration).Complete().IsSuccess, Is.True);

        LogSink memory = factory.CreateMemorySink("memory", 0);
        ILogSinkConfiguration sinkConfiguration = memory;
        sinkConfiguration.SetFilters(null!);
        sinkConfiguration.SetInterfaceFilters(null!);
        Assert.That(((ILogSink)memory).Write(logEvent).IsSuccess, Is.True);

        ILogSinkPipeline sinkPipeline = factory.CreateSinkPipeline("boundary");
        LogSink rejected = sinkPipeline.InstallSubPipeline(null!);
        Assert.That(rejected.GetIsEnabled(), Is.False);
        Assert.That(rejected.GetLastResult().Code, Is.EqualTo("pipeline.child.null"));
        sinkPipeline.SetSubPipelines(null!);
        Assert.That(sinkPipeline.GetSubPipelines(), Is.Empty);

        LogFormatter formatter = factory.CreateNamedFormatter("external-unknown");
        ILogFormatterConfiguration formatterConfiguration = formatter;
        formatterConfiguration.AddFormatter(null!);
        formatterConfiguration.SetFormatters(null!);
        Assert.That(((ILogFormatter)formatter).Format(null), Is.Empty);
        Assert.That(formatterConfiguration.FindFormatter("external-unknown"), Is.SameAs(formatter));

        ILogConfigurationReload missingReload = factory.WatchJsonFile("");
        LogResult missingResult = missingReload.Reload();
        Assert.That(missingResult.IsFailure, Is.True);
        Assert.That(missingResult.Code, Is.EqualTo("configuration.reload.path.missing"));
        Assert.That(missingReload.GetFailureCount(), Is.EqualTo(1));
        missingReload.Dispose();
    }

    static void ExerciseEveryLevelOperation(ILoggerLevels levels, Exception exceptionValue)
    {
        LogResult[] results = new LogResult[]
        {
            levels.Trace("trace"),
            levels.TraceWithValues("trace {Value}", new object?[] { 1 }),
            levels.TraceWithException(exceptionValue, "trace exception"),
            levels.TraceWithExceptionAndValues(exceptionValue, "trace exception {Value}", new object?[] { 1 }),
            levels.Debug("debug"),
            levels.DebugWithValues("debug {Value}", new object?[] { 1 }),
            levels.DebugWithException(exceptionValue, "debug exception"),
            levels.DebugWithExceptionAndValues(exceptionValue, "debug exception {Value}", new object?[] { 1 }),
            levels.Info("info"),
            levels.InfoWithValues("info {Value}", new object?[] { 1 }),
            levels.InfoWithException(exceptionValue, "info exception"),
            levels.InfoWithExceptionAndValues(exceptionValue, "info exception {Value}", new object?[] { 1 }),
            levels.Warn("warn"),
            levels.WarnWithValues("warn {Value}", new object?[] { 1 }),
            levels.WarnWithException(exceptionValue, "warn exception"),
            levels.WarnWithExceptionAndValues(exceptionValue, "warn exception {Value}", new object?[] { 1 }),
            levels.Error("error"),
            levels.ErrorWithValues("error {Value}", new object?[] { 1 }),
            levels.ErrorWithException(exceptionValue, "error exception"),
            levels.ErrorWithExceptionAndValues(exceptionValue, "error exception {Value}", new object?[] { 1 }),
            levels.Fatal("fatal"),
            levels.FatalWithValues("fatal {Value}", new object?[] { 1 }),
            levels.FatalWithException(exceptionValue, "fatal exception"),
            levels.FatalWithExceptionAndValues(exceptionValue, "fatal exception {Value}", new object?[] { 1 })
        };

        Assert.That(results, Has.All.Matches<LogResult>(result => result.IsSuccess));
    }

    static void ExerciseEveryContextOperation(ILoggerContext context, ILogFactory factory)
    {
        LogField field = factory.CreateField("Field", 1);
        LogField[] fields = new LogField[] { field };
        Logger[] children = new Logger[]
        {
            context.For("Worker"),
            context.ForTypeName("Company.Worker"),
            context.ForWithFields("Fields", fields),
            context.ForField("Value", 1),
            context.ForLogField(field),
            context.ForFields(fields),
            context.WithField("Value", 1),
            context.WithLogField(field),
            context.WithFields(fields),
            context.WithFieldGroup("Group", fields),
            context.WithFieldNamespace("Company.Service", fields)
        };
        LogScope[] scopes = new LogScope[]
        {
            context.BeginScope(fields),
            context.BeginScopeField("Scope", 1),
            context.BeginScopeLogField(field),
            context.BeginScopeFieldGroup("Group", fields),
            context.BeginScopeFieldNamespace("Company.Service", fields)
        };

        Assert.That(children, Has.Length.EqualTo(11));
        Assert.That(children, Has.All.Not.Null);
        int index = scopes.Length - 1;
        while (index >= 0)
        {
            ILogScope scope = scopes[index];
            scope.Dispose();
            scope.Dispose();
            index = index - 1;
        }
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

    static string NewTempDirectory(string name)
    {
        return Path.Combine(Path.GetTempPath(), "log-external-consumer", Guid.NewGuid().ToString("N"), name);
    }

    sealed class ExternalClock : ILogClock
    {
        readonly DateTimeOffset value;

        public ExternalClock(DateTimeOffset timestamp)
        {
            value = timestamp;
        }

        public DateTimeOffset UtcNow()
        {
            return value;
        }
    }

    sealed class ExternalEnricher : ILogEnricher
    {
        public LogField[] Enrich(LogLevel level, string loggerName)
        {
            return new LogField[] { new LogFactory().CreateStringField("External", "enriched") };
        }
    }

    sealed class ExternalFilter : ILogFilter
    {
        readonly LogDecision decision;

        public ExternalFilter(LogDecision result)
        {
            decision = result;
        }

        public LogDecision Decide(LogEvent? logEvent)
        {
            return decision;
        }
    }

    sealed class ExternalFormatter : ILogFormatter
    {
        public string Format(LogEvent? logEvent)
        {
            if (logEvent == null)
            {
                return "";
            }

            return logEvent.RenderedMessage;
        }
    }

    sealed class ExternalSink : ILogSink
    {
        readonly string name;
        public int WriteCount;
        public int FlushCount;
        public int DrainCount;
        public int CloseCount;

        public ExternalSink(string sinkName)
        {
            name = sinkName;
        }

        public string GetName()
        {
            return name;
        }

        public bool IsEnabledForLevel(LogLevel level)
        {
            return level != LogLevel.Off;
        }

        public LogResult Write(LogEvent? logEvent)
        {
            WriteCount = WriteCount + 1;
            if (logEvent == null)
            {
                return new LogResult(false, true, false, false, "event.null", "event is null", 0, 1, 0);
            }

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
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }
    }
}
