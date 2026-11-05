using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Constructs built-in logging components without exposing static methods on public API types.
/// </summary>
public class LogFactory : ILogFactory, ILogValueFactory, ILoggerFactory, ILogFieldGroupFactory
{
    /// <summary>Creates a logging component factory.</summary>
    public LogFactory()
    {
    }

    /// <summary>Creates an unnamed root logger with the default informational minimum level.</summary>
    public Logger CreateRootLogger()
    {
        return Logger.Root();
    }

    /// <summary>Creates an unnamed root logger with a fixed minimum level.</summary>
    public Logger CreateRootLoggerWithLevel(LogLevel level)
    {
        return Logger.RootWithLevel(level);
    }

    /// <summary>Creates a named logger with the default informational minimum level.</summary>
    public Logger CreateNamedLogger(string? name)
    {
        return new Logger(name, LogLevelSwitch.Create(LogLevel.Info), false, LogClock.SystemClock());
    }

    /// <summary>Creates a named logger with a fixed minimum level.</summary>
    public Logger CreateNamedLoggerWithLevel(string? name, LogLevel level)
    {
        return new Logger(name, LogLevelSwitch.Create(level), false, LogClock.SystemClock());
    }

    /// <summary>Creates an unnamed root logger sharing a runtime level switch.</summary>
    public Logger CreateRootLoggerWithSwitch(LogLevelSwitch? levelSwitch)
    {
        if (levelSwitch == null)
        {
            return Logger.Root();
        }

        return Logger.RootWithSwitch(levelSwitch);
    }

    /// <summary>Creates a named logger sharing a runtime level switch.</summary>
    public Logger CreateNamedLoggerWithSwitch(string? name, LogLevelSwitch? levelSwitch)
    {
        return new Logger(name, levelSwitch, false, LogClock.SystemClock());
    }

    /// <summary>Creates an explicit no-op logger that drops every level.</summary>
    public Logger CreateNoOpLogger()
    {
        return Logger.NoOp();
    }

    /// <summary>Creates one named dictionary field from structured child fields.</summary>
    public LogField CreateFieldGroup(string? name, LogField[]? fields)
    {
        return LogFieldGroups.CreateGroup(name, fields);
    }

    /// <summary>Creates nested dictionary fields from a dotted namespace and structured child fields.</summary>
    public LogField CreateFieldNamespace(string? namespaceName, LogField[]? fields)
    {
        return LogFieldGroups.CreateNamespace(namespaceName, fields);
    }

    /// <summary>Normalizes an application value into a structured value.</summary>
    public LogValue CreateValue(object? value)
    {
        return LogTemplateBinder.ToLogValue(value);
    }

    /// <summary>Normalizes an application value with string, collection, and nesting limits.</summary>
    public LogValue CreateValueWithLimits(object? value, int maximumStringLength, int maximumCollectionLength, int maximumDepth)
    {
        return LogTemplateBinder.ToLogValueLimit(value, maximumStringLength, maximumCollectionLength, maximumDepth);
    }

    /// <summary>Creates a null structured value.</summary>
    public LogValue CreateNullValue()
    {
        return LogValue.Null();
    }

    /// <summary>Creates a string structured value.</summary>
    public LogValue CreateStringValue(string? value)
    {
        return LogValue.String(value);
    }

    /// <summary>Creates a length-limited string structured value.</summary>
    public LogValue CreateStringValueWithLimit(string? value, int maximumStringLength)
    {
        return LogValue.StringLimit(value, maximumStringLength);
    }

    /// <summary>Creates a Boolean structured value.</summary>
    public LogValue CreateBoolValue(bool value)
    {
        return LogValue.Bool(value);
    }

    /// <summary>Creates a 32-bit integer structured value.</summary>
    public LogValue CreateIntValue(int value)
    {
        return LogValue.Integer(value);
    }

    /// <summary>Creates a 64-bit integer structured value.</summary>
    public LogValue CreateLongValue(long value)
    {
        return LogValue.Integer(value);
    }

