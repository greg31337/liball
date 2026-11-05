using System;

namespace log;

/// <summary>
/// Creates logger configurations from JSON text or files.
/// </summary>
internal class LogJsonConfigurationLoader
{
    /// <summary>
    /// Parses configuration data and returns a populated configuration object.
    /// </summary>
    /// <param name="json">The JSON configuration text.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    internal static LogConfiguration Load(string json)
    {
        LogConfiguration configuration = LogConfiguration.Create();
        LogJsonConfigurationLoader.ApplyJson(configuration, json);
        return configuration;
    }

    /// <summary>
    /// Parses configuration data and returns a populated configuration object.
    /// </summary>
    /// <param name="json">The JSON configuration text.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    internal static LogConfiguration FromJson(string json)
    {
        return LogJsonConfigurationLoader.Load(json);
    }

    /// <summary>
    /// Parses configuration data and returns a populated configuration object.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    internal static LogConfiguration LoadFile(string filePath)
    {
        return LogJsonConfigurationLoader.Load(System.IO.File.ReadAllText(filePath));
    }

    /// <summary>
    /// Parses configuration data and returns a populated configuration object.
    /// </summary>
    /// <param name="json">The JSON configuration text.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    internal static LogConfiguration LoadWithEnvironment(string json)
    {
        LogConfiguration configuration = LogJsonConfigurationLoader.Load(json);
        return LogEnvironmentConfiguration.Apply(configuration);
    }

    /// <summary>
    /// Parses configuration data and returns a populated configuration object.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    internal static LogConfiguration LoadFileWithEnvironment(string filePath)
    {
        return LogJsonConfigurationLoader.LoadWithEnvironment(System.IO.File.ReadAllText(filePath));
    }

    /// <summary>
    /// Creates a watcher for the supplied JSON configuration file.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <returns>The configured file watcher.</returns>
    internal static LogConfigurationReload WatchFile(string filePath)
    {
        return LogConfigurationReload.WatchJsonFile(filePath);
    }

    /// <summary>
    /// Creates a watcher for the supplied JSON configuration file.
    /// </summary>
    /// <param name="filePath">The configuration or log file path.</param>
    /// <returns>The configured file watcher.</returns>
    internal static LogConfigurationReload WatchFileWithEnvironment(string filePath)
    {
        return LogConfigurationReload.WatchJsonFileWithEnvironment(filePath);
    }

    internal static void ApplyJson(LogConfiguration configuration, string json)
    {
        if (configuration == null)
        {
            return;
        }

        using (System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(LogResult.NormalizeText(json)))
        {
            LogJsonConfigurationLoader.ApplyObject(configuration, document.RootElement);
        }

        configuration.Validate();
    }

