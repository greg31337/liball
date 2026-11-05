using System;

namespace log;

/// <summary>
/// Defines construction of built-in formatters, filters, sinks, policies, and configuration sources.
/// </summary>
public interface ILogFactory
{
    /// <summary>Normalizes an application value into a structured value.</summary>
    LogValue CreateValue(object? value);
    /// <summary>Creates a structured field from an application value.</summary>
    LogField CreateField(string name, object? value);
    /// <summary>Creates a log event with runtime metadata defaults.</summary>
    LogEvent CreateEvent(LogLevel level, string loggerName, string messageTemplate, string renderedMessage, LogField[] fields);
    /// <summary>Creates the default text formatter.</summary>
    LogFormatter CreateTextFormatter();
    /// <summary>Creates the compact JSON formatter.</summary>
    LogFormatter CreateCompactJsonFormatter();
    /// <summary>Creates the full JSON formatter.</summary>
    LogFormatter CreateFullJsonFormatter();
    /// <summary>Creates the plain console formatter.</summary>
    LogFormatter CreatePlainConsoleFormatter();
    /// <summary>Creates the ANSI-color console formatter.</summary>
    LogFormatter CreateColorConsoleFormatter();
    /// <summary>Creates the key-value formatter.</summary>
    LogFormatter CreateKeyValueFormatter();
    /// <summary>Creates the default output-template formatter.</summary>
    LogFormatter CreateTemplateFormatter();
    /// <summary>Creates an output-template formatter.</summary>
    LogFormatter CreateOutputTemplateFormatter(string outputTemplate);
    /// <summary>Creates the full exception formatter.</summary>
    LogFormatter CreateFullExceptionFormatter();
    /// <summary>Creates the summary exception formatter.</summary>
    LogFormatter CreateSummaryExceptionFormatter();
    /// <summary>Creates a named formatter.</summary>
    LogFormatter CreateNamedFormatter(string name);
    /// <summary>Creates a callback-based formatter.</summary>
    LogFormatter CreateCustomFormatter(string name, Func<LogEvent, string> formatAction);
    /// <summary>Wraps an interface-based formatter.</summary>
    LogFormatter WrapFormatter(ILogFormatter? formatter);
    /// <summary>Wraps an interface-based filter.</summary>
    LogFilter WrapFilter(ILogFilter filter);
    /// <summary>Creates a minimum-level filter.</summary>
    LogFilter CreateMinimumLevelFilter(LogLevel level);
    /// <summary>Creates a deterministic sampling filter.</summary>
    LogFilter CreateSamplingFilter(int count);
    /// <summary>Creates a one-second rate-limit filter.</summary>
    LogFilter CreateRateLimitFilter(int count);
    /// <summary>Creates a windowed rate-limit filter.</summary>
    LogFilter CreateWindowedRateLimitFilter(int count, TimeSpan window);
    /// <summary>Creates a rate-limit filter with an injected clock.</summary>
    LogFilter CreateClockedRateLimitFilter(int count, TimeSpan window, ILogClock clock);
    /// <summary>Creates a category-prefix filter.</summary>
    LogFilter CreateCategoryFilter(string prefix, LogLevel level);
    /// <summary>Creates a field-presence filter.</summary>
    LogFilter CreateFieldExistsFilter(string name);
    /// <summary>Creates a normalized field-value filter.</summary>
    LogFilter CreateFieldEqualsFilter(string name, object value);
    /// <summary>Creates a structured field-value filter.</summary>
    LogFilter CreateStructuredFieldEqualsFilter(string name, LogValue value);
    /// <summary>Wraps an interface-based sink.</summary>
    LogSink WrapSink(ILogSink sink);
    /// <summary>Creates a named sink pipeline.</summary>
    LogSinkPipeline CreateSinkPipeline(string name);
    /// <summary>Creates a null sink.</summary>
    LogSink CreateNullSink(string name);
    /// <summary>Creates a bounded or unbounded memory sink.</summary>
    LogSink CreateMemorySink(string name, int maximumEvents);
    /// <summary>Creates a text-writer sink.</summary>
    LogSink CreateTextWriterSink(System.IO.TextWriter? writer, ILogFormatter? formatter, bool flushOnWrite);
    /// <summary>Creates a console sink.</summary>
    LogSink CreateConsoleSink(System.IO.TextWriter? outputWriter, System.IO.TextWriter? errorWriter, ILogFormatter? formatter, bool useAnsiColor);
    /// <summary>Creates a debug sink.</summary>
    LogSink CreateDebugSink(Action<string>? writeAction, ILogFormatter? formatter, bool flushOnWrite);
    /// <summary>Creates a trace sink.</summary>
    LogSink CreateTraceSink(Action<string>? writeAction, ILogFormatter? formatter, bool flushOnWrite);
    /// <summary>Creates a file sink with optional rolling and retention.</summary>
    LogSink CreateFileSink(string filePath, ILogFormatter? formatter, bool append, LogRollingPolicy? rollingPolicy, LogRetentionPolicy? retentionPolicy, bool flushOnWrite);
    /// <summary>Creates an EventSource sink.</summary>
    LogSink CreateEventSourceSink(string sourceName, ILogFormatter? formatter, Action<LogEventSourceEntry>? writeAction);
    /// <summary>Creates a callback-based sink.</summary>
    LogSink CreateDelegateSink(string name, Func<LogEvent, LogResult?>? writeAction);
    /// <summary>Creates an asynchronous queue wrapper.</summary>
    LogSink CreateAsyncSink(string name, LogSink? innerSink, int capacity, LogBackpressureMode mode);
    /// <summary>Creates a batch wrapper.</summary>
    LogSink CreateBatchSink(string name, LogSink? innerSink, int batchSize);
    /// <summary>Creates a circuit-breaker wrapper.</summary>
    LogSink CreateCircuitBreakerSink(string name, LogSink? innerSink, int failureThreshold);
    /// <summary>Returns memory-sink events.</summary>
    LogEvent[] GetMemorySinkEvents(LogSink sink);
    /// <summary>Clears memory-sink events.</summary>
    void ClearMemorySink(LogSink sink);
    /// <summary>Returns queued asynchronous events.</summary>
    LogEvent[] GetAsyncSinkEvents(LogSink sink);
    /// <summary>Returns queued batch events.</summary>
    LogEvent[] GetBatchSinkEvents(LogSink sink);
    /// <summary>Sets a sink minimum level.</summary>
    void SetSinkMinimumLevel(LogSink sink, LogLevel level);
    /// <summary>Clears a sink minimum level.</summary>
    void ClearSinkMinimumLevel(LogSink sink);
    /// <summary>Adds an interface-based sink filter.</summary>
    void AddSinkFilter(LogSink sink, ILogFilter filter);
    /// <summary>Creates a rolling policy.</summary>
    LogRollingPolicy CreateRollingPolicy(LogRollingInterval interval, long maximumFileSizeBytes);
    /// <summary>Creates a retention policy.</summary>
    LogRetentionPolicy CreateRetentionPolicy(int maximumFileCount, TimeSpan maximumFileAge);
    /// <summary>Loads configuration from JSON.</summary>
    LogConfiguration LoadJson(string json);
    /// <summary>Loads configuration from a JSON file.</summary>
    LogConfiguration LoadJsonFile(string filePath);
    /// <summary>Loads JSON and applies process environment values.</summary>
    LogConfiguration LoadJsonWithEnvironment(string json);
    /// <summary>Loads a JSON file and applies process environment values.</summary>
    LogConfiguration LoadJsonFileWithEnvironment(string filePath);
    /// <summary>Creates a JSON file watcher.</summary>
    LogConfigurationReload WatchJsonFile(string filePath);
    /// <summary>Creates a JSON file watcher that applies environment values.</summary>
    LogConfigurationReload WatchJsonFileWithEnvironment(string filePath);
}