    /// <summary>Creates a decimal structured value.</summary>
    public LogValue CreateDecimalValue(decimal value)
    {
        return LogValue.Decimal(value);
    }

    /// <summary>Creates a floating-point structured value.</summary>
    public LogValue CreateDoubleValue(double value)
    {
        return LogValue.FloatingPoint(value);
    }

    /// <summary>Creates a date and time structured value.</summary>
    public LogValue CreateDateTimeValue(DateTimeOffset value)
    {
        return LogValue.DateTime(value);
    }

    /// <summary>Creates a duration structured value.</summary>
    public LogValue CreateDurationValue(TimeSpan value)
    {
        return LogValue.Duration(value);
    }

    /// <summary>Creates an exception structured value.</summary>
    public LogValue CreateExceptionValue(Exception? value)
    {
        return LogValue.Exception(value);
    }

    /// <summary>Creates a length-limited exception structured value.</summary>
    public LogValue CreateExceptionValueWithLimit(Exception? value, int maximumStringLength)
    {
        return LogValue.ExceptionLimit(value, maximumStringLength);
    }

    /// <summary>Creates an object-text structured value without reflection.</summary>
    public LogValue CreateObjectTextValue(object? value)
    {
        return LogValue.ObjectText(value);
    }

    /// <summary>Creates a length-limited object-text structured value without reflection.</summary>
    public LogValue CreateObjectTextValueWithLimit(object? value, int maximumStringLength)
    {
        return LogValue.ObjectTextLimit(value, maximumStringLength);
    }

    /// <summary>Creates an array structured value from application values.</summary>
    public LogValue CreateArrayValue(object?[]? values)
    {
        return LogTemplateBinder.ToLogValue(values);
    }

    /// <summary>Creates an array structured value with string, collection, and nesting limits.</summary>
    public LogValue CreateArrayValueWithLimits(object?[]? values, int maximumStringLength, int maximumCollectionLength, int maximumDepth)
    {
        return LogTemplateBinder.ToLogValueLimit(values, maximumStringLength, maximumCollectionLength, maximumDepth);
    }

    /// <summary>Creates a dictionary structured value from application values.</summary>
    public LogValue CreateDictionaryValue(Dictionary<string, object>? values)
    {
        return LogTemplateBinder.ToLogValue(values);
    }

    /// <summary>Creates a dictionary structured value with string, collection, and nesting limits.</summary>
    public LogValue CreateDictionaryValueWithLimits(Dictionary<string, object>? values, int maximumStringLength, int maximumCollectionLength, int maximumDepth)
    {
        return LogTemplateBinder.ToLogValueLimit(values, maximumStringLength, maximumCollectionLength, maximumDepth);
    }

    /// <summary>Creates a validated raw JSON structured value.</summary>
    /// <exception cref="System.Text.Json.JsonException">The supplied text is not one complete JSON value.</exception>
    public LogValue CreateRawJsonValue(string? value)
    {
        return LogFactory.CreateValidatedRawJson(value, LogValue.DefaultMaximumStringLength, LogValue.DefaultMaximumDepth);
    }

    /// <summary>Creates a validated raw JSON structured value subject to string and nesting limits.</summary>
    /// <exception cref="System.Text.Json.JsonException">The supplied text is not one complete JSON value.</exception>
    /// <exception cref="ArgumentException">The supplied JSON exceeds a configured limit.</exception>
    public LogValue CreateRawJsonValueWithLimits(string? value, int maximumStringLength, int maximumDepth)
    {
        return LogFactory.CreateValidatedRawJson(value, maximumStringLength, maximumDepth);
    }

    /// <summary>Creates a structured field from an application value.</summary>
    public LogField CreateField(string name, object? value)
    {
        return LogField.ValueField(name, LogTemplateBinder.ToLogValue(value));
    }