    internal static void ApplyObject(LogConfiguration configuration, System.Text.Json.JsonElement element)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return;
        }

        System.Text.Json.JsonElement name = LogJsonConfigurationLoader.Property(element, "name");
        if (name.ValueKind == System.Text.Json.JsonValueKind.Undefined)
        {
            name = LogJsonConfigurationLoader.Property(element, "loggerName");
        }

        if (name.ValueKind != System.Text.Json.JsonValueKind.Undefined)
        {
            configuration.Name(LogJsonConfigurationLoader.StringValue(name, ""));
        }

        System.Text.Json.JsonElement minimumLevel = LogJsonConfigurationLoader.Property(element, "minimumLevel");
        if (minimumLevel.ValueKind == System.Text.Json.JsonValueKind.Undefined)
        {
            minimumLevel = LogJsonConfigurationLoader.Property(element, "level");
        }

        if (minimumLevel.ValueKind != System.Text.Json.JsonValueKind.Undefined)
        {
            configuration.MinimumLevel(LogJsonConfigurationLoader.LevelValue(minimumLevel, LogLevel.Info));
        }

        System.Text.Json.JsonElement runtimeMetadata = LogJsonConfigurationLoader.Property(element, "runtimeMetadata");
        if (runtimeMetadata.ValueKind == System.Text.Json.JsonValueKind.Undefined)
        {
            runtimeMetadata = LogJsonConfigurationLoader.Property(element, "includeRuntimeMetadata");
        }

        if (runtimeMetadata.ValueKind != System.Text.Json.JsonValueKind.Undefined)
        {
            configuration.RuntimeMetadata(LogJsonConfigurationLoader.BoolValue(runtimeMetadata, true));
        }

        System.Text.Json.JsonElement activity = LogJsonConfigurationLoader.Property(element, "activity");
        if (activity.ValueKind == System.Text.Json.JsonValueKind.Undefined)
        {
            activity = LogJsonConfigurationLoader.Property(element, "includeActivity");
        }

        if (activity.ValueKind != System.Text.Json.JsonValueKind.Undefined)
        {
            configuration.Activity(LogJsonConfigurationLoader.BoolValue(activity, true));
        }

        LogJsonConfigurationLoader.ApplyFields(configuration, element);
        LogJsonConfigurationLoader.ApplyCategoryLevels(configuration, element);
        LogJsonConfigurationLoader.ApplyFilters(configuration, element);
        LogJsonConfigurationLoader.ApplyEnrichers(configuration, element);
        LogJsonConfigurationLoader.ApplySinks(configuration, element);
    }

    internal static void ApplyFields(LogConfiguration configuration, System.Text.Json.JsonElement element)
    {
        System.Text.Json.JsonElement fields = LogJsonConfigurationLoader.Property(element, "globalFields");
        if (fields.ValueKind == System.Text.Json.JsonValueKind.Undefined)
        {
            fields = LogJsonConfigurationLoader.Property(element, "fields");
        }

        if (fields.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (System.Text.Json.JsonProperty property in fields.EnumerateObject())
            {
                configuration.AddGlobalLogField(LogField.ValueField(property.Name, LogJsonConfigurationLoader.LogValueFromJson(property.Value)));
            }
        }

        if (fields.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (System.Text.Json.JsonElement field in fields.EnumerateArray())
            {
                string name = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(field, "name"), "");
                System.Text.Json.JsonElement value = LogJsonConfigurationLoader.Property(field, "value");
                configuration.AddGlobalLogField(LogField.ValueField(name, LogJsonConfigurationLoader.LogValueFromJson(value)));
            }
        }
    }

    internal static void ApplyCategoryLevels(LogConfiguration configuration, System.Text.Json.JsonElement element)
    {
        System.Text.Json.JsonElement categoryLevels = LogJsonConfigurationLoader.Property(element, "categoryLevels");
        if (categoryLevels.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return;
        }

        foreach (System.Text.Json.JsonProperty property in categoryLevels.EnumerateObject())
        {
            configuration.CategoryLevel(property.Name, LogJsonConfigurationLoader.LevelValue(property.Value, LogLevel.Info));
        }
    }

    internal static void ApplyFilters(LogConfiguration configuration, System.Text.Json.JsonElement element)
    {
        System.Text.Json.JsonElement filters = LogJsonConfigurationLoader.Property(element, "filters");
        if (filters.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return;
        }

        foreach (System.Text.Json.JsonElement filter in filters.EnumerateArray())
        {
            configuration.AddFilter(LogJsonConfigurationLoader.FilterFromJson(filter));
        }
    }

    internal static void ApplyEnrichers(LogConfiguration configuration, System.Text.Json.JsonElement element)
    {
        System.Text.Json.JsonElement enrichers = LogJsonConfigurationLoader.Property(element, "enrichers");
        if (enrichers.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return;
        }

        foreach (System.Text.Json.JsonElement enricher in enrichers.EnumerateArray())
        {
            configuration.AddEnricher(LogJsonConfigurationLoader.EnricherFromJson(enricher));
        }
    }

    internal static void ApplySinks(LogConfiguration configuration, System.Text.Json.JsonElement element)
    {
        System.Text.Json.JsonElement sinks = LogJsonConfigurationLoader.Property(element, "sinks");
        if (sinks.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return;
        }

        foreach (System.Text.Json.JsonElement sink in sinks.EnumerateArray())
        {
            configuration.AddSink(LogJsonConfigurationLoader.SinkFromJson(sink));
        }
    }

    internal static LogSink SinkFromJson(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return LogJsonConfigurationLoader.SimpleSink(LogJsonConfigurationLoader.StringValue(element, ""));
        }

        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return LogSink.Named("json-unknown");
        }

        string kind = LogJsonConfigurationLoader.Kind(element);
        LogSink sink = LogJsonConfigurationLoader.SinkForKind(kind, element);
        LogJsonConfigurationLoader.ApplySinkOptions(sink, element);
        return sink;
    }

    internal static LogSink SinkForKind(string kind, System.Text.Json.JsonElement element)
    {
        if (kind == "memory")
        {
            return MemoryLogSink.Named(LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "name"), "memory"));
        }

        if (kind == "null")
        {
            return NullLogSink.Named(LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "name"), "null"));
        }

        if (kind == "file")
        {
            return LogJsonConfigurationLoader.FileSinkFromJson(element);
        }

        if (kind == "console")
        {
            return LogJsonConfigurationLoader.ConsoleSinkFromJson(element);
        }

        if (kind == "debug")
        {
            return DebugLogSink.Create();
        }

        if (kind == "trace")
        {
            return TraceLogSink.Create();
        }

        if (kind == "eventsource" || kind == "event-source")
        {
            return LogJsonConfigurationLoader.EventSourceSinkFromJson(element);
        }

        if (kind == "async")
        {
            return LogJsonConfigurationLoader.AsyncSinkFromJson(element);
        }

        if (kind == "batch")
        {
            return LogJsonConfigurationLoader.BatchSinkFromJson(element);
        }

        if (kind == "circuitbreaker" || kind == "circuit-breaker")
        {
            return LogJsonConfigurationLoader.CircuitBreakerSinkFromJson(element);
        }

        return LogSink.Named(LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "name"), "json-unknown"));
    }

    internal static LogSink SimpleSink(string kind)
    {
        string normalizedKind = LogJsonConfigurationLoader.NormalizeName(kind);
        if (normalizedKind == "memory")
        {
            return MemoryLogSink.Create();
        }

        if (normalizedKind == "null")
        {
            return NullLogSink.Create();
        }

        if (normalizedKind == "console")
        {
            return ConsoleLogSink.Create();
        }

        if (normalizedKind == "debug")
        {
            return DebugLogSink.Create();
        }

        if (normalizedKind == "trace")
        {
            return TraceLogSink.Create();
        }

        return LogSink.Named("json-unknown");
    }

    internal static LogSink FileSinkFromJson(System.Text.Json.JsonElement element)
    {
        string path = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "path"), "");
        if (path.Length == 0)
        {
            path = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "filePath"), "");
        }

        bool append = LogJsonConfigurationLoader.BoolValue(LogJsonConfigurationLoader.Property(element, "append"), true);
        LogFormatter formatter = LogJsonConfigurationLoader.FormatterFromJson(element);
        LogSink sink = FileLogSink.WithFormatter(path, formatter, append);
        System.Text.Json.JsonElement rolling = LogJsonConfigurationLoader.Property(element, "rolling");
        if (rolling.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            FileLogSink.SetRollingPolicy(sink, LogJsonConfigurationLoader.RollingPolicyFromJson(rolling));
        }

        FileLogSink.SetFlushOnWrite(sink, LogJsonConfigurationLoader.BoolValue(LogJsonConfigurationLoader.Property(element, "flushOnWrite"), false));
        return sink;
    }

    internal static LogSink ConsoleSinkFromJson(System.Text.Json.JsonElement element)
    {
        LogSink sink = ConsoleLogSink.WithFormatter(Console.Out, Console.Error, LogJsonConfigurationLoader.FormatterFromJson(element));
        ConsoleLogSink.SetErrorMinimumLevel(sink, LogJsonConfigurationLoader.LevelValue(LogJsonConfigurationLoader.Property(element, "errorMinimumLevel"), LogLevel.Error));
        ConsoleLogSink.SetFlushOnWrite(sink, LogJsonConfigurationLoader.BoolValue(LogJsonConfigurationLoader.Property(element, "flushOnWrite"), false));
        return sink;
    }

    internal static LogSink EventSourceSinkFromJson(System.Text.Json.JsonElement element)
    {
        string sourceName = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "sourceName"), EventSourceLogSink.DefaultSourceName);
        LogSink sink = EventSourceLogSink.WithFormatter(sourceName, LogJsonConfigurationLoader.FormatterFromJson(element));
        EventSourceLogSink.SetEventName(sink, LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "eventName"), EventSourceLogSink.DefaultEventName));
        return sink;
    }

    internal static LogSink AsyncSinkFromJson(System.Text.Json.JsonElement element)
    {
        LogSink? inner = LogJsonConfigurationLoader.InnerSinkFromJson(element);
        int capacity = LogJsonConfigurationLoader.IntValue(LogJsonConfigurationLoader.Property(element, "capacity"), AsyncLogSink.DefaultCapacity);
        LogBackpressureMode mode = LogJsonConfigurationLoader.BackpressureValue(LogJsonConfigurationLoader.Property(element, "backpressure"), LogBackpressureMode.DropNewest);
        return AsyncLogSink.WithBackpressure(inner, capacity, mode);
    }

    internal static LogSink BatchSinkFromJson(System.Text.Json.JsonElement element)
    {
        LogSink? inner = LogJsonConfigurationLoader.InnerSinkFromJson(element);
        int batchSize = LogJsonConfigurationLoader.IntValue(LogJsonConfigurationLoader.Property(element, "batchSize"), BatchLogSink.DefaultBatchSize);
        return BatchLogSink.WithSize(inner, batchSize);
    }

    internal static LogSink CircuitBreakerSinkFromJson(System.Text.Json.JsonElement element)
    {
        LogSink? inner = LogJsonConfigurationLoader.InnerSinkFromJson(element);
        int threshold = LogJsonConfigurationLoader.IntValue(LogJsonConfigurationLoader.Property(element, "failureThreshold"), CircuitBreakerLogSink.DefaultFailureThreshold);
        return CircuitBreakerLogSink.WithThreshold(inner, threshold);
    }

    internal static LogSink? InnerSinkFromJson(System.Text.Json.JsonElement element)
    {
        System.Text.Json.JsonElement inner = LogJsonConfigurationLoader.Property(element, "inner");
        if (inner.ValueKind == System.Text.Json.JsonValueKind.Undefined)
        {
            inner = LogJsonConfigurationLoader.Property(element, "innerSink");
        }

        if (inner.ValueKind == System.Text.Json.JsonValueKind.Undefined)
        {
            return null;
        }

        return LogJsonConfigurationLoader.SinkFromJson(inner);
    }

    internal static void ApplySinkOptions(LogSink sink, System.Text.Json.JsonElement element)
    {
        if (sink == null || element.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return;
        }

        string configuredName = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "name"), "");
        if (configuredName.Length > 0)
        {
            sink.Name = configuredName;
        }

        System.Text.Json.JsonElement enabled = LogJsonConfigurationLoader.Property(element, "enabled");
        if (enabled.ValueKind != System.Text.Json.JsonValueKind.Undefined)
        {
            sink.IsEnabled = LogJsonConfigurationLoader.BoolValue(enabled, true);
        }

        System.Text.Json.JsonElement minimumLevel = LogJsonConfigurationLoader.Property(element, "minimumLevel");
        if (minimumLevel.ValueKind != System.Text.Json.JsonValueKind.Undefined)
        {
            sink.SetMinimumLevel(LogJsonConfigurationLoader.LevelValue(minimumLevel, LogLevel.Trace));
        }

        System.Text.Json.JsonElement filters = LogJsonConfigurationLoader.Property(element, "filters");
        if (filters.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            sink.SetFilters(LogJsonConfigurationLoader.FiltersFromJson(filters));
        }
    }

    internal static LogFilter[] FiltersFromJson(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return new LogFilter[0];
        }

        System.Collections.Generic.List<LogFilter> filters = new System.Collections.Generic.List<LogFilter>();
        foreach (System.Text.Json.JsonElement filter in element.EnumerateArray())
        {
            filters.Add(LogJsonConfigurationLoader.FilterFromJson(filter));
        }

        return filters.ToArray();
    }

    internal static LogFilter FilterFromJson(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return new LogFilter();
        }

        string kind = LogJsonConfigurationLoader.Kind(element);
        if (kind == LogFilter.MinimumLevelKind.ToLowerInvariant() || kind == "minimum-level")
        {
            return LogFilter.MinimumLevel(LogJsonConfigurationLoader.LevelValue(LogJsonConfigurationLoader.Property(element, "level"), LogLevel.Info));
        }

        if (kind == LogFilter.CategoryPrefixKind.ToLowerInvariant() || kind == "category-prefix")
        {
            string prefix = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "prefix"), "");
            return LogFilter.CategoryPrefix(prefix, LogJsonConfigurationLoader.LevelValue(LogJsonConfigurationLoader.Property(element, "level"), LogLevel.Info));
        }

        if (kind == LogFilter.FieldExistsKind.ToLowerInvariant() || kind == "field-exists")
        {
            return LogFilter.FieldExists(LogJsonConfigurationLoader.FieldNameFromJson(element));
        }

        if (kind == LogFilter.FieldEqualsKind.ToLowerInvariant() || kind == "field-equals")
        {
            return LogFilter.FieldEqualsValue(LogJsonConfigurationLoader.FieldNameFromJson(element), LogJsonConfigurationLoader.LogValueFromJson(LogJsonConfigurationLoader.Property(element, "value")));
        }

        if (kind == LogFilter.SampleEveryKind.ToLowerInvariant() || kind == "sample-every")
        {
            return LogFilter.SampleEvery(LogJsonConfigurationLoader.IntValue(LogJsonConfigurationLoader.Property(element, "count"), 1));
        }

        if (kind == LogFilter.RateLimitKind.ToLowerInvariant() || kind == "rate-limit")
        {
            int count = LogJsonConfigurationLoader.IntValue(LogJsonConfigurationLoader.Property(element, "count"), 0);
            int windowMilliseconds = LogJsonConfigurationLoader.IntValue(LogJsonConfigurationLoader.Property(element, "windowMilliseconds"), 1000);
            return LogFilter.RateLimitWindow(count, TimeSpan.FromMilliseconds(windowMilliseconds));
        }

        LogFilter filter = new LogFilter();
        filter.Kind = kind;
        return filter;
    }

    internal static LogEnricher EnricherFromJson(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return LogJsonConfigurationLoader.SimpleEnricher(LogJsonConfigurationLoader.StringValue(element, ""));
        }

        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return new LogEnricher();
        }

        string kind = LogJsonConfigurationLoader.Kind(element);
        if (kind == LogEnricher.FieldKind)
        {
            string name = LogJsonConfigurationLoader.FieldNameFromJson(element);
            return LogEnricher.FieldValueEnricher(LogField.ValueField(name, LogJsonConfigurationLoader.LogValueFromJson(LogJsonConfigurationLoader.Property(element, "value"))));
        }

        return LogJsonConfigurationLoader.SimpleEnricher(kind);
    }

    internal static LogEnricher SimpleEnricher(string kind)
    {
        string normalizedKind = LogJsonConfigurationLoader.NormalizeName(kind);
        if (normalizedKind == LogEnricher.RuntimeKind)
        {
            return LogEnricher.Runtime();
        }

        if (normalizedKind == LogEnricher.ActivityKind)
        {
            return LogEnricher.Activity();
        }

        LogEnricher enricher = new LogEnricher();
        enricher.Kind = normalizedKind;
        return enricher;
    }

    internal static LogRollingPolicy RollingPolicyFromJson(System.Text.Json.JsonElement element)
    {
        LogRollingInterval interval = LogJsonConfigurationLoader.RollingIntervalValue(LogJsonConfigurationLoader.Property(element, "interval"), LogRollingInterval.None);
        long maximumFileSizeBytes = LogJsonConfigurationLoader.LongValue(LogJsonConfigurationLoader.Property(element, "maximumFileSizeBytes"), 0);
        if (maximumFileSizeBytes == 0)
        {
            maximumFileSizeBytes = LogJsonConfigurationLoader.LongValue(LogJsonConfigurationLoader.Property(element, "sizeBytes"), 0);
        }

        return LogRollingPolicy.ByIntervalAndSize(interval, maximumFileSizeBytes);
    }

    internal static LogFormatter FormatterFromJson(System.Text.Json.JsonElement element)
    {
        string formatterName = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "formatter"), LogFormatter.TextName);
        string normalizedName = LogFormatter.NormalizeName(formatterName);
        if (normalizedName == LogFormatter.TemplateName)
        {
            string outputTemplate = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "outputTemplate"), TemplateLogFormatter.DefaultTemplate);
            return TemplateLogFormatter.WithTemplate(outputTemplate);
        }

        return LogFormatter.Named(normalizedName);
    }

    internal static string FieldNameFromJson(System.Text.Json.JsonElement element)
    {
        string name = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "field"), "");
        if (name.Length == 0)
        {
            name = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "name"), "");
        }

        return name;
    }

    internal static string Kind(System.Text.Json.JsonElement element)
    {
        string kind = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "kind"), "");
        if (kind.Length == 0)
        {
            kind = LogJsonConfigurationLoader.StringValue(LogJsonConfigurationLoader.Property(element, "type"), "");
        }

        return LogJsonConfigurationLoader.NormalizeName(kind);
    }

    internal static System.Text.Json.JsonElement Property(System.Text.Json.JsonElement element, string name)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return default(System.Text.Json.JsonElement);
        }

        foreach (System.Text.Json.JsonProperty property in element.EnumerateObject())
        {
            if (LogJsonConfigurationLoader.NameEquals(property.Name, name))
            {
                return property.Value;
            }
        }

        return default(System.Text.Json.JsonElement);
    }

    internal static bool NameEquals(string left, string right)
    {
        return string.Equals(LogResult.NormalizeText(left), LogResult.NormalizeText(right), StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeName(string value)
    {
        return LogResult.NormalizeText(value).Trim().ToLowerInvariant();
    }

    internal static string StringValue(System.Text.Json.JsonElement element, string fallback)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return LogResult.NormalizeText(element.GetString());
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            return element.GetRawText();
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.True)
        {
            return "true";
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.False)
        {
            return "false";
        }

        return LogResult.NormalizeText(fallback);
    }

    internal static bool BoolValue(System.Text.Json.JsonElement element, bool fallback)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.True)
        {
            return true;
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.False)
        {
            return false;
        }

        string text = LogJsonConfigurationLoader.NormalizeName(LogJsonConfigurationLoader.StringValue(element, ""));
        if (text == "true" || text == "yes" || text == "on" || text == "1")
        {
            return true;
        }

        if (text == "false" || text == "no" || text == "off" || text == "0")
        {
            return false;
        }

        return fallback;
    }

    internal static int IntValue(System.Text.Json.JsonElement element, int fallback)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            int value = 0;
            if (element.TryGetInt32(out value))
            {
                return value;
            }
        }

        int parsed = 0;
        if (int.TryParse(LogJsonConfigurationLoader.StringValue(element, ""), out parsed))
        {
            return parsed;
        }

        return fallback;
    }

    internal static long LongValue(System.Text.Json.JsonElement element, long fallback)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            long value = 0;
            if (element.TryGetInt64(out value))
            {
                return value;
            }
        }

        long parsed = 0;
        if (long.TryParse(LogJsonConfigurationLoader.StringValue(element, ""), out parsed))
        {
            return parsed;
        }

        return fallback;
    }

    internal static LogLevel LevelValue(System.Text.Json.JsonElement element, LogLevel fallback)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            return LogLevelSwitch.NormalizeLevel((LogLevel)LogJsonConfigurationLoader.IntValue(element, (int)fallback));
        }

        string text = LogJsonConfigurationLoader.NormalizeName(LogJsonConfigurationLoader.StringValue(element, ""));
        if (text == "trace")
        {
            return LogLevel.Trace;
        }

        if (text == "debug")
        {
            return LogLevel.Debug;
        }

        if (text == "info" || text == "information")
        {
            return LogLevel.Info;
        }

        if (text == "warn" || text == "warning")
        {
            return LogLevel.Warn;
        }

        if (text == "error")
        {
            return LogLevel.Error;
        }

        if (text == "fatal" || text == "critical")
        {
            return LogLevel.Fatal;
        }

        if (text == "off" || text == "none")
        {
            return LogLevel.Off;
        }

        return fallback;
    }

    internal static LogBackpressureMode BackpressureValue(System.Text.Json.JsonElement element, LogBackpressureMode fallback)
    {
        string text = LogJsonConfigurationLoader.NormalizeName(LogJsonConfigurationLoader.StringValue(element, ""));
        if (text == "block")
        {
            return LogBackpressureMode.Block;
        }

        if (text == "dropoldest" || text == "drop-oldest")
        {
            return LogBackpressureMode.DropOldest;
        }

        if (text == "dropnewest" || text == "drop-newest")
        {
            return LogBackpressureMode.DropNewest;
        }

        return fallback;
    }

    internal static LogRollingInterval RollingIntervalValue(System.Text.Json.JsonElement element, LogRollingInterval fallback)
    {
        string text = LogJsonConfigurationLoader.NormalizeName(LogJsonConfigurationLoader.StringValue(element, ""));
        if (text == "hour")
        {
            return LogRollingInterval.Hour;
        }

        if (text == "day" || text == "date")
        {
            return LogRollingInterval.Day;
        }

        if (text == "month")
        {
            return LogRollingInterval.Month;
        }

        if (text == "year")
        {
            return LogRollingInterval.Year;
        }

        if (text == "none" || text == "off")
        {
            return LogRollingInterval.None;
        }

        return fallback;
    }

    internal static LogValue LogValueFromJson(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Null || element.ValueKind == System.Text.Json.JsonValueKind.Undefined)
        {
            return LogValue.Null();
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return LogValue.String(LogResult.NormalizeText(element.GetString()));
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.True)
        {
            return LogValue.Bool(true);
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.False)
        {
            return LogValue.Bool(false);
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            long integerValue = 0;
            if (element.TryGetInt64(out integerValue))
            {
                return LogValue.Integer(integerValue);
            }

            double floatingPointValue = 0d;
            if (element.TryGetDouble(out floatingPointValue))
            {
                return LogValue.FloatingPoint(floatingPointValue);
            }

            return LogValue.Raw(element.GetRawText());
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            System.Collections.Generic.List<LogValue> values = new System.Collections.Generic.List<LogValue>();
            foreach (System.Text.Json.JsonElement item in element.EnumerateArray())
            {
                values.Add(LogJsonConfigurationLoader.LogValueFromJson(item));
            }

            return LogValue.Array(values.ToArray());
        }

        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            System.Collections.Generic.Dictionary<string, LogValue> values = new System.Collections.Generic.Dictionary<string, LogValue>();
            foreach (System.Text.Json.JsonProperty property in element.EnumerateObject())
            {
                values[property.Name] = LogJsonConfigurationLoader.LogValueFromJson(property.Value);
            }

            return LogValue.Dictionary(values);
        }

        return LogValue.Raw(element.GetRawText());
    }
}
