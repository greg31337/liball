using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Builds structured events, applies context and filters, and writes accepted events to its pipeline.
/// </summary>
/// <remarks>
/// Disabled events are rejected before message-template binding and event allocation. Writes return LogResult so sink failures and drops remain observable.
/// </remarks>
public class Logger : ILogger, ILoggerState, ILoggerLevels, ILoggerContext, ILoggerPipeline, ILoggerDuration
{
    /// <summary>
    /// The normalized name used to identify this object.
    /// </summary>
    internal string Name = "";
    internal LogLevelSwitch LevelSwitch = new LogLevelSwitch();
    internal ILogClock Clock = LogClock.SystemClock();
    /// <summary>
    /// Whether this logger intentionally discards all events.
    /// </summary>
    internal bool IsNoOp;
    /// <summary>
    /// Whether the object passed validation and can be used normally.
    /// </summary>
    internal bool IsValid;
    /// <summary>
    /// The validation error text, or an empty string for a valid object.
    /// </summary>
    internal string ErrorMessage = "";
    internal Dictionary<string, LogLevelSwitch> CategoryLevelSwitches = new Dictionary<string, LogLevelSwitch>();
    internal LogSinkThresholds SinkThresholds = new LogSinkThresholds();
    /// <summary>
    /// The most recent event accepted by this object, or null when none has been accepted.
    /// </summary>
    internal LogEvent? LastEvent;
    /// <summary>
    /// The result returned by the logger's most recent write.
    /// </summary>
    internal LogResult LastWriteResult = LogResult.Success();
    /// <summary>
    /// The cumulative number of accepted write operations.
    /// </summary>
    internal int WrittenCount;
    /// <summary>
    /// The cumulative number of dropped events.
    /// </summary>
    internal int DroppedCount;
    /// <summary>
    /// The number of failed operations observed by this object.
    /// </summary>
    internal int FailureCount;
    /// <summary>
    /// The cumulative number of diagnostic outcomes.
    /// </summary>
    internal int DiagnosticCount;
    internal LogField[] BoundFields = new LogField[0];
    internal LogScopeStorage ScopeStorage = new LogScopeStorage();
    internal LogActivityEnricher ActivityEnricher = new LogActivityEnricher();
    internal LogBuiltInEnricher BuiltInEnricher = new LogBuiltInEnricher();
    internal ILogEnricher[] Enrichers = new ILogEnricher[0];
    internal bool IncludeRuntimeMetadata;
    internal LogFilterPipeline FilterPipeline = new LogFilterPipeline();
    internal LogPipeline Pipeline = new LogPipeline();
    internal object SyncRoot = new object();

    internal Logger(string? name, LogLevelSwitch? levelSwitch, bool isNoOp, ILogClock? clock)
    {
        Name = LogName.NormalizeLoggerName(name);
        IsNoOp = isNoOp;
        IsValid = true;
        ErrorMessage = "";
        CategoryLevelSwitches = new Dictionary<string, LogLevelSwitch>();
        SinkThresholds = new LogSinkThresholds();
        LastEvent = null;
        LastWriteResult = LogResult.Success();
        WrittenCount = 0;
        DroppedCount = 0;
        FailureCount = 0;
        DiagnosticCount = 0;
        BoundFields = new LogField[0];
        ScopeStorage = new LogScopeStorage();
        ActivityEnricher = new LogActivityEnricher();
        BuiltInEnricher = new LogBuiltInEnricher();
        Enrichers = new ILogEnricher[0];
        IncludeRuntimeMetadata = true;
        FilterPipeline = new LogFilterPipeline();
        Pipeline = new LogPipeline();
        SyncRoot = new object();

        if (levelSwitch == null)
        {
            if (isNoOp)
            {
                LevelSwitch = LogLevelSwitch.Create(LogLevel.Off);
            }
            else
            {
                LevelSwitch = LogLevelSwitch.Create(LogLevel.Info);
            }
        }
        else
        {
            LevelSwitch = levelSwitch;
        }

        if (clock == null)
        {
            Clock = LogClock.SystemClock();
        }
        else
        {
            Clock = clock;
        }

        if (Name.Length > 0)
        {
            string validationError = LogName.ValidateLoggerName(Name);
            if (validationError.Length > 0)
            {
                IsValid = false;
                ErrorMessage = validationError;
            }
        }
    }