    /// <summary>Creates a structured field from an application value with string, collection, and nesting limits.</summary>
    public LogField CreateFieldWithLimits(string name, object? value, int maximumStringLength, int maximumCollectionLength, int maximumDepth)
    {
        return new LogField(name, LogTemplateBinder.ToLogValueLimit(value, maximumStringLength, maximumCollectionLength, maximumDepth));
    }

    /// <summary>Creates a null structured field.</summary>
    public LogField CreateNullField(string name)
    {
        return new LogField(name, LogValue.Null());
    }

    /// <summary>Creates a string structured field.</summary>
    public LogField CreateStringField(string name, string? value)
    {
        return new LogField(name, LogValue.String(value));
    }

    /// <summary>Creates a length-limited string structured field.</summary>
    public LogField CreateStringFieldWithLimit(string name, string? value, int maximumStringLength)
    {
        return new LogField(name, LogValue.StringLimit(value, maximumStringLength));
    }

    /// <summary>Creates a Boolean structured field.</summary>
    public LogField CreateBoolField(string name, bool value)
    {
        return new LogField(name, LogValue.Bool(value));
    }

    /// <summary>Creates a 32-bit integer structured field.</summary>
    public LogField CreateIntField(string name, int value)
    {
        return new LogField(name, LogValue.Integer(value));
    }

    /// <summary>Creates a 64-bit integer structured field.</summary>
    public LogField CreateLongField(string name, long value)
    {
        return new LogField(name, LogValue.Integer(value));
    }

    /// <summary>Creates a decimal structured field.</summary>
    public LogField CreateDecimalField(string name, decimal value)
    {
        return new LogField(name, LogValue.Decimal(value));
    }

    /// <summary>Creates a floating-point structured field.</summary>
    public LogField CreateDoubleField(string name, double value)
    {
        return new LogField(name, LogValue.FloatingPoint(value));
    }

    /// <summary>Creates a date and time structured field.</summary>
    public LogField CreateDateTimeField(string name, DateTimeOffset value)
    {
        return new LogField(name, LogValue.DateTime(value));
    }

    /// <summary>Creates a duration structured field.</summary>
    public LogField CreateDurationField(string name, TimeSpan value)
    {
        return new LogField(name, LogValue.Duration(value));
    }

    /// <summary>Creates an exception structured field.</summary>
    public LogField CreateExceptionField(string name, Exception? value)
    {
        return new LogField(name, LogValue.Exception(value));
    }

    /// <summary>Creates a length-limited exception structured field.</summary>
    public LogField CreateExceptionFieldWithLimit(string name, Exception? value, int maximumStringLength)
    {
        return new LogField(name, LogValue.ExceptionLimit(value, maximumStringLength));
    }

    /// <summary>Creates an object-text structured field without reflection.</summary>
    public LogField CreateObjectTextField(string name, object? value)
    {
        return new LogField(name, LogValue.ObjectText(value));
    }

    /// <summary>Creates a length-limited object-text structured field without reflection.</summary>
    public LogField CreateObjectTextFieldWithLimit(string name, object? value, int maximumStringLength)
    {
        return new LogField(name, LogValue.ObjectTextLimit(value, maximumStringLength));
    }

    /// <summary>Creates an array structured field from application values.</summary>
    public LogField CreateArrayField(string name, object?[]? values)
    {
        return new LogField(name, LogTemplateBinder.ToLogValue(values));
    }

    /// <summary>Creates an array structured field with string, collection, and nesting limits.</summary>
    public LogField CreateArrayFieldWithLimits(string name, object?[]? values, int maximumStringLength, int maximumCollectionLength, int maximumDepth)
    {
        return new LogField(name, LogTemplateBinder.ToLogValueLimit(values, maximumStringLength, maximumCollectionLength, maximumDepth));
    }

    /// <summary>Creates a dictionary structured field from application values.</summary>
    public LogField CreateDictionaryField(string name, Dictionary<string, object>? values)
    {
        return new LogField(name, LogTemplateBinder.ToLogValue(values));
    }

