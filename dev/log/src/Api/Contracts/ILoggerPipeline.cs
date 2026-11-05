using System;

namespace log;

/// <summary>
/// Defines logger enrichment, filtering, sink composition, and lifecycle operations.
/// </summary>
public interface ILoggerPipeline
{
    /// <summary>Adds an enricher.</summary>
    void AddEnricher(ILogEnricher enricher);
    /// <summary>Replaces the enrichers.</summary>
    void SetEnrichers(ILogEnricher[] enrichers);
    /// <summary>Removes all enrichers.</summary>
    void ClearEnrichers();
    /// <summary>Adds a concrete filter.</summary>
    void AddFilter(LogFilter filter);
    /// <summary>Adds an interface-based filter.</summary>
    void AddInterfaceFilter(ILogFilter filter);
    /// <summary>Replaces the concrete filters.</summary>
    void SetFilters(LogFilter[] filters);
    /// <summary>Replaces the interface-based filters.</summary>
    void SetInterfaceFilters(ILogFilter[] filters);
    /// <summary>Removes all filters.</summary>
    void ClearFilters();
    /// <summary>Adds a concrete sink.</summary>
    void AddSink(LogSink sink);
    /// <summary>Adds an interface-based sink.</summary>
    void AddInterfaceSink(ILogSink sink);
    /// <summary>Replaces the concrete sinks.</summary>
    void SetSinks(LogSink[] sinks);
    /// <summary>Replaces the interface-based sinks.</summary>
    void SetInterfaceSinks(ILogSink[] sinks);
    /// <summary>Removes all sinks.</summary>
    void ClearSinks();
    /// <summary>Installs a child pipeline and returns its configurable routing sink.</summary>
    LogSink InstallSubPipeline(LogSinkPipeline pipeline);
    /// <summary>Replaces child pipelines while preserving direct sinks.</summary>
    void SetSubPipelines(LogSinkPipeline[] pipelines);
    /// <summary>Returns a detached snapshot of installed child pipelines.</summary>
    LogSinkPipeline[] GetSubPipelines();
    /// <summary>Returns the configurable routing sinks for installed child pipelines.</summary>
    LogSink[] GetSubPipelineSinks();
    /// <summary>Removes child pipelines while preserving direct sinks.</summary>
    void ClearSubPipelines();
    /// <summary>Flushes all sinks.</summary>
    LogResult Flush();
    /// <summary>Drains all sinks.</summary>
    LogResult Drain();
    /// <summary>Closes all sinks.</summary>
    LogResult Close();
    /// <summary>Returns a pipeline health snapshot.</summary>
    LogPipelineHealth Health();
    /// <summary>Sets the shutdown timeout.</summary>
    void SetShutdownTimeout(TimeSpan timeout);
    /// <summary>Sets a common sink minimum level.</summary>
    void SetSinkMinimumLevel(LogLevel level);
    /// <summary>Clears the common sink minimum level.</summary>
    void ClearSinkMinimumLevel();
    /// <summary>Sets a category minimum level.</summary>
    LogResult SetCategoryLevel(string loggerName, LogLevel level);
}
