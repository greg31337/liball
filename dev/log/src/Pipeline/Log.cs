using System;

namespace log;

/// <summary>
/// Provides a process-wide logger facade for applications that prefer global configuration.
/// </summary>
internal class Log
{
    internal static readonly GlobalLogState State = new GlobalLogState();

    /// <summary>
    /// Returns the current process-wide logger.
    /// </summary>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger Current()
    {
        lock (State.SyncRoot)
        {
            return State.CurrentLogger;
        }
    }

    /// <summary>
    /// Atomically replaces the process-wide logger and returns the replacement.
    /// </summary>
    /// <param name="logger">The logger value.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger SetLogger(Logger logger)
    {
        lock (State.SyncRoot)
        {
            if (logger == null)
            {
                State.CurrentLogger = Logger.Root();
            }
            else
            {
                State.CurrentLogger = logger;
            }

            return State.CurrentLogger;
        }
    }

    /// <summary>
    /// Replaces the process-wide logger with a new root logger.
    /// </summary>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger Reset()
    {
        return SetLogger(Logger.Root());
    }

    /// <summary>
    /// Replaces the process-wide logger with a logger that drops all events.
    /// </summary>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger NoOp()
    {
        return SetLogger(Logger.NoOp());
    }

    /// <summary>
    /// Builds and installs a logger from a programmatic configuration.
    /// </summary>
    /// <param name="configuration">The configuration to validate or install.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger Configure(LogConfiguration configuration)
    {
        if (configuration == null)
        {
            return Reset();
        }

        return SetLogger(configuration.CreateLogger());
    }

    /// <summary>
    /// Loads JSON text, builds its logger, and installs it globally.
    /// </summary>
    /// <param name="json">The JSON configuration text.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger ConfigureJson(string json)
    {
        return Configure(LogJsonConfigurationLoader.Load(json));
    }

    /// <summary>
    /// Loads a JSON file, builds its logger, and installs it globally.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger ConfigureJsonFile(string filePath)
    {
        return Configure(LogJsonConfigurationLoader.LoadFile(filePath));
    }

    /// <summary>
    /// Loads JSON text, applies environment overrides, and installs the resulting logger.
    /// </summary>
    /// <param name="json">The JSON configuration text.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger ConfigureJsonWithEnvironment(string json)
    {
        return Configure(LogJsonConfigurationLoader.LoadWithEnvironment(json));
    }

    /// <summary>
    /// Loads a JSON file, applies environment overrides, and installs the resulting logger.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger ConfigureJsonFileWithEnvironment(string filePath)
    {
        return Configure(LogJsonConfigurationLoader.LoadFileWithEnvironment(filePath));
    }

    /// <summary>
    /// Reports whether the global logger would accept the specified level.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsEnabled(LogLevel level)
    {
        return Current().IsEnabled(level);
    }

    /// <summary>
    /// Writes a specified-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Write(LogLevel level, string messageTemplate)
    {
        return Current().Write(level, messageTemplate);
    }

    /// <summary>
    /// Writes a specified-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Write(LogLevel level, string messageTemplate, object?[]? values)
    {
        return Current().WriteWithValues(level, messageTemplate, values);
    }