    /// <summary>Creates a dictionary structured field with string, collection, and nesting limits.</summary>
    public LogField CreateDictionaryFieldWithLimits(string name, Dictionary<string, object>? values, int maximumStringLength, int maximumCollectionLength, int maximumDepth)
    {
        return new LogField(name, LogTemplateBinder.ToLogValueLimit(values, maximumStringLength, maximumCollectionLength, maximumDepth));
    }

    /// <summary>Creates a validated raw JSON structured field.</summary>
    /// <exception cref="System.Text.Json.JsonException">The supplied text is not one complete JSON value.</exception>
    public LogField CreateRawJsonField(string name, string? value)
    {
        return new LogField(name, LogFactory.CreateValidatedRawJson(value, LogValue.DefaultMaximumStringLength, LogValue.DefaultMaximumDepth));
    }

    /// <summary>Creates a validated raw JSON structured field subject to string and nesting limits.</summary>
    /// <exception cref="System.Text.Json.JsonException">The supplied text is not one complete JSON value.</exception>
    /// <exception cref="ArgumentException">The supplied JSON exceeds a configured limit.</exception>
    public LogField CreateRawJsonFieldWithLimits(string name, string? value, int maximumStringLength, int maximumDepth)
    {
        return new LogField(name, LogFactory.CreateValidatedRawJson(value, maximumStringLength, maximumDepth));
    }

    /// <summary>Creates a log event with runtime metadata defaults.</summary>
    public LogEvent CreateEvent(LogLevel level, string loggerName, string messageTemplate, string renderedMessage, LogField[] fields)
    {
        return LogEvent.Create(level, loggerName, messageTemplate, renderedMessage, fields);
    }

    /// <summary>Creates the default text formatter.</summary>
    public LogFormatter CreateTextFormatter()
    {
        return TextLogFormatter.Plain();
    }

    /// <summary>Creates the compact JSON formatter.</summary>
    public LogFormatter CreateCompactJsonFormatter()
    {
        return JsonLogFormatter.Compact();
    }

    /// <summary>Creates the full JSON formatter.</summary>
    public LogFormatter CreateFullJsonFormatter()
    {
        return JsonLogFormatter.Full();
    }

    /// <summary>Creates the plain console formatter.</summary>
    public LogFormatter CreatePlainConsoleFormatter()
    {
        return ConsoleTextLogFormatter.Plain();
    }

    /// <summary>Creates the ANSI-color console formatter.</summary>
    public LogFormatter CreateColorConsoleFormatter()
    {
        return ConsoleTextLogFormatter.AnsiColor();
    }

    /// <summary>Creates the key-value formatter.</summary>
    public LogFormatter CreateKeyValueFormatter()
    {
        return KeyValueLogFormatter.KeyValue();
    }

    /// <summary>Creates the default output-template formatter.</summary>
    public LogFormatter CreateTemplateFormatter()
    {
        return TemplateLogFormatter.Template();
    }

    /// <summary>Creates an output-template formatter.</summary>
    public LogFormatter CreateOutputTemplateFormatter(string outputTemplate)
    {
        return TemplateLogFormatter.WithTemplate(outputTemplate);
    }

    /// <summary>Creates the full exception formatter.</summary>
    public LogFormatter CreateFullExceptionFormatter()
    {
        return ExceptionLogFormatter.Full();
    }

    /// <summary>Creates the summary exception formatter.</summary>
    public LogFormatter CreateSummaryExceptionFormatter()
    {
        return ExceptionLogFormatter.Summary();
    }

    /// <summary>Creates a named formatter.</summary>
    public LogFormatter CreateNamedFormatter(string name)
    {
        return LogFormatter.Named(name);
    }

    /// <summary>Creates a callback-based formatter.</summary>
    public LogFormatter CreateCustomFormatter(string name, Func<LogEvent, string> formatAction)
    {
        return LogFormatter.Custom(name, formatAction);
    }

    /// <summary>Wraps an interface-based formatter.</summary>
    public LogFormatter WrapFormatter(ILogFormatter? formatter)
    {
        return LogFormatter.Wrap(formatter);
    }

