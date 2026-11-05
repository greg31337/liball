using System;
using System.Collections.Generic;

namespace log;

/// <summary>
/// Builds a logger configuration from levels, sinks, filters, enrichers, and shared fields.
/// </summary>
/// <remarks>
/// Configuration methods mutate and return the same instance for chaining. CreateLogger validates the current settings and constructs a logger with copied collections.
/// </remarks>
public class LogConfiguration : ILogConfiguration
{
    internal string LoggerName = "";
    internal LogLevelSwitch ConfiguredLevelSwitch = LogLevelSwitch.Create(LogLevel.Info);
    internal ILogClock ConfiguredClock = LogClock.SystemClock();
    internal LogSink[] Sinks = new LogSink[0];
    internal LogFilter[] Filters = new LogFilter[0];
    internal LogEnricher[] Enrichers = new LogEnricher[0];
    internal LogField[] GlobalFields = new LogField[0];
    internal Dictionary<string, LogLevelSwitch> CategoryLevelSwitches = new Dictionary<string, LogLevelSwitch>();
    internal LogConfigurationDiagnostic[] LastDiagnostics = new LogConfigurationDiagnostic[0];
    internal bool IncludeRuntimeMetadata;
    internal bool IncludeActivity;

    /// <summary>
    /// Creates a configuration with an Info minimum level and no sinks.
    /// </summary>
    public LogConfiguration()
    {
        LoggerName = "";
        ConfiguredLevelSwitch = LogLevelSwitch.Create(LogLevel.Info);
        ConfiguredClock = LogClock.SystemClock();
        Sinks = new LogSink[0];
        Filters = new LogFilter[0];
        Enrichers = new LogEnricher[0];
        GlobalFields = new LogField[0];
        CategoryLevelSwitches = new Dictionary<string, LogLevelSwitch>();
        LastDiagnostics = new LogConfigurationDiagnostic[0];
        IncludeRuntimeMetadata = true;
        IncludeActivity = true;
    }

    /// <summary>
    /// Creates a configuration with an Info minimum level and no sinks.
    /// </summary>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    internal static LogConfiguration Create()
    {
        return new LogConfiguration();
    }

    /// <summary>
    /// Sets the root logger name after applying logger-name normalization.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration Name(string name)
    {
        LoggerName = LogName.NormalizeLoggerName(name);
        return this;
    }

    /// <summary>
    /// Sets a fixed minimum level for the logger created from this configuration.
    /// </summary>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration MinimumLevel(LogLevel level)
    {
        ConfiguredLevelSwitch = LogLevelSwitch.Create(level);
        return this;
    }

    /// <summary>
    /// Uses a shared level switch so the logger minimum can change at runtime.
    /// </summary>
    /// <param name="levelSwitch">The shared runtime level switch.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration LevelSwitch(LogLevelSwitch levelSwitch)
    {
        if (levelSwitch == null)
        {
            ConfiguredLevelSwitch = LogLevelSwitch.Create(LogLevel.Info);
        }
        else
        {
            ConfiguredLevelSwitch = levelSwitch;
        }

        return this;
    }

    /// <summary>
    /// Uses an injected UTC time source for event timestamps.
    /// </summary>
    /// <param name="clock">The UTC time source.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration Clock(ILogClock clock)
    {
        if (clock == null)
        {
            ConfiguredClock = LogClock.SystemClock();
        }
        else
        {
            ConfiguredClock = clock;
        }

        return this;
    }

    /// <summary>
    /// Adds sink to this log configuration.
    /// </summary>
    /// <param name="sink">The sink to inspect or configure.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration AddSink(LogSink sink)
    {
        Sinks = LogPipeline.AppendSink(Sinks, sink);
        return this;
    }

    /// <summary>
    /// Adds an interface-based sink to this configuration.
    /// </summary>
    /// <param name="sink">The destination to add.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration AddInterfaceSink(ILogSink sink)
    {
        return AddSink(LogSink.Wrap(sink));
    }

    /// <summary>
    /// Sets sinks for this log configuration.
    /// </summary>
    /// <param name="sinks">The sinks to copy into the destination.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration SetSinks(LogSink[] sinks)
    {
        Sinks = LogPipeline.CopySinks(sinks);
        return this;
    }