    /// <summary>
    /// Creates an unnamed root logger with the default minimum level.
    /// </summary>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger Root()
    {
        return new Logger("", LogLevelSwitch.Create(LogLevel.Info), false, LogClock.SystemClock());
    }

    /// <summary>
    /// Creates an unnamed root logger with a fixed minimum level.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger RootWithLevel(LogLevel level)
    {
        return new Logger("", LogLevelSwitch.Create(level), false, LogClock.SystemClock());
    }

    /// <summary>
    /// Creates an unnamed root logger that shares the supplied runtime level switch.
    /// </summary>
    /// <param name="levelSwitch">The shared runtime level switch.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger RootWithSwitch(LogLevelSwitch levelSwitch)
    {
        return new Logger("", levelSwitch, false, LogClock.SystemClock());
    }

    /// <summary>
    /// Creates a logger that intentionally drops every event.
    /// </summary>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger NoOp()
    {
        return new Logger("", LogLevelSwitch.Create(LogLevel.Off), true, LogClock.SystemClock());
    }

    /// <summary>
    /// Returns the logger's current runtime-adjustable minimum level.
    /// </summary>
    /// <returns>The normalized current level.</returns>
    /// <summary>Returns the logger name.</summary>
    public string GetName()
    {
        return Name;
    }

    /// <summary>Reports whether the logger intentionally discards events.</summary>
    public bool GetIsNoOp()
    {
        return IsNoOp;
    }

    /// <summary>Reports whether the logger configuration is valid.</summary>
    public bool GetIsValid()
    {
        return IsValid;
    }

    /// <summary>Returns the logger validation error.</summary>
    public string GetErrorMessage()
    {
        return ErrorMessage;
    }

    /// <summary>Returns the most recently accepted event.</summary>
    public LogEvent? GetLastEvent()
    {
        return LastEvent;
    }

    /// <summary>Returns the most recent write result.</summary>
    public LogResult GetLastWriteResult()
    {
        return LastWriteResult;
    }

    /// <summary>Returns the accepted-write count.</summary>
    public int GetWrittenCount()
    {
        return WrittenCount;
    }

    /// <summary>Returns the dropped-event count.</summary>
    public int GetDroppedCount()
    {
        return DroppedCount;
    }

    /// <summary>Returns the failure count.</summary>
    public int GetFailureCount()
    {
        return FailureCount;
    }

    /// <summary>Returns the diagnostic count.</summary>
    public int GetDiagnosticCount()
    {
        return DiagnosticCount;
    }

    /// <summary>Returns the effective minimum level.</summary>
    public LogLevel GetMinimumLevel()
    {
        if (IsNoOp)
        {
            return LogLevel.Off;
        }

        return FindCategoryLevelSwitch(Name).GetLevel();
    }

    /// <summary>
    /// Checks category, sink thresholds, and pipeline availability before event allocation.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    public bool IsEnabled(LogLevel level)
    {
        if (IsNoOp)
        {
            return false;
        }

        if (!IsValid)
        {
            return false;
        }

        if (!Logger.IsEnabledForSwitchAndSink(level, FindCategoryLevelSwitch(Name), SinkThresholds))
        {
            return false;
        }

        if (Pipeline == null)
        {
            return true;
        }

        return Pipeline.IsEnabled(level);
    }

    /// <summary>
    /// Writes a specified-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal LogResult Write(LogLevel level, string messageTemplate)
    {
        return WriteWithExceptionAndValues(level, null, messageTemplate, new object[0]);
    }

    /// <summary>
    /// Writes a specified-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult WriteWithValues(LogLevel level, string? messageTemplate, object?[]? values)
    {
        return WriteWithExceptionAndValues(level, null, messageTemplate, values);
    }

    /// <summary>
    /// Writes a specified-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal LogResult WriteWithException(LogLevel level, Exception? exceptionValue, string messageTemplate)
    {
        return WriteWithExceptionAndValues(level, exceptionValue, messageTemplate, new object[0]);
    }

