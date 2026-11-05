using log;
using NUnit.Framework;

namespace log.PublicApiTests;

[TestFixture]
public class PublicLoggerOperationCoverageTests
{
    [Test]
    public void LevelSwitchAndLoggerEnabledChecks_CoverEveryLevelAndNormalizationBoundary()
    {
        ILogLevelSwitch levelSwitch = new LogLevelSwitch();
        Assert.That(levelSwitch.GetLevel(), Is.EqualTo(LogLevel.Info));
        Assert.That(levelSwitch.IsEnabled(LogLevel.Trace), Is.False);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Debug), Is.False);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Info), Is.True);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Warn), Is.True);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Error), Is.True);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Fatal), Is.True);
        Assert.That(levelSwitch.IsEnabled(LogLevel.Off), Is.False);

        levelSwitch.SetLevel((LogLevel)(-1));
        Assert.That(levelSwitch.GetLevel(), Is.EqualTo(LogLevel.Trace));
        Assert.That(levelSwitch.IsEnabled((LogLevel)(-1)), Is.True);

        levelSwitch.SetLevel((LogLevel)int.MaxValue);
        Assert.That(levelSwitch.GetLevel(), Is.EqualTo(LogLevel.Off));
        foreach (LogLevel level in Enum.GetValues<LogLevel>())
        {
            Assert.That(levelSwitch.IsEnabled(level), Is.False);
        }

        LogLevelSwitch sharedSwitch = new LogLevelSwitch();
        sharedSwitch.SetLevel(LogLevel.Error);
        Logger logger = new LogFactory().CreateNamedLoggerWithSwitch("Application", sharedSwitch);
        Assert.That(logger.IsEnabled(LogLevel.Trace), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Debug), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Info), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Warn), Is.False);
        Assert.That(logger.IsEnabled(LogLevel.Error), Is.True);
        Assert.That(logger.IsEnabled(LogLevel.Fatal), Is.True);
        Assert.That(logger.IsEnabled(LogLevel.Off), Is.False);

        sharedSwitch.SetLevel(LogLevel.Debug);
        Assert.That(logger.GetMinimumLevel(), Is.EqualTo(LogLevel.Debug));
        Assert.That(logger.IsEnabled(LogLevel.Debug), Is.True);
    }

    [Test]
    public void GeneralWriteOperations_CoverValuesExceptionsEventsAndNullEventFailure()
    {
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;
        Logger concreteLogger = ((ILoggerFactory)factory).CreateNamedLoggerWithLevel("Application.Worker", LogLevel.Trace);
        ILogger logger = concreteLogger;
        LogSink sink = componentFactory.CreateMemorySink("memory", 0);
        ((ILoggerPipeline)concreteLogger).AddSink(sink);
        InvalidOperationException exceptionValue = new InvalidOperationException("failed");
        LogEvent explicitEvent = CreateEvent(LogLevel.Warn, "Application.Explicit", "explicit");

        LogResult valuesResult = logger.WriteWithValues(LogLevel.Info, "Processed {Count}", new object?[] { 3 });
        LogResult exceptionResult = logger.WriteWithExceptionAndValues(LogLevel.Error, exceptionValue, "Failed {Operation}", new object?[] { "sync" });
        LogResult eventResult = logger.WriteEvent(explicitEvent);
        LogResult nullResult = logger.WriteEvent(null);
        LogEvent[] events = componentFactory.GetMemorySinkEvents(sink);

        Assert.That(valuesResult.IsSuccess, Is.True);
        Assert.That(exceptionResult.IsSuccess, Is.True);
        Assert.That(eventResult.IsSuccess, Is.True);
        Assert.That(nullResult.IsFailure, Is.True);
        Assert.That(nullResult.Code, Is.EqualTo("logger.event.null"));
        Assert.That(events, Has.Length.EqualTo(3));
        Assert.That(events[0].Level, Is.EqualTo(LogLevel.Info));
        Assert.That(events[0].RenderedMessage, Is.EqualTo("Processed 3"));
        Assert.That(events[0].Fields[0].Name, Is.EqualTo("Count"));
        Assert.That(events[1].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(events[1].RenderedMessage, Is.EqualTo("Failed sync"));
        Assert.That(events[1].ExceptionValue, Is.SameAs(exceptionValue));
        Assert.That(events[2], Is.SameAs(explicitEvent));
        Assert.That(concreteLogger.GetLastWriteResult(), Is.SameAs(nullResult));
        Assert.That(concreteLogger.GetLastEvent(), Is.SameAs(explicitEvent));
        Assert.That(concreteLogger.GetWrittenCount(), Is.EqualTo(3));
        Assert.That(concreteLogger.GetFailureCount(), Is.EqualTo(1));
    }

    [Test]
    public void ConvenienceLevelOperations_CoverEveryPlainValueAndExceptionForm()
    {
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;
        Logger logger = ((ILoggerFactory)factory).CreateRootLoggerWithLevel(LogLevel.Trace);
        ILoggerLevels levels = logger;
        LogSink sink = componentFactory.CreateMemorySink("memory", 0);
        ((ILoggerPipeline)logger).AddSink(sink);
        InvalidOperationException exceptionValue = new InvalidOperationException("failure");

        LogResult[] results = new LogResult[]
        {
            levels.Trace("trace plain"),
            levels.TraceWithValues("trace {Value}", new object?[] { 1 }),
            levels.TraceWithException(exceptionValue, "trace exception"),
            levels.TraceWithExceptionAndValues(exceptionValue, "trace exception {Value}", new object?[] { 2 }),
            levels.Debug("debug plain"),
            levels.DebugWithValues("debug {Value}", new object?[] { 1 }),
            levels.DebugWithException(exceptionValue, "debug exception"),
            levels.DebugWithExceptionAndValues(exceptionValue, "debug exception {Value}", new object?[] { 2 }),
            levels.Info("info plain"),
            levels.InfoWithValues("info {Value}", new object?[] { 1 }),
            levels.InfoWithException(exceptionValue, "info exception"),
            levels.InfoWithExceptionAndValues(exceptionValue, "info exception {Value}", new object?[] { 2 }),
            levels.Warn("warn plain"),
            levels.WarnWithValues("warn {Value}", new object?[] { 1 }),
            levels.WarnWithException(exceptionValue, "warn exception"),
            levels.WarnWithExceptionAndValues(exceptionValue, "warn exception {Value}", new object?[] { 2 }),
            levels.Error("error plain"),
            levels.ErrorWithValues("error {Value}", new object?[] { 1 }),
            levels.ErrorWithException(exceptionValue, "error exception"),
            levels.ErrorWithExceptionAndValues(exceptionValue, "error exception {Value}", new object?[] { 2 }),
            levels.Fatal("fatal plain"),
            levels.FatalWithValues("fatal {Value}", new object?[] { 1 }),
            levels.FatalWithException(exceptionValue, "fatal exception"),
            levels.FatalWithExceptionAndValues(exceptionValue, "fatal exception {Value}", new object?[] { 2 })
        };

        foreach (LogResult result in results)
        {
            Assert.That(result.IsSuccess, Is.True);
        }

        LogEvent[] events = componentFactory.GetMemorySinkEvents(sink);
        Assert.That(events, Has.Length.EqualTo(24));
        AssertLevelGroup(events, 0, LogLevel.Trace, exceptionValue);
        AssertLevelGroup(events, 4, LogLevel.Debug, exceptionValue);
        AssertLevelGroup(events, 8, LogLevel.Info, exceptionValue);
        AssertLevelGroup(events, 12, LogLevel.Warn, exceptionValue);
        AssertLevelGroup(events, 16, LogLevel.Error, exceptionValue);
        AssertLevelGroup(events, 20, LogLevel.Fatal, exceptionValue);
        Assert.That(events[1].RenderedMessage, Is.EqualTo("trace 1"));
        Assert.That(events[3].RenderedMessage, Is.EqualTo("trace exception 2"));
        Assert.That(events[21].RenderedMessage, Is.EqualTo("fatal 1"));
        Assert.That(events[23].RenderedMessage, Is.EqualTo("fatal exception 2"));
    }

    [Test]
    public void CategoryHierarchy_UsesMostSpecificOverrideUpdatesAndValidation()
    {
        LogFactory factory = new LogFactory();
        ILogFactory componentFactory = factory;
        Logger root = ((ILoggerFactory)factory).CreateNamedLoggerWithLevel("Application", LogLevel.Trace);
        ILoggerPipeline rootPipeline = root;
        LogSink sink = componentFactory.CreateMemorySink("memory", 0);
        rootPipeline.AddSink(sink);
        Assert.That(rootPipeline.SetCategoryLevel("Application.Services", LogLevel.Warn).IsSuccess, Is.True);
        Assert.That(rootPipeline.SetCategoryLevel("Application.Services.Worker", LogLevel.Error).IsSuccess, Is.True);

        Logger services = ((ILoggerContext)root).For("Services");
        Logger worker = ((ILoggerContext)services).For("Worker");
        Logger detail = ((ILoggerContext)worker).For("Detail");
        Logger sibling = ((ILoggerContext)services).For("Other");

        Assert.That(root.GetMinimumLevel(), Is.EqualTo(LogLevel.Trace));
        Assert.That(services.GetMinimumLevel(), Is.EqualTo(LogLevel.Warn));
        Assert.That(worker.GetMinimumLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(detail.GetMinimumLevel(), Is.EqualTo(LogLevel.Error));
        Assert.That(sibling.GetMinimumLevel(), Is.EqualTo(LogLevel.Warn));
        Assert.That(detail.IsEnabled(LogLevel.Warn), Is.False);
        Assert.That(detail.IsEnabled(LogLevel.Error), Is.True);

        LogResult dropped = detail.WriteWithValues(LogLevel.Warn, "warn", null);
        LogResult accepted = detail.WriteWithValues(LogLevel.Error, "error", null);
        Assert.That(dropped.IsDropped, Is.True);
        Assert.That(accepted.IsSuccess, Is.True);
        Assert.That(componentFactory.GetMemorySinkEvents(sink)[0].LoggerName, Is.EqualTo("Application.Services.Worker.Detail"));

        Assert.That(rootPipeline.SetCategoryLevel("Application.Services.Worker", LogLevel.Debug).IsSuccess, Is.True);
        Assert.That(detail.GetMinimumLevel(), Is.EqualTo(LogLevel.Debug));
        Assert.That(detail.IsEnabled(LogLevel.Info), Is.True);
        LogResult invalid = rootPipeline.SetCategoryLevel("bad category", LogLevel.Info);
        Assert.That(invalid.IsFailure, Is.True);
        Assert.That(invalid.Code, Is.EqualTo("logger.category.invalid"));
    }

    [Test]
    public void NoOpLogger_DropsEveryWriteFormWithoutCreatingEvents()
    {
        ILoggerFactory factory = new LogFactory();
        Logger concreteLogger = factory.CreateNoOpLogger();
        ILogger logger = concreteLogger;
        ILoggerLevels levels = concreteLogger;
        InvalidOperationException exceptionValue = new InvalidOperationException("ignored");
        LogEvent explicitEvent = CreateEvent(LogLevel.Fatal, "Application", "explicit");

        LogResult values = logger.WriteWithValues(LogLevel.Fatal, "value {Value}", new object?[] { 1 });
        LogResult exception = logger.WriteWithExceptionAndValues(LogLevel.Error, exceptionValue, "failed", null);
        LogResult eventResult = logger.WriteEvent(explicitEvent);
        LogResult convenience = levels.Info("info");

        Assert.That(values.IsDropped, Is.True);
        Assert.That(exception.IsDropped, Is.True);
        Assert.That(eventResult.IsDropped, Is.True);
        Assert.That(convenience.IsDropped, Is.True);
        foreach (LogLevel level in Enum.GetValues<LogLevel>())
        {
            Assert.That(logger.IsEnabled(level), Is.False);
        }

        Assert.That(concreteLogger.GetIsNoOp(), Is.True);
        Assert.That(concreteLogger.GetLastEvent(), Is.Null);
        Assert.That(concreteLogger.GetWrittenCount(), Is.Zero);
        Assert.That(concreteLogger.GetDroppedCount(), Is.EqualTo(4));
        Assert.That(concreteLogger.GetFailureCount(), Is.Zero);
        Assert.That(concreteLogger.GetDiagnosticCount(), Is.Zero);
        Assert.That(concreteLogger.GetLastWriteResult(), Is.SameAs(convenience));
    }

    [Test]
    public void LoggerStateCounters_TrackResultsAndEveryResetOperation()
    {
        LogFactory factory = new LogFactory();
        Logger logger = ((ILoggerFactory)factory).CreateNamedLoggerWithLevel("Application.State", LogLevel.Trace);
        ILoggerState state = logger;
        SequenceSink destination = new SequenceSink();
        ((ILoggerPipeline)logger).AddInterfaceSink(destination);

        logger.WriteWithValues(LogLevel.Info, "success", null);
        logger.WriteWithValues(LogLevel.Info, "dropped", null);
        logger.WriteWithValues(LogLevel.Info, "failure", null);
        LogResult diagnostic = logger.WriteWithValues(LogLevel.Info, "diagnostic", null);

        Assert.That(state.GetName(), Is.EqualTo("Application.State"));
        Assert.That(state.GetIsNoOp(), Is.False);
        Assert.That(state.GetIsValid(), Is.True);
        Assert.That(state.GetErrorMessage(), Is.Empty);
        Assert.That(state.GetMinimumLevel(), Is.EqualTo(LogLevel.Trace));
        Assert.That(state.GetWrittenCount(), Is.EqualTo(2));
        Assert.That(state.GetDroppedCount(), Is.EqualTo(2));
        Assert.That(state.GetFailureCount(), Is.EqualTo(3));
        Assert.That(state.GetDiagnosticCount(), Is.EqualTo(4));
        Assert.That(state.GetLastEvent(), Is.Not.Null);
        Assert.That(state.GetLastEvent()!.RenderedMessage, Is.EqualTo("diagnostic"));
        Assert.That(state.GetLastWriteResult(), Is.SameAs(diagnostic));
        Assert.That(state.GetLastWriteResult().IsDiagnostic, Is.True);

        state.ClearDroppedCount();
        Assert.That(state.GetDroppedCount(), Is.Zero);
        Assert.That(state.GetFailureCount(), Is.EqualTo(3));
        Assert.That(state.GetDiagnosticCount(), Is.EqualTo(4));
        state.ClearFailureCount();
        Assert.That(state.GetFailureCount(), Is.Zero);
        Assert.That(state.GetDiagnosticCount(), Is.EqualTo(4));
        state.ClearDiagnosticCount();
        Assert.That(state.GetDiagnosticCount(), Is.Zero);
        logger.WriteWithValues(LogLevel.Info, "another diagnostic", null);
        Assert.That(state.GetDiagnosticCount(), Is.EqualTo(4));
        Assert.That(state.GetWrittenCount(), Is.EqualTo(3));
        state.ClearCounters();
        Assert.That(state.GetDroppedCount(), Is.Zero);
        Assert.That(state.GetFailureCount(), Is.Zero);
        Assert.That(state.GetDiagnosticCount(), Is.Zero);
        Assert.That(state.GetWrittenCount(), Is.EqualTo(3));
    }

    static LogEvent CreateEvent(LogLevel level, string loggerName, string message)
    {
        return new LogEvent(DateTimeOffset.UnixEpoch, level, loggerName, 0, "", message, message, new LogField[0], null, "", "", "", 0, 0, "", "", "", "", 0);
    }

    static void AssertLevelGroup(LogEvent[] events, int startIndex, LogLevel level, Exception exceptionValue)
    {
        Assert.That(events[startIndex].Level, Is.EqualTo(level));
        Assert.That(events[startIndex + 1].Level, Is.EqualTo(level));
        Assert.That(events[startIndex + 2].Level, Is.EqualTo(level));
        Assert.That(events[startIndex + 3].Level, Is.EqualTo(level));
        Assert.That(events[startIndex].ExceptionValue, Is.Null);
        Assert.That(events[startIndex + 1].ExceptionValue, Is.Null);
        Assert.That(events[startIndex + 2].ExceptionValue, Is.SameAs(exceptionValue));
        Assert.That(events[startIndex + 3].ExceptionValue, Is.SameAs(exceptionValue));
    }

    class SequenceSink : ILogSink
    {
        int writeCount;

        public string GetName()
        {
            return "sequence";
        }

        public bool IsEnabledForLevel(LogLevel level)
        {
            return true;
        }

        public LogResult Write(LogEvent? logEvent)
        {
            writeCount = writeCount + 1;
            if (writeCount == 1)
            {
                return new LogResult(true, false, false, false, "", "", 0, 0, 0);
            }

            if (writeCount == 2)
            {
                return new LogResult(false, false, true, false, "sink.dropped", "dropped", 2, 0, 0);
            }

            if (writeCount == 3)
            {
                return new LogResult(false, true, false, false, "sink.failed", "failed", 0, 3, 0);
            }

            return new LogResult(true, false, false, true, "sink.diagnostic", "diagnostic", 0, 0, 4);
        }

        public LogResult Flush()
        {
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Drain()
        {
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }

        public LogResult Close()
        {
            return new LogResult(true, false, false, false, "", "", 0, 0, 0);
        }
    }
}