    /// <summary>
    /// Writes a specified-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Write(LogLevel level, Exception exceptionValue, string messageTemplate)
    {
        return Current().WriteWithException(level, exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a specified-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Write(LogLevel level, Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return Current().WriteWithExceptionAndValues(level, exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a trace-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Trace(string messageTemplate)
    {
        return Current().Trace(messageTemplate);
    }

    /// <summary>
    /// Writes a trace-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Trace(string messageTemplate, object?[]? values)
    {
        return Current().TraceWithValues(messageTemplate, values);
    }

    /// <summary>
    /// Writes a trace-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Trace(Exception exceptionValue, string messageTemplate)
    {
        return Current().TraceWithException(exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a trace-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Trace(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return Current().TraceWithExceptionAndValues(exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a debug-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Debug(string messageTemplate)
    {
        return Current().Debug(messageTemplate);
    }

    /// <summary>
    /// Writes a debug-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Debug(string messageTemplate, object?[]? values)
    {
        return Current().DebugWithValues(messageTemplate, values);
    }

    /// <summary>
    /// Writes a debug-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Debug(Exception exceptionValue, string messageTemplate)
    {
        return Current().DebugWithException(exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a debug-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Debug(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return Current().DebugWithExceptionAndValues(exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a info-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Info(string messageTemplate)
    {
        return Current().Info(messageTemplate);
    }

    /// <summary>
    /// Writes a info-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Info(string messageTemplate, object?[]? values)
    {
        return Current().InfoWithValues(messageTemplate, values);
    }

    /// <summary>
    /// Writes a info-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Info(Exception exceptionValue, string messageTemplate)
    {
        return Current().InfoWithException(exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a info-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Info(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return Current().InfoWithExceptionAndValues(exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a warn-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Warn(string messageTemplate)
    {
        return Current().Warn(messageTemplate);
    }

    /// <summary>
    /// Writes a warn-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Warn(string messageTemplate, object?[]? values)
    {
        return Current().WarnWithValues(messageTemplate, values);
    }

    /// <summary>
    /// Writes a warn-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Warn(Exception exceptionValue, string messageTemplate)
    {
        return Current().WarnWithException(exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a warn-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Warn(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return Current().WarnWithExceptionAndValues(exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a error-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Error(string messageTemplate)
    {
        return Current().Error(messageTemplate);
    }

    /// <summary>
    /// Writes a error-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Error(string messageTemplate, object?[]? values)
    {
        return Current().ErrorWithValues(messageTemplate, values);
    }

    /// <summary>
    /// Writes a error-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Error(Exception exceptionValue, string messageTemplate)
    {
        return Current().ErrorWithException(exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a error-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Error(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return Current().ErrorWithExceptionAndValues(exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Writes a fatal-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Fatal(string messageTemplate)
    {
        return Current().Fatal(messageTemplate);
    }

    /// <summary>
    /// Writes a fatal-level event; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Fatal(string messageTemplate, object?[]? values)
    {
        return Current().FatalWithValues(messageTemplate, values);
    }

    /// <summary>
    /// Writes a fatal-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Fatal(Exception exceptionValue, string messageTemplate)
    {
        return Current().FatalWithException(exceptionValue, messageTemplate);
    }

    /// <summary>
    /// Writes a fatal-level event and attaches the supplied exception; supplied values bind to message-template placeholders in order.
    /// </summary>
    /// <param name="exceptionValue">The exception to attach or normalize.</param>
    /// <param name="messageTemplate">The message template; braced placeholders are bound positionally to supplied values.</param>
    /// <param name="values">Values bound positionally to message-template placeholders.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Fatal(Exception? exceptionValue, string messageTemplate, object?[]? values)
    {
        return Current().FatalWithExceptionAndValues(exceptionValue, messageTemplate, values);
    }

    /// <summary>
    /// Creates a child logger whose name is combined with the current logger name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger For(string name)
    {
        return Current().For(name);
    }

    /// <summary>
    /// Creates a child logger whose name is combined with the current logger name.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger ForTypeName(string name)
    {
        return Current().For(name);
    }

    /// <summary>
    /// Creates a log configured with the supplied field.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger WithField(string name, object value)
    {
        return Current().ForField(name, value);
    }

    /// <summary>
    /// Creates a log configured with the supplied field.
    /// </summary>
    /// <param name="field">The structured field to add.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger WithField(LogField field)
    {
        return Current().ForLogField(field);
    }

    /// <summary>
    /// Creates a log configured with the supplied fields.
    /// </summary>
    /// <param name="fields">The structured fields to copy or process.</param>
    /// <returns>The selected, created, or installed logger.</returns>
    internal static Logger WithFields(LogField[] fields)
    {
        return Current().ForFields(fields);
    }

    /// <summary>
    /// Pushes contextual fields onto the current asynchronous flow and returns their disposable scope.
    /// </summary>
    /// <param name="fields">The structured fields to copy or process.</param>
    /// <returns>A scope that removes the contextual fields when disposed.</returns>
    internal static LogScope BeginScope(LogField[] fields)
    {
        return Current().BeginScope(fields);
    }

    /// <summary>
    /// Pushes contextual fields onto the current asynchronous flow and returns their disposable scope.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>A scope that removes the contextual fields when disposed.</returns>
    internal static LogScope BeginScope(string name, object value)
    {
        return Current().BeginScopeField(name, value);
    }

    /// <summary>
    /// Pushes contextual fields onto the current asynchronous flow and returns their disposable scope.
    /// </summary>
    /// <param name="field">The structured field to add.</param>
    /// <returns>A scope that removes the contextual fields when disposed.</returns>
    internal static LogScope BeginScope(LogField field)
    {
        return Current().BeginScopeLogField(field);
    }

    /// <summary>
    /// Adds sink to this log.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    internal static void AddSink(LogSink sink)
    {
        Current().AddSink(sink);
    }

    /// <summary>
    /// Adds an interface-based sink to the global logger.
    /// </summary>
    /// <param name="sink">The destination to add.</param>
    internal static void AddSink(ILogSink sink)
    {
        Current().AddInterfaceSink(sink);
    }

    /// <summary>
    /// Sets sinks for this log.
    /// </summary>
    /// <param name="sinks">The sinks to copy into the destination.</param>
    internal static void SetSinks(LogSink[] sinks)
    {
        Current().SetSinks(sinks);
    }

    /// <summary>
    /// Replaces global logger sinks with interface-based destinations.
    /// </summary>
    /// <param name="sinks">The destinations to copy into the global logger.</param>
    internal static void SetSinks(ILogSink[] sinks)
    {
        Current().SetInterfaceSinks(sinks);
    }

    /// <summary>
    /// Clears sinks and restores the default behavior.
    /// </summary>
    internal static void ClearSinks()
    {
        Current().ClearSinks();
    }

    /// <summary>
    /// Adds filter to this log.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    internal static void AddFilter(LogFilter filter)
    {
        Current().AddFilter(filter);
    }

    /// <summary>
    /// Adds an interface-based filter to the global logger.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    internal static void AddFilter(ILogFilter filter)
    {
        Current().AddInterfaceFilter(filter);
    }

    /// <summary>
    /// Sets filters for this log.
    /// </summary>
    /// <param name="filters">The filters to copy into the destination.</param>
    internal static void SetFilters(LogFilter[] filters)
    {
        Current().SetFilters(filters);
    }

    /// <summary>
    /// Replaces global logger filters with interface-based rules.
    /// </summary>
    /// <param name="filters">The filters to copy into the global logger.</param>
    internal static void SetFilters(ILogFilter[] filters)
    {
        Current().SetInterfaceFilters(filters);
    }

    /// <summary>
    /// Clears filters and restores the default behavior.
    /// </summary>
    internal static void ClearFilters()
    {
        Current().ClearFilters();
    }

    /// <summary>
    /// Sets category level for this log.
    /// </summary>
    /// <param name="loggerName">The dotted logger or category name.</param>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult SetCategoryLevel(string loggerName, LogLevel level)
    {
        return Current().SetCategoryLevel(loggerName, level);
    }

    /// <summary>
    /// Sets sink minimum level for this log.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    internal static void SetSinkMinimumLevel(LogLevel level)
    {
        Current().SetSinkMinimumLevel(level);
    }

    /// <summary>
    /// Clears sink minimum level and restores the default behavior.
    /// </summary>
    internal static void ClearSinkMinimumLevel()
    {
        Current().ClearSinkMinimumLevel();
    }

    /// <summary>
    /// Flushes the current global logger and all of its sinks.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Flush()
    {
        return Current().Flush();
    }

    /// <summary>
    /// Forwards all events buffered by the current global logger's sinks.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Drain()
    {
        return Current().Drain();
    }

    /// <summary>
    /// Drains and closes the current global logger and its sinks.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Close()
    {
        return Current().Close();
    }

    /// <summary>
    /// Returns a point-in-time health snapshot for the global logger.
    /// </summary>
    /// <returns>A point-in-time pipeline health snapshot.</returns>
    internal static LogPipelineHealth Health()
    {
        return Current().Health();
    }

    /// <summary>
    /// Sets shutdown timeout for this log.
    /// </summary>
    /// <param name="timeout">The maximum time allowed for draining or closing.</param>
    internal static void SetShutdownTimeout(TimeSpan timeout)
    {
        Current().SetShutdownTimeout(timeout);
    }
}

internal class GlobalLogState
{
    internal Logger CurrentLogger = Logger.Root();
    internal object SyncRoot = new object();
}