    /// <summary>
    /// Writes a specified-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult WriteWithExceptionAndValues(LogLevel level, Exception? exceptionValue, string? messageTemplate, object?[]? values)
    {
        if (!IsEnabled(level))
        {
            return CompleteWrite(DisabledWriteResult());
        }

        string normalizedMessageTemplate = LogResult.NormalizeText(messageTemplate);
        LogTemplate template = LogTemplateParser.ParseTemplate(normalizedMessageTemplate);
        object?[] normalizedValues = NormalizeValues(values);
        LogField[] fields = LogTemplateBinder.BindTemplate(template, normalizedValues);
        LogTemplateRenderer renderer = new LogTemplateRenderer();
        string renderedMessage = renderer.RenderFields(template, fields);
        LogEvent logEvent = CreateEventBuilder(level).SetException(exceptionValue).SetMessage(normalizedMessageTemplate, renderedMessage).AddFields(fields).Build();

        return WriteEvent(logEvent);
    }

    /// <summary>
    /// Begins a duration measurement that writes through this logger when completed.
    /// </summary>
    /// <param name="level">The level for the completion event.</param>
    /// <param name="messageTemplate">The completion event message template.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <param name="durationFieldName">The structured duration field name, or empty for <c>Duration</c>.</param>
    /// <param name="timeProvider">The monotonic .NET time source, or null for <see cref="TimeProvider.System"/>.</param>
    /// <returns>A logger-bound duration measurement.</returns>
    public LogDuration BeginDuration(LogLevel level, string? messageTemplate, object?[]? values, string? durationFieldName, TimeProvider? timeProvider)
    {
        return new LogDuration(this, level, messageTemplate, values, durationFieldName, timeProvider);
    }

    internal LogResult WriteDurationCompletion(LogLevel level, string messageTemplate, object?[] values, string durationFieldName, TimeSpan elapsed)
    {
        if (!IsEnabled(level))
        {
            return CompleteWrite(DisabledWriteResult());
        }

        LogEvent logEvent = CreateEventBuilder(level)
            .SetMessageTemplateAndValues(messageTemplate, values)
            .AddField(new LogField(durationFieldName, LogValue.Duration(elapsed)))
            .Build();
        return WriteEvent(logEvent);
    }

    /// <summary>
    /// Writes an already constructed event through logger filters and sinks.
    /// </summary>
    /// <param name="logEvent">The event to evaluate, format, or write.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult WriteEvent(LogEvent? logEvent)
    {
        lock (SyncRoot)
        {
            return WriteEventCore(logEvent);
        }
    }

    internal LogResult WriteEventCore(LogEvent? logEvent)
    {
        if (logEvent == null)
        {
            return CompleteWrite(LogResult.FailureCode("logger.event.null", "log event cannot be null"));
        }

        if (!IsEnabled(logEvent.Level))
        {
            return CompleteWrite(DisabledWriteResult());
        }

        if (FilterPipeline.Decide(logEvent) == LogDecision.Exclude)
        {
            return CompleteWrite(LogResult.Dropped("log event filtered"));
        }

        LogResult pipelineResult = Pipeline.Write(logEvent);
        CompleteWrite(pipelineResult);
        if (!pipelineResult.ShouldContinue())
        {
            return LastWriteResult;
        }

        LastEvent = logEvent;
        WrittenCount = LogResult.IncrementCount(WrittenCount);
        return LastWriteResult;
    }

    internal LogResult CompleteWrite(LogResult result)
    {
        lock (SyncRoot)
        {
            LastWriteResult = LogSink.NormalizeResult(result);
            DroppedCount = LogResult.AddCounts(DroppedCount, LastWriteResult.DroppedCount);
            FailureCount = LogResult.AddCounts(FailureCount, LastWriteResult.FailureCount);
            DiagnosticCount = LogResult.AddCounts(DiagnosticCount, LastWriteResult.DiagnosticCount);
            LogSelfLog.WriteResult("logger " + Name, LastWriteResult);
            return LastWriteResult;
        }
    }

    /// <summary>
    /// Clears dropped count and restores the default behavior.
    /// </summary>
    public void ClearDroppedCount()
    {
        DroppedCount = 0;
    }

    /// <summary>
    /// Clears failure count and restores the default behavior.
    /// </summary>
    public void ClearFailureCount()
    {
        FailureCount = 0;
    }