    /// <summary>Wraps an interface-based filter.</summary>
    public LogFilter WrapFilter(ILogFilter filter)
    {
        return LogFilter.Wrap(filter);
    }

    /// <summary>Creates a minimum-level filter.</summary>
    public LogFilter CreateMinimumLevelFilter(LogLevel level)
    {
        return LogFilter.MinimumLevel(level);
    }

    /// <summary>Creates a deterministic sampling filter.</summary>
    public LogFilter CreateSamplingFilter(int count)
    {
        return LogFilter.SampleEvery(count);
    }

    /// <summary>Creates a one-second rate-limit filter.</summary>
    public LogFilter CreateRateLimitFilter(int count)
    {
        return LogFilter.RateLimit(count);
    }

    /// <summary>Creates a windowed rate-limit filter.</summary>
    public LogFilter CreateWindowedRateLimitFilter(int count, TimeSpan window)
    {
        return LogFilter.RateLimitWindow(count, window);
    }

    /// <summary>Creates a rate-limit filter with an injected clock.</summary>
    public LogFilter CreateClockedRateLimitFilter(int count, TimeSpan window, ILogClock clock)
    {
        return LogFilter.RateLimitWithClock(count, window, clock);
    }

    /// <summary>Creates a category-prefix filter.</summary>
    public LogFilter CreateCategoryFilter(string prefix, LogLevel level)
    {
        return LogFilter.CategoryPrefix(prefix, level);
    }

    /// <summary>Creates a field-presence filter.</summary>
    public LogFilter CreateFieldExistsFilter(string name)
    {
        return LogFilter.FieldExists(name);
    }

    /// <summary>Creates a normalized field-value filter.</summary>
    public LogFilter CreateFieldEqualsFilter(string name, object value)
    {
        return LogFilter.FieldEquals(name, value);
    }

    /// <summary>Creates a structured field-value filter.</summary>
    public LogFilter CreateStructuredFieldEqualsFilter(string name, LogValue value)
    {
        return LogFilter.FieldEqualsValue(name, value);
    }

    /// <summary>Wraps an interface-based sink.</summary>
    public LogSink WrapSink(ILogSink sink)
    {
        return LogSink.Wrap(sink);
    }

    /// <summary>Creates a named sink pipeline.</summary>
    public LogSinkPipeline CreateSinkPipeline(string name)
    {
        return new LogSinkPipeline(name);
    }

    /// <summary>Creates a null sink.</summary>
    public LogSink CreateNullSink(string name)
    {
        return NullLogSink.Named(name);
    }

    /// <summary>Creates a bounded or unbounded memory sink.</summary>
    public LogSink CreateMemorySink(string name, int maximumEvents)
    {
        LogSink sink = MemoryLogSink.Named(name);
        sink.MaximumMemoryEvents = MemoryLogSink.NormalizeMaximumEvents(maximumEvents);
        return sink;
    }

    /// <summary>Creates a text-writer sink.</summary>
    public LogSink CreateTextWriterSink(System.IO.TextWriter? writer, ILogFormatter? formatter, bool flushOnWrite)
    {
        LogSink sink = TextWriterLogSink.WithFormatter(writer, LogFormatter.Wrap(formatter));
        TextWriterLogSink.SetFlushOnWrite(sink, flushOnWrite);
        return sink;
    }

    /// <summary>Creates a console sink.</summary>
    public LogSink CreateConsoleSink(System.IO.TextWriter? outputWriter, System.IO.TextWriter? errorWriter, ILogFormatter? formatter, bool useAnsiColor)
    {
        LogSink sink = ConsoleLogSink.WithFormatter(outputWriter, errorWriter, LogFormatter.Wrap(formatter));
        if (formatter == null)
        {
            sink.SinkFormatter = ConsoleTextLogFormatter.WithAnsiColor(useAnsiColor);
        }

        return sink;
    }

