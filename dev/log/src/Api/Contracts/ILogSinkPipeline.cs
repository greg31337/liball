using System;

namespace log;

/// <summary>
/// Defines a named sink pipeline that can compose direct sinks and nested child pipelines.
/// </summary>
public interface ILogSinkPipeline
{
    /// <summary>Returns the pipeline name.</summary>
    string GetName();
    /// <summary>Adds a concrete sink.</summary>
    void AddSink(LogSink sink);
    /// <summary>Adds an interface-based sink.</summary>
    void AddInterfaceSink(ILogSink sink);
    /// <summary>Replaces all sinks.</summary>
    void SetSinks(LogSink[] sinks);
    /// <summary>Replaces all sinks with interface-based destinations.</summary>
    void SetInterfaceSinks(ILogSink[] sinks);
    /// <summary>Returns a detached snapshot of the direct sink collection.</summary>
    LogSink[] GetSinks();
    /// <summary>Removes all direct sinks and child pipelines.</summary>
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
    /// <summary>Reports whether any destination accepts a level.</summary>
    bool IsEnabled(LogLevel level);
    /// <summary>Flushes all destinations recursively.</summary>
    LogResult Flush();
    /// <summary>Drains all destinations recursively.</summary>
    LogResult Drain();
    /// <summary>Closes all destinations recursively.</summary>
    LogResult Close();
    /// <summary>Returns a pipeline health snapshot.</summary>
    LogPipelineHealth Health();
    /// <summary>Sets the recursive shutdown timeout.</summary>
    void SetShutdownTimeout(TimeSpan timeout);
}
