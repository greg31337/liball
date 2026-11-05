using System.Collections.Generic;

namespace log;

/// <summary>
/// Defines fluent logger configuration, validation, and construction operations.
/// </summary>
public interface ILogConfiguration
{
    /// <summary>Sets the root logger name.</summary>
    LogConfiguration Name(string name);
    /// <summary>Sets the fixed minimum level.</summary>
    LogConfiguration MinimumLevel(LogLevel level);
    /// <summary>Uses a shared runtime level switch.</summary>
    LogConfiguration LevelSwitch(LogLevelSwitch levelSwitch);
    /// <summary>Uses the supplied UTC clock.</summary>
    LogConfiguration Clock(ILogClock clock);
    /// <summary>Adds a concrete sink.</summary>
    LogConfiguration AddSink(LogSink sink);
    /// <summary>Adds an interface-based sink.</summary>
    LogConfiguration AddInterfaceSink(ILogSink sink);
    /// <summary>Replaces the concrete sinks.</summary>
    LogConfiguration SetSinks(LogSink[] sinks);
    /// <summary>Replaces the interface-based sinks.</summary>
    LogConfiguration SetInterfaceSinks(ILogSink[] sinks);
    /// <summary>Removes all sinks.</summary>
    LogConfiguration ClearSinks();
    /// <summary>Adds a concrete filter.</summary>
    LogConfiguration AddFilter(LogFilter filter);
    /// <summary>Adds an interface-based filter.</summary>
    LogConfiguration AddInterfaceFilter(ILogFilter filter);
    /// <summary>Replaces the concrete filters.</summary>
    LogConfiguration SetFilters(LogFilter[] filters);
    /// <summary>Replaces the interface-based filters.</summary>
    LogConfiguration SetInterfaceFilters(ILogFilter[] filters);
    /// <summary>Removes all filters.</summary>
    LogConfiguration ClearFilters();
    /// <summary>Adds an interface-based enricher.</summary>
    LogConfiguration AddInterfaceEnricher(ILogEnricher enricher);
    /// <summary>Replaces the interface-based enrichers.</summary>
    LogConfiguration SetInterfaceEnrichers(ILogEnricher[] enrichers);
    /// <summary>Removes all enrichers.</summary>
    LogConfiguration ClearEnrichers();
    /// <summary>Adds a normalized global field.</summary>
    LogConfiguration AddGlobalField(string name, object value);
    /// <summary>Adds an existing global field.</summary>
    LogConfiguration AddGlobalLogField(LogField field);
    /// <summary>Replaces the global fields.</summary>
    LogConfiguration SetGlobalFields(LogField[] fields);
    /// <summary>Removes all global fields.</summary>
    LogConfiguration ClearGlobalFields();
    /// <summary>Controls runtime metadata enrichment.</summary>
    LogConfiguration RuntimeMetadata(bool includeRuntimeMetadata);
    /// <summary>Controls activity enrichment.</summary>
    LogConfiguration Activity(bool includeActivity);
    /// <summary>Sets the minimum level for a logger category.</summary>
    LogConfiguration CategoryLevel(string loggerName, LogLevel level);
    /// <summary>Returns configuration diagnostics.</summary>
    LogConfigurationDiagnostic[] Validate();
    /// <summary>Returns validation as a logging result.</summary>
    LogResult ValidateResult();
    /// <summary>Reports whether the configuration is valid.</summary>
    bool IsValid();
    /// <summary>Applies process environment settings.</summary>
    LogConfiguration ApplyEnvironment();
    /// <summary>Applies explicit environment settings.</summary>
    LogConfiguration ApplyEnvironmentValues(Dictionary<string, string> environment);
    /// <summary>Creates a logger from this configuration.</summary>
    Logger CreateLogger();
}