    /// <summary>Creates a debug sink.</summary>
    public LogSink CreateDebugSink(Action<string>? writeAction, ILogFormatter? formatter, bool flushOnWrite)
    {
        LogSink sink = DebugLogSink.WithFormatter(writeAction, LogFormatter.Wrap(formatter));
        DebugLogSink.SetFlushOnWrite(sink, flushOnWrite);
        return sink;
    }

    /// <summary>Creates a trace sink.</summary>
    public LogSink CreateTraceSink(Action<string>? writeAction, ILogFormatter? formatter, bool flushOnWrite)
    {
        LogSink sink = TraceLogSink.WithFormatter(writeAction, LogFormatter.Wrap(formatter));
        TraceLogSink.SetFlushOnWrite(sink, flushOnWrite);
        return sink;
    }

    /// <summary>Creates a file sink with optional rolling and retention.</summary>
    public LogSink CreateFileSink(string filePath, ILogFormatter? formatter, bool append, LogRollingPolicy? rollingPolicy, LogRetentionPolicy? retentionPolicy, bool flushOnWrite)
    {
        LogSink sink = FileLogSink.WithFormatter(filePath, LogFormatter.Wrap(formatter), append);
        if (rollingPolicy != null)
        {
            FileLogSink.SetRollingPolicy(sink, rollingPolicy);
        }

        if (retentionPolicy != null)
        {
            FileLogSink.SetRetentionPolicy(sink, retentionPolicy);
        }

        FileLogSink.SetFlushOnWrite(sink, flushOnWrite);
        return sink;
    }

    /// <summary>Creates an EventSource sink.</summary>
    public LogSink CreateEventSourceSink(string sourceName, ILogFormatter? formatter, Action<LogEventSourceEntry>? writeAction)
    {
        LogSink sink = EventSourceLogSink.WithFormatter(sourceName, LogFormatter.Wrap(formatter));
        if (writeAction != null)
        {
            EventSourceLogSink.SetWriteAction(sink, writeAction);
        }

        return sink;
    }

    /// <summary>Creates a callback-based sink.</summary>
    public LogSink CreateDelegateSink(string name, Func<LogEvent, LogResult?>? writeAction)
    {
        return DelegateLogSink.Named(name, writeAction);
    }

    /// <summary>Creates an asynchronous queue wrapper.</summary>
    public LogSink CreateAsyncSink(string name, LogSink? innerSink, int capacity, LogBackpressureMode mode)
    {
        return AsyncLogSink.NamedWithBackpressure(name, innerSink, capacity, mode);
    }

    /// <summary>Creates a batch wrapper.</summary>
    public LogSink CreateBatchSink(string name, LogSink? innerSink, int batchSize)
    {
        return BatchLogSink.Named(name, innerSink, batchSize);
    }

    /// <summary>Creates a circuit-breaker wrapper.</summary>
    public LogSink CreateCircuitBreakerSink(string name, LogSink? innerSink, int failureThreshold)
    {
        return CircuitBreakerLogSink.Named(name, innerSink, failureThreshold);
    }

    /// <summary>Returns memory-sink events.</summary>
    public LogEvent[] GetMemorySinkEvents(LogSink sink)
    {
        return MemoryLogSink.Events(sink);
    }

    /// <summary>Clears memory-sink events.</summary>
    public void ClearMemorySink(LogSink sink)
    {
        MemoryLogSink.Clear(sink);
    }

    /// <summary>Returns queued asynchronous events.</summary>
    public LogEvent[] GetAsyncSinkEvents(LogSink sink)
    {
        return AsyncLogSink.Events(sink);
    }

    /// <summary>Returns queued batch events.</summary>
    public LogEvent[] GetBatchSinkEvents(LogSink sink)
    {
        return BatchLogSink.Events(sink);
    }

    /// <summary>Sets a sink minimum level.</summary>
    public void SetSinkMinimumLevel(LogSink sink, LogLevel level)
    {
        sink.SetMinimumLevel(level);
    }