    /// <summary>
    /// Replaces the configured sinks with interface-based destinations.
    /// </summary>
    /// <param name="sinks">The destinations to copy into this configuration.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration SetInterfaceSinks(ILogSink[] sinks)
    {
        ClearSinks();
        if (sinks == null)
        {
            return this;
        }

        foreach (ILogSink sink in sinks)
        {
            AddInterfaceSink(sink);
        }

        return this;
    }

    /// <summary>
    /// Clears sinks and restores the default behavior.
    /// </summary>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration ClearSinks()
    {
        Sinks = new LogSink[0];
        return this;
    }

    /// <summary>
    /// Adds filter to this log configuration.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration AddFilter(LogFilter filter)
    {
        Filters = LogFilterPipeline.AppendFilter(Filters, filter);
        return this;
    }

    /// <summary>
    /// Adds an interface-based filter to this configuration.
    /// </summary>
    /// <param name="filter">The filter to add.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration AddInterfaceFilter(ILogFilter filter)
    {
        return AddFilter(LogFilter.Wrap(filter));
    }

    /// <summary>
    /// Sets filters for this log configuration.
    /// </summary>
    /// <param name="filters">The filters to copy into the destination.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration SetFilters(LogFilter[] filters)
    {
        Filters = LogFilterPipeline.CopyFilters(filters);
        return this;
    }

    /// <summary>
    /// Replaces configured filters with interface-based rules.
    /// </summary>
    /// <param name="filters">The filters to copy into this configuration.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration SetInterfaceFilters(ILogFilter[] filters)
    {
        ClearFilters();
        if (filters == null)
        {
            return this;
        }

        foreach (ILogFilter filter in filters)
        {
            AddInterfaceFilter(filter);
        }

        return this;
    }

    /// <summary>
    /// Clears filters and restores the default behavior.
    /// </summary>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration ClearFilters()
    {
        Filters = new LogFilter[0];
        return this;
    }

    /// <summary>
    /// Adds enricher to this log configuration.
    /// </summary>
    /// <param name="enricher">The enricher to add.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    internal LogConfiguration AddEnricher(LogEnricher enricher)
    {
        Enrichers = LogEnricher.AppendEnricher(Enrichers, enricher);
        return this;
    }

    /// <summary>
    /// Adds an interface-based dynamic field provider to this configuration.
    /// </summary>
    /// <param name="enricher">The enricher to add.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration AddInterfaceEnricher(ILogEnricher enricher)
    {
        return AddEnricher(LogEnricher.Wrap(enricher));
    }

    /// <summary>
    /// Sets enrichers for this log configuration.
    /// </summary>
    /// <param name="enrichers">The enrichers to copy into the destination.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    internal LogConfiguration SetEnrichers(LogEnricher[] enrichers)
    {
        Enrichers = LogEnricher.CopyEnrichers(enrichers);
        return this;
    }

    /// <summary>
    /// Replaces configured enrichers with interface-based dynamic field providers.
    /// </summary>
    /// <param name="enrichers">The enrichers to copy into this configuration.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration SetInterfaceEnrichers(ILogEnricher[] enrichers)
    {
        ClearEnrichers();
        if (enrichers == null)
        {
            return this;
        }

        foreach (ILogEnricher enricher in enrichers)
        {
            AddInterfaceEnricher(enricher);
        }

        return this;
    }

    /// <summary>
    /// Clears enrichers and restores the default behavior.
    /// </summary>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration ClearEnrichers()
    {
        Enrichers = new LogEnricher[0];
        return this;
    }

    /// <summary>
    /// Adds global field to this log configuration.
    /// </summary>
    /// <param name="name">The name to normalize or assign.</param>
    /// <param name="value">The value to normalize or compare.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration AddGlobalField(string name, object value)
    {
        return AddGlobalLogField(LogField.ValueField(name, LogTemplateBinder.ToLogValue(value)));
    }

    /// <summary>
    /// Adds global log field to this log configuration.
    /// </summary>
    /// <param name="field">The structured field to add.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration AddGlobalLogField(LogField field)
    {
        GlobalFields = LogEventBuilder.AppendField(GlobalFields, field);
        return this;
    }

    /// <summary>
    /// Sets global fields for this log configuration.
    /// </summary>
    /// <param name="fields">The structured fields to copy or process.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration SetGlobalFields(LogField[] fields)
    {
        GlobalFields = Logger.CopyFields(fields);
        return this;
    }

