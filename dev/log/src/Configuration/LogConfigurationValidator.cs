using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Checks logger configurations before they are used to create a logger.
/// </summary>
internal class LogConfigurationValidator
{
    /// <summary>
    /// Returns every diagnostic found in the supplied configuration.
    /// </summary>
    /// <param name="configuration">The configuration to validate or install.</param>
    /// <returns>A newly allocated array containing every validation diagnostic.</returns>
    internal static LogConfigurationDiagnostic[] Validate(LogConfiguration configuration)
    {
        LogConfigurationDiagnostic[] diagnostics = new LogConfigurationDiagnostic[0];
        if (configuration == null)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                diagnostics,
                LogConfigurationDiagnostic.Error("configuration.null", "configuration cannot be null", "configuration"));
        }

        diagnostics = LogConfigurationValidator.ValidateLoggerName(diagnostics, configuration.LoggerName);
        diagnostics = LogConfigurationValidator.ValidateRootReferences(diagnostics, configuration);
        diagnostics = LogConfigurationValidator.ValidateSinks(diagnostics, configuration.Sinks, "sinks");
        diagnostics = LogConfigurationValidator.ValidateFilters(diagnostics, configuration.Filters, "filters");
        diagnostics = LogConfigurationValidator.ValidateEnrichers(diagnostics, configuration.Enrichers, "enrichers");
        diagnostics = LogConfigurationValidator.ValidateGlobalFields(diagnostics, configuration.GlobalFields, "globalFields");
        diagnostics = LogConfigurationValidator.ValidateCategoryLevelSwitches(diagnostics, configuration.CategoryLevelSwitches);
        return diagnostics;
    }

    /// <summary>
    /// Summarizes configuration diagnostics as a single result.
    /// </summary>
    /// <param name="configuration">The configuration to validate or install.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult ValidateResult(LogConfiguration configuration)
    {
        LogConfigurationDiagnostic[] diagnostics = LogConfigurationValidator.Validate(configuration);
        return LogConfigurationValidator.ToResult(diagnostics);
    }

    internal static LogResult ToResult(LogConfigurationDiagnostic[] diagnostics)
    {
        int errorCount = LogConfigurationDiagnostic.CountErrors(diagnostics);
        if (errorCount > 0)
        {
            LogConfigurationDiagnostic? error = LogConfigurationDiagnostic.FirstError(diagnostics);
            if (error == null)
            {
                return LogResult.FailureCode("configuration.invalid", "configuration is invalid");
            }

            return new LogResult(false, true, false, false, error.Code, error.Message, 0, errorCount, LogConfigurationDiagnostic.CountWarnings(diagnostics));
        }

        int warningCount = LogConfigurationDiagnostic.CountWarnings(diagnostics);
        if (warningCount > 0)
        {
            LogConfigurationDiagnostic? warning = LogConfigurationDiagnostic.FirstDiagnostic(diagnostics);
            if (warning == null)
            {
                return LogResult.DiagnosticCode("configuration.warning", "configuration has warnings");
            }

            return new LogResult(true, false, false, true, warning.Code, warning.Message, 0, 0, warningCount);
        }

        return LogResult.Success();
    }

    internal static bool HasErrors(LogConfigurationDiagnostic[] diagnostics)
    {
        return LogConfigurationDiagnostic.CountErrors(diagnostics) > 0;
    }

    /// <summary>
    /// Reports whether the supplied configuration has no validation errors.
    /// </summary>
    /// <param name="configuration">The configuration to validate or install.</param>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal static bool IsValid(LogConfiguration configuration)
    {
        return !LogConfigurationValidator.HasErrors(LogConfigurationValidator.Validate(configuration));
    }

    internal static LogConfigurationDiagnostic[] ValidateLoggerName(LogConfigurationDiagnostic[] diagnostics, string loggerName)
    {
        string normalizedName = LogName.NormalizeLoggerName(loggerName);
        if (normalizedName.Length == 0)
        {
            return diagnostics;
        }

        string validationError = LogName.ValidateLoggerName(normalizedName);
        if (validationError.Length == 0)
        {
            return diagnostics;
        }

        return LogConfigurationDiagnostic.AppendDiagnostic(
            diagnostics,
            LogConfigurationDiagnostic.Error("configuration.logger.name.invalid", validationError, "loggerName"));
    }

    internal static LogConfigurationDiagnostic[] ValidateRootReferences(LogConfigurationDiagnostic[] diagnostics, LogConfiguration configuration)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (configuration.ConfiguredLevelSwitch == null)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("configuration.levelSwitch.missing", "level switch cannot be null", "levelSwitch"));
        }

        if (configuration.ConfiguredClock == null)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("configuration.clock.missing", "clock cannot be null", "clock"));
        }

        return result;
    }

    internal static LogConfigurationDiagnostic[] ValidateSinks(LogConfigurationDiagnostic[] diagnostics, LogSink[] sinks, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (sinks == null || sinks.Length == 0)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Warning("configuration.sinks.empty", "no sinks are configured", path));
        }

        int usableSinkCount = 0;
        int index = 0;
        while (index < sinks.Length)
        {
            string sinkPath = path + "[" + index.ToString() + "]";
            if (sinks[index] == null)
            {
                result = LogConfigurationDiagnostic.AppendDiagnostic(
                    result,
                    LogConfigurationDiagnostic.Error("configuration.sink.null", "sink cannot be null", sinkPath));
            }
            else
            {
                usableSinkCount = usableSinkCount + 1;
                result = LogConfigurationValidator.ValidateSink(result, sinks[index], sinkPath);
            }

            index = index + 1;
        }

        if (usableSinkCount == 0)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Warning("configuration.sinks.empty", "no usable sinks are configured", path));
        }

        return result;
    }

    internal static LogConfigurationDiagnostic[] ValidateSink(LogConfigurationDiagnostic[] diagnostics, LogSink sink, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (sink == null)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("configuration.sink.null", "sink cannot be null", path));
        }

        if (sink.IsInterfaceSink && sink.InterfaceSink == null)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("configuration.sink.interface.missing", "interface sink cannot be null", path));
        }

        if (sink.IsTextWriterSink && sink.TextWriter == null)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("sink.textwriter.missing", "text writer cannot be null", path + ".writer"));
        }

        if (sink.IsConsoleSink)
        {
            result = LogConfigurationValidator.ValidateConsoleSink(result, sink, path);
        }

        if (sink.IsFileSink && LogResult.NormalizeText(sink.FilePath).Length == 0)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("sink.file.path.missing", "file path cannot be empty", path + ".filePath"));
        }

        if (sink.IsDelegateSink && sink.DelegateWriteAction == null)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("sink.delegate.missing", "delegate write action cannot be null", path + ".writeAction"));
        }

        if (sink.HasSubPipeline && sink.SubPipeline == null)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("configuration.sink.pipeline.missing", "sub-pipeline cannot be null", path + ".pipeline"));
        }

        result = LogConfigurationValidator.ValidateSinkFilters(result, sink, path);
        result = LogConfigurationValidator.ValidateAsyncSink(result, sink, path);
        result = LogConfigurationValidator.ValidateBatchSink(result, sink, path);
        result = LogConfigurationValidator.ValidateCircuitBreakerSink(result, sink, path);
        return result;
    }

    internal static LogConfigurationDiagnostic[] ValidateConsoleSink(LogConfigurationDiagnostic[] diagnostics, LogSink sink, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (sink.ConsoleOutWriter == null)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("sink.console.output.missing", "console output writer cannot be null", path + ".outputWriter"));
        }

        if (sink.ConsoleErrorWriter == null)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("sink.console.error.missing", "console error writer cannot be null", path + ".errorWriter"));
        }

        return result;
    }

    internal static LogConfigurationDiagnostic[] ValidateSinkFilters(LogConfigurationDiagnostic[] diagnostics, LogSink sink, string path)
    {
        if (sink.FilterPipeline == null)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                diagnostics,
                LogConfigurationDiagnostic.Error("configuration.sink.filters.missing", "sink filter pipeline cannot be null", path + ".filters"));
        }

        return LogConfigurationValidator.ValidateFilters(diagnostics, sink.FilterPipeline.Filters, path + ".filters");
    }

    internal static LogConfigurationDiagnostic[] ValidateAsyncSink(LogConfigurationDiagnostic[] diagnostics, LogSink sink, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (!sink.IsAsyncSink)
        {
            return result;
        }

        if (sink.AsyncQueueCapacity < 1)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("configuration.sink.async.capacity.invalid", "async queue capacity must be positive", path + ".capacity"));
        }

        if (sink.AsyncInnerSink == null)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("sink.async.inner.missing", "async inner sink cannot be null", path + ".inner"));
        }

        return LogConfigurationValidator.ValidateSink(result, sink.AsyncInnerSink, path + ".inner");
    }

    internal static LogConfigurationDiagnostic[] ValidateBatchSink(LogConfigurationDiagnostic[] diagnostics, LogSink sink, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (!sink.IsBatchSink)
        {
            return result;
        }

        if (sink.BatchSize < 1)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("configuration.sink.batch.size.invalid", "batch size must be positive", path + ".batchSize"));
        }

        if (sink.BatchInnerSink == null)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("sink.batch.inner.missing", "batch inner sink cannot be null", path + ".inner"));
        }

        return LogConfigurationValidator.ValidateSink(result, sink.BatchInnerSink, path + ".inner");
    }

    internal static LogConfigurationDiagnostic[] ValidateCircuitBreakerSink(LogConfigurationDiagnostic[] diagnostics, LogSink sink, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (!sink.IsCircuitBreakerSink)
        {
            return result;
        }

        if (sink.CircuitBreakerFailureThreshold < 1)
        {
            result = LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("configuration.sink.circuit.threshold.invalid", "circuit breaker failure threshold must be positive", path + ".failureThreshold"));
        }

        if (sink.CircuitBreakerInnerSink == null)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("sink.circuit.inner.missing", "circuit breaker inner sink cannot be null", path + ".inner"));
        }

        return LogConfigurationValidator.ValidateSink(result, sink.CircuitBreakerInnerSink, path + ".inner");
    }

    internal static LogConfigurationDiagnostic[] ValidateFilters(LogConfigurationDiagnostic[] diagnostics, LogFilter[] filters, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (filters == null)
        {
            return result;
        }

        int index = 0;
        while (index < filters.Length)
        {
            result = LogConfigurationValidator.ValidateFilter(result, filters[index], path + "[" + index.ToString() + "]");
            index = index + 1;
        }

        return result;
    }

    internal static LogConfigurationDiagnostic[] ValidateFilter(LogConfigurationDiagnostic[] diagnostics, LogFilter filter, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (filter == null)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("configuration.filter.null", "filter cannot be null", path));
        }

        if (filter.IsInterfaceFilter)
        {
            if (filter.InterfaceFilter == null)
            {
                return LogConfigurationDiagnostic.AppendDiagnostic(
                    result,
                    LogConfigurationDiagnostic.Error("configuration.filter.interface.missing", "interface filter cannot be null", path));
            }

            return result;
        }

        if (filter.Kind == LogFilter.MinimumLevelKind)
        {
            return result;
        }

        if (filter.Kind == LogFilter.CategoryPrefixKind)
        {
            return LogConfigurationValidator.ValidateCategoryPrefixFilter(result, filter, path);
        }

        if (filter.Kind == LogFilter.FieldExistsKind)
        {
            return LogConfigurationValidator.ValidateFieldNameText(result, filter.Text, "configuration.filter.field.invalid", "filter field name is invalid", path + ".field");
        }

        if (filter.Kind == LogFilter.FieldEqualsKind)
        {
            return LogConfigurationValidator.ValidateFieldNameText(result, filter.Text, "configuration.filter.field.invalid", "filter field name is invalid", path + ".field");
        }

        if (filter.Kind == LogFilter.SampleEveryKind)
        {
            return LogConfigurationValidator.ValidatePositiveCount(result, filter.Count, "configuration.filter.sample.count.invalid", "sample count must be positive", path + ".count");
        }

        if (filter.Kind == LogFilter.RateLimitKind)
        {
            result = LogConfigurationValidator.ValidateNonNegativeCount(result, filter.Count, "configuration.filter.ratelimit.count.invalid", "rate limit count cannot be negative", path + ".count");
            if (filter.Window <= TimeSpan.Zero)
            {
                result = LogConfigurationDiagnostic.AppendDiagnostic(
                    result,
                    LogConfigurationDiagnostic.Error("configuration.filter.ratelimit.window.invalid", "rate limit window must be positive", path + ".window"));
            }

            if (filter.Clock == null)
            {
                result = LogConfigurationDiagnostic.AppendDiagnostic(
                    result,
                    LogConfigurationDiagnostic.Error("configuration.filter.ratelimit.clock.missing", "rate limit clock cannot be null", path + ".clock"));
            }

            return result;
        }

        return LogConfigurationDiagnostic.AppendDiagnostic(
            result,
            LogConfigurationDiagnostic.Warning("configuration.filter.kind.unknown", "filter kind is unknown", path + ".kind"));
    }

    internal static LogConfigurationDiagnostic[] ValidateCategoryPrefixFilter(LogConfigurationDiagnostic[] diagnostics, LogFilter filter, string path)
    {
        string validationError = LogName.ValidateLoggerName(LogName.NormalizeLoggerName(filter.Text));
        if (validationError.Length == 0)
        {
            return diagnostics;
        }

        return LogConfigurationDiagnostic.AppendDiagnostic(
            diagnostics,
            LogConfigurationDiagnostic.Error("configuration.filter.category.invalid", validationError, path + ".prefix"));
    }

    internal static LogConfigurationDiagnostic[] ValidateFieldNameText(LogConfigurationDiagnostic[] diagnostics, string fieldName, string code, string message, string path)
    {
        string validationError = LogName.ValidateFieldName(fieldName);
        if (validationError.Length == 0)
        {
            return diagnostics;
        }

        return LogConfigurationDiagnostic.AppendDiagnostic(
            diagnostics,
            LogConfigurationDiagnostic.Error(code, message + ": " + validationError, path));
    }

    internal static LogConfigurationDiagnostic[] ValidatePositiveCount(LogConfigurationDiagnostic[] diagnostics, int count, string code, string message, string path)
    {
        if (count > 0)
        {
            return diagnostics;
        }

        return LogConfigurationDiagnostic.AppendDiagnostic(
            diagnostics,
            LogConfigurationDiagnostic.Error(code, message, path));
    }

    internal static LogConfigurationDiagnostic[] ValidateNonNegativeCount(LogConfigurationDiagnostic[] diagnostics, int count, string code, string message, string path)
    {
        if (count >= 0)
        {
            return diagnostics;
        }

        return LogConfigurationDiagnostic.AppendDiagnostic(
            diagnostics,
            LogConfigurationDiagnostic.Error(code, message, path));
    }

    internal static LogConfigurationDiagnostic[] ValidateEnrichers(LogConfigurationDiagnostic[] diagnostics, LogEnricher[] enrichers, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (enrichers == null)
        {
            return result;
        }

        int index = 0;
        while (index < enrichers.Length)
        {
            result = LogConfigurationValidator.ValidateEnricher(result, enrichers[index], path + "[" + index.ToString() + "]");
            index = index + 1;
        }

        return result;
    }

    internal static LogConfigurationDiagnostic[] ValidateEnricher(LogConfigurationDiagnostic[] diagnostics, LogEnricher enricher, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (enricher == null)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error("configuration.enricher.null", "enricher cannot be null", path));
        }

        if (enricher.Kind == LogEnricher.FieldKind)
        {
            return LogConfigurationValidator.ValidateField(result, enricher.FieldValue, "configuration.enricher.field.invalid", path + ".field");
        }

        if (enricher.Kind == LogEnricher.RuntimeKind)
        {
            return result;
        }

        if (enricher.Kind == LogEnricher.ActivityKind)
        {
            return result;
        }

        if (enricher.Kind == LogEnricher.InterfaceKind)
        {
            if (enricher.InterfaceEnricher == null)
            {
                return LogConfigurationDiagnostic.AppendDiagnostic(
                    result,
                    LogConfigurationDiagnostic.Error("configuration.enricher.interface.missing", "interface enricher cannot be null", path));
            }

            return result;
        }

        return LogConfigurationDiagnostic.AppendDiagnostic(
            result,
            LogConfigurationDiagnostic.Warning("configuration.enricher.kind.unknown", "enricher kind is unknown", path + ".kind"));
    }

    internal static LogConfigurationDiagnostic[] ValidateGlobalFields(LogConfigurationDiagnostic[] diagnostics, LogField[] fields, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (fields == null)
        {
            return result;
        }

        int index = 0;
        while (index < fields.Length)
        {
            result = LogConfigurationValidator.ValidateField(result, fields[index], "configuration.globalfield.invalid", path + "[" + index.ToString() + "]");
            index = index + 1;
        }

        return result;
    }

    internal static LogConfigurationDiagnostic[] ValidateField(LogConfigurationDiagnostic[] diagnostics, LogField field, string code, string path)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (field == null)
        {
            return LogConfigurationDiagnostic.AppendDiagnostic(
                result,
                LogConfigurationDiagnostic.Error(code, "field cannot be null", path));
        }

        if (field.IsValid)
        {
            return result;
        }

        return LogConfigurationDiagnostic.AppendDiagnostic(
            result,
            LogConfigurationDiagnostic.Error(code, field.ErrorMessage, path));
    }

    internal static LogConfigurationDiagnostic[] ValidateCategoryLevelSwitches(LogConfigurationDiagnostic[] diagnostics, Dictionary<string, LogLevelSwitch> switches)
    {
        LogConfigurationDiagnostic[] result = diagnostics;
        if (switches == null)
        {
            return result;
        }

        foreach (KeyValuePair<string, LogLevelSwitch> entry in switches)
        {
            string path = "categoryLevels." + entry.Key;
            string validationError = LogName.ValidateLoggerName(LogName.NormalizeLoggerName(entry.Key));
            if (validationError.Length > 0)
            {
                result = LogConfigurationDiagnostic.AppendDiagnostic(
                    result,
                    LogConfigurationDiagnostic.Error("configuration.category.name.invalid", validationError, path));
            }

            if (entry.Value == null)
            {
                result = LogConfigurationDiagnostic.AppendDiagnostic(
                    result,
                    LogConfigurationDiagnostic.Error("configuration.category.levelSwitch.missing", "category level switch cannot be null", path));
            }
        }

        return result;
    }
}