    /// <summary>Clears a sink minimum level.</summary>
    public void ClearSinkMinimumLevel(LogSink sink)
    {
        sink.ClearMinimumLevel();
    }

    /// <summary>Adds an interface-based sink filter.</summary>
    public void AddSinkFilter(LogSink sink, ILogFilter filter)
    {
        sink.AddInterfaceFilter(filter);
    }

    /// <summary>Creates a rolling policy.</summary>
    public LogRollingPolicy CreateRollingPolicy(LogRollingInterval interval, long maximumFileSizeBytes)
    {
        return LogRollingPolicy.ByIntervalAndSize(interval, maximumFileSizeBytes);
    }

    /// <summary>Creates a retention policy.</summary>
    public LogRetentionPolicy CreateRetentionPolicy(int maximumFileCount, TimeSpan maximumFileAge)
    {
        return LogRetentionPolicy.ByCountAndAge(maximumFileCount, maximumFileAge);
    }

    /// <summary>Loads configuration from JSON.</summary>
    public LogConfiguration LoadJson(string json)
    {
        return LogJsonConfigurationLoader.Load(json);
    }

    /// <summary>Loads configuration from a JSON file.</summary>
    public LogConfiguration LoadJsonFile(string filePath)
    {
        return LogJsonConfigurationLoader.LoadFile(filePath);
    }

    /// <summary>Loads JSON and applies process environment values.</summary>
    public LogConfiguration LoadJsonWithEnvironment(string json)
    {
        return LogJsonConfigurationLoader.LoadWithEnvironment(json);
    }

    /// <summary>Loads a JSON file and applies process environment values.</summary>
    public LogConfiguration LoadJsonFileWithEnvironment(string filePath)
    {
        return LogJsonConfigurationLoader.LoadFileWithEnvironment(filePath);
    }

    /// <summary>Creates a JSON file watcher.</summary>
    public LogConfigurationReload WatchJsonFile(string filePath)
    {
        return LogJsonConfigurationLoader.WatchFile(filePath);
    }

    /// <summary>Creates a JSON file watcher that applies environment values.</summary>
    public LogConfigurationReload WatchJsonFileWithEnvironment(string filePath)
    {
        return LogJsonConfigurationLoader.WatchFileWithEnvironment(filePath);
    }

    internal static LogValue CreateValidatedRawJson(string? value, int maximumStringLength, int maximumDepth)
    {
        string normalizedValue = LogResult.NormalizeText(value);
        int stringLimit = LogValue.NormalizeMaximumLength(maximumStringLength);
        int depthLimit = LogValue.NormalizeMaximumLength(maximumDepth);
        if (normalizedValue.Length > stringLimit)
        {
            throw new ArgumentException("raw JSON exceeds the maximum string length");
        }

        using (System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(normalizedValue))
        {
            int capturedDepth = LogFactory.JsonDepth(document.RootElement);
            if (capturedDepth > depthLimit)
            {
                throw new ArgumentException("raw JSON exceeds the maximum nesting depth");
            }

            LogValue result = LogValue.RawLimit(normalizedValue, stringLimit);
            result.SetLimitMetadata(false, normalizedValue.Length, normalizedValue.Length, stringLimit, capturedDepth);
            return result;
        }
    }

    internal static int JsonDepth(System.Text.Json.JsonElement element)
    {
        int maximumChildDepth = 0;
        if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (System.Text.Json.JsonElement child in element.EnumerateArray())
            {
                int childDepth = LogFactory.JsonDepth(child);
                if (childDepth > maximumChildDepth)
                {
                    maximumChildDepth = childDepth;
                }
            }

            return maximumChildDepth + 1;
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (System.Text.Json.JsonProperty property in element.EnumerateObject())
            {
                int childDepth = LogFactory.JsonDepth(property.Value);
                if (childDepth > maximumChildDepth)
                {
                    maximumChildDepth = childDepth;
                }
            }

            return maximumChildDepth + 1;
        }

        return 0;
    }
}