    /// <summary>
    /// Clears global fields and restores the default behavior.
    /// </summary>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration ClearGlobalFields()
    {
        GlobalFields = new LogField[0];
        return this;
    }

    /// <summary>
    /// Enables or disables thread, process, machine, and application enrichment.
    /// </summary>
    /// <param name="includeRuntimeMetadata">The include runtime metadata value.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration RuntimeMetadata(bool includeRuntimeMetadata)
    {
        IncludeRuntimeMetadata = includeRuntimeMetadata;
        return this;
    }

    /// <summary>
    /// Enables or disables capture of the current System.Diagnostics.Activity identifiers.
    /// </summary>
    /// <param name="includeActivity">The include activity value.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration Activity(bool includeActivity)
    {
        IncludeActivity = includeActivity;
        return this;
    }

    /// <summary>
    /// Assigns a minimum level to a logger category and its descendants.
    /// </summary>
    /// <param name="loggerName">The dotted logger or category name.</param>
    /// <param name="level">The minimum or event level.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration CategoryLevel(string loggerName, LogLevel level)
    {
        string normalizedLoggerName = LogName.NormalizeLoggerName(loggerName);
        string validationError = LogName.ValidateLoggerName(normalizedLoggerName);
        if (validationError.Length > 0)
        {
            return this;
        }

        CategoryLevelSwitches[normalizedLoggerName] = LogLevelSwitch.Create(level);
        return this;
    }

    /// <summary>
    /// Returns all validation diagnostics for the current settings.
    /// </summary>
    /// <returns>A newly allocated array containing every validation diagnostic.</returns>
    public LogConfigurationDiagnostic[] Validate()
    {
        LastDiagnostics = LogConfigurationValidator.Validate(this);
        return LogConfigurationDiagnostic.CopyDiagnostics(LastDiagnostics);
    }

    /// <summary>
    /// Summarizes validation diagnostics as one logging result.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    public LogResult ValidateResult()
    {
        LastDiagnostics = LogConfigurationValidator.Validate(this);
        return LogConfigurationValidator.ToResult(LastDiagnostics);
    }

    /// <summary>
    /// Reports whether validation finds no errors.
    /// </summary>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    public bool IsValid()
    {
        LastDiagnostics = LogConfigurationValidator.Validate(this);
        return !LogConfigurationValidator.HasErrors(LastDiagnostics);
    }

    /// <summary>
    /// Applies supported LOG_ environment values to this configuration.
    /// </summary>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration ApplyEnvironment()
    {
        return LogEnvironmentConfiguration.Apply(this);
    }

    /// <summary>
    /// Applies supported LOG_ environment values to this configuration.
    /// </summary>
    /// <param name="environment">The environment values to apply instead of reading the current process.</param>
    /// <returns>The populated configuration, or this configuration for fluent mutation methods.</returns>
    public LogConfiguration ApplyEnvironmentValues(Dictionary<string, string> environment)
    {
        return LogEnvironmentConfiguration.Apply(this, environment);
    }

    /// <summary>
    /// Validates the configuration and creates a logger from copied settings.
    /// </summary>
    /// <returns>The selected, created, or installed logger.</returns>
    public Logger CreateLogger()
    {
        LastDiagnostics = LogConfigurationValidator.Validate(this);
        Logger logger = new Logger(LoggerName, ConfiguredLevelSwitch, false, ConfiguredClock);
        logger.SetSinks(Sinks);
        logger.SetFilters(Filters);
        logger.IncludeRuntimeMetadata = IncludeRuntimeMetadata;
        logger.BuiltInEnricher.IncludeRuntimeMetadata = IncludeRuntimeMetadata;
        logger.ActivityEnricher.IsEnabled = IncludeActivity;
        logger.BuiltInEnricher.SetStaticFields(GlobalFields);
        foreach (LogEnricher enricher in Enrichers)
        {
            if (enricher != null)
            {
                enricher.Apply(logger);
            }
        }

        foreach (KeyValuePair<string, LogLevelSwitch> entry in CategoryLevelSwitches)
        {
            logger.CategoryLevelSwitches[entry.Key] = entry.Value;
        }

        return logger;
    }
}