    /// <summary>
    /// Clears diagnostic count and restores the default behavior.
    /// </summary>
    public void ClearDiagnosticCount()
    {
        DiagnosticCount = 0;
    }

    /// <summary>
    /// Clears counters and restores the default behavior.
    /// </summary>
    public void ClearCounters()
    {
        DroppedCount = 0;
        FailureCount = 0;
        DiagnosticCount = 0;
    }

    /// <summary>
    /// Writes a trace-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Trace(string messageTemplate)
    {
        return Write(LogLevel.Trace, messageTemplate);
    }

    /// <summary>
    /// Writes a trace-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult TraceWithValues(string messageTemplate, object?[]? values)
    {
        return WriteWithValues(LogLevel.Trace, messageTemplate, values);
    }

    /// <summary>
    /// Writes a trace-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult TraceWithException(Exception exceptionValue, string messageTemplate)
    {
        return WriteWithException(LogLevel.Trace, exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a trace-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult TraceWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return WriteWithExceptionAndValues(LogLevel.Trace, exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a debug-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Debug(string messageTemplate)
    {
        return Write(LogLevel.Debug, messageTemplate);
    }

    /// <summary>
    /// Writes a debug-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult DebugWithValues(string messageTemplate, object?[]? values)
    {
        return WriteWithValues(LogLevel.Debug, messageTemplate, values);
    }

    /// <summary>
    /// Writes a debug-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult DebugWithException(Exception exceptionValue, string messageTemplate)
    {
        return WriteWithException(LogLevel.Debug, exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a debug-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult DebugWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return WriteWithExceptionAndValues(LogLevel.Debug, exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a info-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Info(string messageTemplate)
    {
        return Write(LogLevel.Info, messageTemplate);
    }

    /// <summary>
    /// Writes a info-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult InfoWithValues(string messageTemplate, object?[]? values)
    {
        return WriteWithValues(LogLevel.Info, messageTemplate, values);
    }

    /// <summary>
    /// Writes a info-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult InfoWithException(Exception exceptionValue, string messageTemplate)
    {
        return WriteWithException(LogLevel.Info, exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a info-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult InfoWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return WriteWithExceptionAndValues(LogLevel.Info, exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a warn-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Warn(string messageTemplate)
    {
        return Write(LogLevel.Warn, messageTemplate);
    }

    /// <summary>
    /// Writes a warn-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult WarnWithValues(string messageTemplate, object?[]? values)
    {
        return WriteWithValues(LogLevel.Warn, messageTemplate, values);
    }

    /// <summary>
    /// Writes a warn-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult WarnWithException(Exception exceptionValue, string messageTemplate)
    {
        return WriteWithException(LogLevel.Warn, exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a warn-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult WarnWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return WriteWithExceptionAndValues(LogLevel.Warn, exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a error-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Error(string messageTemplate)
    {
        return Write(LogLevel.Error, messageTemplate);
    }

    /// <summary>
    /// Writes a error-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult ErrorWithValues(string messageTemplate, object?[]? values)
    {
        return WriteWithValues(LogLevel.Error, messageTemplate, values);
    }

    /// <summary>
    /// Writes a error-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult ErrorWithException(Exception exceptionValue, string messageTemplate)
    {
        return WriteWithException(LogLevel.Error, exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a error-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult ErrorWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return WriteWithExceptionAndValues(LogLevel.Error, exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a fatal-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Fatal(string messageTemplate)
    {
        return Write(LogLevel.Fatal, messageTemplate);
    }

    /// <summary>
    /// Writes a fatal-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult FatalWithValues(string messageTemplate, object?[]? values)
    {
        return WriteWithValues(LogLevel.Fatal, messageTemplate, values);
    }

    /// <summary>
    /// Writes a fatal-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult FatalWithException(Exception exceptionValue, string messageTemplate)
    {
        return WriteWithException(LogLevel.Fatal, exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a fatal-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult FatalWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return WriteWithExceptionAndValues(LogLevel.Fatal, exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Creates a child logger whose name is combined with the current logger name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    public Logger For(string name)
    {
        string childName = Logger.CombineNames(Name, name);
        Logger child = new Logger(childName, LevelSwitch, IsNoOp, Clock);
        child.CategoryLevelSwitches = CategoryLevelSwitches;
        child.SinkThresholds = SinkThresholds;
        child.BoundFields = Logger.CopyFields(BoundFields);
        child.ScopeStorage = ScopeStorage;
        child.ActivityEnricher = ActivityEnricher;
        child.BuiltInEnricher = BuiltInEnricher;
        child.Enrichers = Logger.CopyEnrichers(Enrichers);
        child.IncludeRuntimeMetadata = IncludeRuntimeMetadata;
        child.FilterPipeline = FilterPipeline;
        child.Pipeline = Pipeline;
        return child;
    }

    /// <summary>
    /// Creates a child category from normalized CLR or C# type-name text.
    /// </summary>
    /// <param name="typeName">The type name, including optional generic, nested-type, array, or assembly-qualified syntax.</param>
    /// <returns>The selected child logger with inherited context and pipeline behavior.</returns>
    public Logger ForTypeName(string? typeName)
    {
        return For(LogName.NormalizeTypeName(typeName));
    }

    /// <summary>
    /// Creates a child logger whose name is combined with the current logger name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="fields">The structured fields to copy or process.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    public Logger ForWithFields(string name, LogField[] fields)
    {
        Logger child = For(name);
        child.BoundFields = LogEventBuilder.AppendFields(child.BoundFields, fields);
        return child;
    }

    /// <summary>
    /// Creates a child logger with the supplied bound field data.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    public Logger ForField(string name, object value)
    {
        return WithField(name, value);
    }

    /// <summary>
    /// Creates a child logger with the supplied bound field data.
    /// </summary>
    /// <param name="field">The structured field to add.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    public Logger ForLogField(LogField field)
    {
        return WithLogField(field);
    }

    /// <summary>
    /// Creates a child logger with the supplied bound field data.
    /// </summary>
    /// <param name="fields">The structured fields to copy or process.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    public Logger ForFields(LogField[] fields)
    {
        return WithFields(fields);
    }

    /// <summary>
    /// Creates a same-category child logger with one bound application value.
    /// </summary>
    /// <param name="name">The structured field name.</param>
    /// <param name="value">The application value to normalize.</param>
    /// <returns>A contextual child logger.</returns>
    public Logger WithField(string name, object? value)
    {
        return WithLogField(LogField.ValueField(name, LogTemplateBinder.ToLogValue(value)));
    }

    /// <summary>
    /// Creates a same-category child logger with one bound structured field.
    /// </summary>
    /// <param name="field">The field, or null to preserve one invalid field entry.</param>
    /// <returns>A contextual child logger.</returns>
    public Logger WithLogField(LogField? field)
    {
        Logger child = For("");
        child.BoundFields = LogEventBuilder.AppendField(child.BoundFields, field);
        return child;
    }

    /// <summary>
    /// Creates a same-category child logger with bound structured fields.
    /// </summary>
    /// <param name="fields">The fields to copy, or null to add none.</param>
    /// <returns>A contextual child logger.</returns>
    public Logger WithFields(LogField[]? fields)
    {
        Logger child = For("");
        if (fields != null)
        {
            child.BoundFields = LogEventBuilder.AppendFields(child.BoundFields, fields);
        }

        return child;
    }

    /// <summary>
    /// Creates a same-category child logger with one named field group.
    /// </summary>
    /// <param name="name">The group field name.</param>
    /// <param name="fields">The structured child fields.</param>
    /// <returns>A contextual child logger.</returns>
    public Logger WithFieldGroup(string? name, LogField[]? fields)
    {
        return WithLogField(LogFieldGroups.CreateGroup(name, fields));
    }

    /// <summary>
    /// Creates a same-category child logger with one dotted nested field namespace.
    /// </summary>
    /// <param name="namespaceName">The dotted namespace to expand into nested groups.</param>
    /// <param name="fields">The structured child fields.</param>
    /// <returns>A contextual child logger.</returns>
    public Logger WithFieldNamespace(string? namespaceName, LogField[]? fields)
    {
        return WithLogField(LogFieldGroups.CreateNamespace(namespaceName, fields));
    }

    /// <summary>
    /// Pushes contextual fields onto the current asynchronous flow and returns their disposable scope.
    /// </summary>
    /// <param name="fields">The structured fields to copy or process.</param>
    /// <returns>A scope that removes the contextual fields when disposed.</returns>
    public LogScope BeginScope(LogField[] fields)
    {
        return ScopeStorage.Begin(fields);
    }

    /// <summary>
    /// Pushes contextual fields onto the current asynchronous flow and returns their disposable scope.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>A scope that removes the contextual fields when disposed.</returns>
    public LogScope BeginScopeField(string name, object value)
    {
        return ScopeStorage.BeginField(name, value);
    }

    /// <summary>
    /// Pushes contextual fields onto the current asynchronous flow and returns their disposable scope.
    /// </summary>
    /// <param name="field">The structured field to add.</param>
    /// <returns>A scope that removes the contextual fields when disposed.</returns>
    public LogScope BeginScopeLogField(LogField field)
    {
        return ScopeStorage.BeginLogField(field);
    }

    /// <summary>
    /// Pushes one named field group onto the current asynchronous flow.
    /// </summary>
    /// <param name="name">The group field name.</param>
    /// <param name="fields">The structured child fields.</param>
    /// <returns>A scope that removes the group when disposed.</returns>
    public LogScope BeginScopeFieldGroup(string? name, LogField[]? fields)
    {
        return ScopeStorage.BeginLogField(LogFieldGroups.CreateGroup(name, fields));
    }

    /// <summary>
    /// Pushes one dotted nested field namespace onto the current asynchronous flow.
    /// </summary>
    /// <param name="namespaceName">The dotted namespace to expand into nested groups.</param>
    /// <param name="fields">The structured child fields.</param>
    /// <returns>A scope that removes the namespace when disposed.</returns>
    public LogScope BeginScopeFieldNamespace(string? namespaceName, LogField[]? fields)
    {
        return ScopeStorage.BeginLogField(LogFieldGroups.CreateNamespace(namespaceName, fields));
    }

    /// <summary>
    /// Adds a dynamic field provider to this logger.
    /// </summary>
    /// <param name="enricher">The enricher to invoke while constructing each enabled event.</param>
    public void AddEnricher(ILogEnricher enricher)
    {
        if (enricher == null)
        {
            return;
        }

        ILogEnricher[] copy = new ILogEnricher[Enrichers.Length + 1];
        int index = 0;
        while (index < Enrichers.Length)
        {
            copy[index] = Enrichers[index];
            index = index + 1;
        }

        copy[Enrichers.Length] = enricher;
        Enrichers = copy;
    }

    /// <summary>
    /// Replaces dynamic field providers for this logger.
    /// </summary>
    /// <param name="enrichers">The enrichers to copy into this logger.</param>
    public void SetEnrichers(ILogEnricher[] enrichers)
    {
        Enrichers = Logger.CopyEnrichers(enrichers);
    }

    /// <summary>
    /// Removes all interface-based dynamic field providers from this logger.
    /// </summary>
    public void ClearEnrichers()
    {
        Enrichers = new ILogEnricher[0];
    }

    /// <summary>
    /// Adds filter to this logger.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    public void AddFilter(LogFilter filter)
    {
        FilterPipeline.AddFilter(filter);
    }

    /// <summary>
    /// Adds an interface-based filter to this logger.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    public void AddInterfaceFilter(ILogFilter filter)
    {
        FilterPipeline.AddInterfaceFilter(filter);
    }

    /// <summary>
    /// Sets filters for this logger.
    /// </summary>
    /// <param name="filters">The filters to copy into the destination.</param>
    public void SetFilters(LogFilter[] filters)
    {
        FilterPipeline.SetFilters(filters);
    }

    /// <summary>
    /// Replaces logger filters with interface-based rules.
    /// </summary>
    /// <param name="filters">The filters to copy into this logger.</param>
    public void SetInterfaceFilters(ILogFilter[] filters)
    {
        FilterPipeline.SetInterfaceFilters(filters);
    }

    /// <summary>
    /// Clears filters and restores the default behavior.
    /// </summary>
    public void ClearFilters()
    {
        FilterPipeline.ClearFilters();
    }

    /// <summary>
    /// Adds sink to this logger.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    public void AddSink(LogSink sink)
    {
        Pipeline.AddSink(sink);
    }

    /// <summary>
    /// Adds an interface-based sink to this logger.
    /// </summary>
    /// <param name="sink">The destination to add.</param>
    public void AddInterfaceSink(ILogSink sink)
    {
        Pipeline.AddInterfaceSink(sink);
    }

    /// <summary>
    /// Adds pipeline to this logger.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="pipeline">The child pipeline that receives routed events.</param>
    internal void AddPipeline(string name, LogPipeline pipeline)
    {
        Pipeline.AddPipeline(name, pipeline);
    }

    /// <summary>
    /// Sets sinks for this logger.
    /// </summary>
    /// <param name="sinks">The sinks to copy into the destination.</param>
    public void SetSinks(LogSink[] sinks)
    {
        Pipeline.SetSinks(sinks);
    }

    /// <summary>
    /// Replaces the logger sinks with interface-based destinations.
    /// </summary>
    /// <param name="sinks">The destinations to copy into this logger.</param>
    public void SetInterfaceSinks(ILogSink[] sinks)
    {
        Pipeline.SetInterfaceSinks(sinks);
    }

    /// <summary>
    /// Clears sinks and restores the default behavior.
    /// </summary>
    public void ClearSinks()
    {
        Pipeline.ClearSinks();
    }

    /// <summary>
    /// Installs a child pipeline and returns its configurable routing sink.
    /// </summary>
    /// <param name="pipeline">The child pipeline to install.</param>
    /// <returns>The routing sink, or a disabled sink carrying a failure result when the child is null.</returns>
    public LogSink InstallSubPipeline(LogSinkPipeline pipeline)
    {
        return LogSinkPipeline.InstallInto(Pipeline, pipeline);
    }

    /// <summary>
    /// Replaces child pipelines while preserving direct sinks.
    /// </summary>
    /// <param name="pipelines">The child pipelines to install.</param>
    public void SetSubPipelines(LogSinkPipeline[] pipelines)
    {
        LogSinkPipeline.ReplaceIn(Pipeline, pipelines);
    }

    /// <summary>
    /// Returns a detached snapshot of installed child pipelines.
    /// </summary>
    /// <returns>The installed child pipelines in routing order.</returns>
    public LogSinkPipeline[] GetSubPipelines()
    {
        return LogSinkPipeline.InspectPipelines(Pipeline);
    }

    /// <summary>
    /// Returns the configurable routing sinks for installed child pipelines.
    /// </summary>
    /// <returns>The child routing sinks in routing order.</returns>
    public LogSink[] GetSubPipelineSinks()
    {
        return LogSinkPipeline.InspectRoutingSinks(Pipeline);
    }

    /// <summary>
    /// Removes child pipelines while preserving direct sinks.
    /// </summary>
    public void ClearSubPipelines()
    {
        LogSinkPipeline.RemoveFrom(Pipeline);
    }

    /// <summary>
    /// Flushes every sink owned by this logger.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Flush()
    {
        if (Pipeline == null)
        {
            return LogResult.Success();
        }

        return Pipeline.Flush();
    }

    /// <summary>
    /// Forwards pending asynchronous and batch events.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Drain()
    {
        if (Pipeline == null)
        {
            return LogResult.Success();
        }

        return Pipeline.Drain();
    }

    /// <summary>
    /// Drains and closes every sink owned by this logger.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult Close()
    {
        if (Pipeline == null)
        {
            return LogResult.Success();
        }

        return Pipeline.Close();
    }

    /// <summary>
    /// Returns a point-in-time health snapshot for this logger.
    /// </summary>
    /// <returns>A point-in-time pipeline health snapshot.</returns>
    public LogPipelineHealth Health()
    {
        return LogPipelineHealth.FromLogger(this);
    }

    /// <summary>
    /// Sets shutdown timeout for this logger.
    /// </summary>
    /// <param name="timeout">The maximum time allowed for draining or closing.</param>
    public void SetShutdownTimeout(TimeSpan timeout)
    {
        if (Pipeline == null)
        {
            return;
        }

        Pipeline.SetShutdownTimeout(timeout);
    }

    /// <summary>
    /// Sets sink minimum level for this logger.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    public void SetSinkMinimumLevel(LogLevel level)
    {
        SinkThresholds.SetMinimumLevel(level);
    }

    /// <summary>
    /// Clears sink minimum level and restores the default behavior.
    /// </summary>
    public void ClearSinkMinimumLevel()
    {
        SinkThresholds.ClearMinimumLevel();
    }

    /// <summary>
    /// Sets category level for this logger.
    /// </summary>
    /// <param name="loggerName">The dotted logger or category name.</param>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult SetCategoryLevel(string loggerName, LogLevel level)
    {
        string normalizedLoggerName = LogName.NormalizeLoggerName(loggerName);
        string validationError = LogName.ValidateLoggerName(normalizedLoggerName);
        if (validationError.Length > 0)
        {
            return LogResult.FailureCode("logger.category.invalid", validationError);
        }

        if (CategoryLevelSwitches.ContainsKey(normalizedLoggerName))
        {
            CategoryLevelSwitches[normalizedLoggerName].SetLevel(level);
        }
        else
        {
            CategoryLevelSwitches[normalizedLoggerName] = LogLevelSwitch.Create(level);
        }

        return LogResult.Success();
    }

    internal LogLevelSwitch FindCategoryLevelSwitch(string loggerName)
    {
        string currentName = LogName.NormalizeLoggerName(loggerName);

        while (currentName.Length > 0)
        {
            if (CategoryLevelSwitches.ContainsKey(currentName))
            {
                return CategoryLevelSwitches[currentName];
            }

            int separatorIndex = currentName.LastIndexOf('.');
            if (separatorIndex < 0)
            {
                break;
            }

            currentName = currentName.Substring(0, separatorIndex);
        }

        return LevelSwitch;
    }

    internal static string CombineNames(string parentName, string childName)
    {
        string parent = LogName.NormalizeLoggerName(parentName);
        string child = LogName.NormalizeLoggerName(childName);

        if (child.Length == 0)
        {
            return parent;
        }

        if (parent.Length == 0)
        {
            return child;
        }

        return parent + "." + child;
    }

    internal LogEventBuilder CreateEventBuilder(LogLevel level)
    {
        LogEventBuilder builder = LogEventBuilder.Create(Clock);
        builder.SuppressRuntime(!IncludeRuntimeMetadata);
        builder.SetLoggerName(Name);
        builder.SetLevel(level);
        builder.AddScopeFields(BoundFields);
        builder.AddScopeFields(ScopeStorage.GetCurrentFields());
        if (BuiltInEnricher != null)
        {
            BuiltInEnricher.Enrich(builder);
        }

        if (ActivityEnricher != null)
        {
            ActivityEnricher.Enrich(builder);
        }

        foreach (ILogEnricher enricher in Enrichers)
        {
            if (enricher != null)
            {
                builder.AddEnricherFields(enricher.Enrich(level, Name));
            }
        }

        return builder;
    }

    internal static bool IsEnabledForSwitchAndSink(LogLevel level, LogLevelSwitch levelSwitch, LogSinkThresholds sinkThresholds)
    {
        if (levelSwitch == null)
        {
            return false;
        }

        if (!levelSwitch.IsEnabled(level))
        {
            return false;
        }

        if (sinkThresholds == null)
        {
            return true;
        }

        return sinkThresholds.IsEnabled(level);
    }

    internal LogResult DisabledWriteResult()
    {
        if (!IsValid)
        {
            return LogResult.FailureCode("logger.invalid", ErrorMessage);
        }

        return LogResult.Dropped("log event disabled");
    }

    internal static object?[] NormalizeValues(object?[]? values)
    {
        if (values == null)
        {
            return new object?[0];
        }

        return values;
    }

    internal static LogField[] CopyFields(LogField[] fields)
    {
        if (fields == null)
        {
            return new LogField[0];
        }

        LogField[] copy = new LogField[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            copy[index] = fields[index];
            index = index + 1;
        }

        return copy;
    }

    internal static ILogEnricher[] CopyEnrichers(ILogEnricher[] enrichers)
    {
        if (enrichers == null)
        {
            return new ILogEnricher[0];
        }

        ILogEnricher[] copy = new ILogEnricher[enrichers.Length];
        int index = 0;
        while (index < enrichers.Length)
        {
            copy[index] = enrichers[index];
            index = index + 1;
        }

        return copy;
    }
}
