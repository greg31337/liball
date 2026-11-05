using System;

namespace log;

/// <summary>
/// Composes named sink branches that can be installed in a logger or another sink pipeline.
/// </summary>
/// <remarks>
/// Installing a child returns its routing sink. Configure that sink's minimum level and filters to apply rules before events enter the child pipeline.
/// </remarks>
public class LogSinkPipeline : ILogSinkPipeline
{
    internal string Name = "";
    internal LogPipeline Pipeline = new LogPipeline();

    internal LogSinkPipeline(string name)
    {
        Name = LogResult.NormalizeText(name);
        Pipeline = new LogPipeline();
    }

    /// <summary>
    /// Returns the stable pipeline name used by its routing sink.
    /// </summary>
    /// <returns>The normalized pipeline name.</returns>
    public string GetName()
    {
        return Name;
    }

    /// <summary>
    /// Adds a concrete destination to this pipeline.
    /// </summary>
    /// <param name="sink">The destination to add.</param>
    public void AddSink(LogSink sink)
    {
        Pipeline.AddSink(sink);
    }

    /// <summary>
    /// Adds an interface-based destination to this pipeline.
    /// </summary>
    /// <param name="sink">The destination to add.</param>
    public void AddInterfaceSink(ILogSink sink)
    {
        Pipeline.AddInterfaceSink(sink);
    }

    /// <summary>
    /// Replaces every direct sink and child routing sink in this pipeline.
    /// </summary>
    /// <param name="sinks">The sinks to copy into this pipeline.</param>
    public void SetSinks(LogSink[] sinks)
    {
        Pipeline.SetSinks(sinks);
    }

    /// <summary>
    /// Replaces every sink with interface-based destinations.
    /// </summary>
    /// <param name="sinks">The destinations to copy into this pipeline.</param>
    public void SetInterfaceSinks(ILogSink[] sinks)
    {
        Pipeline.SetInterfaceSinks(sinks);
    }

    /// <summary>
    /// Returns a detached snapshot of this pipeline's direct sink collection.
    /// </summary>
    /// <returns>A copied sink array.</returns>
    public LogSink[] GetSinks()
    {
        return LogPipeline.CopySinks(Pipeline.Sinks);
    }

    /// <summary>
    /// Removes every direct sink and child pipeline.
    /// </summary>
    public void ClearSinks()
    {
        Pipeline.ClearSinks();
    }

    /// <summary>
    /// Installs a child pipeline and returns its configurable routing sink.
    /// </summary>
    /// <param name="pipeline">The child pipeline to install.</param>
    /// <returns>The routing sink, or a disabled sink carrying a failure result when the child is null or recursive.</returns>
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
    /// Reports whether any nested destination accepts the specified level.
    /// </summary>
    /// <param name="level">The level to inspect.</param>
    /// <returns>true when at least one destination accepts the level; otherwise, false.</returns>
    public bool IsEnabled(LogLevel level)
    {
        return Pipeline.IsEnabled(level);
    }

    /// <summary>
    /// Flushes every nested destination recursively.
    /// </summary>
    /// <returns>The combined lifecycle result.</returns>
    public LogResult Flush()
    {
        return Pipeline.Flush();
    }

    /// <summary>
    /// Drains every nested destination recursively.
    /// </summary>
    /// <returns>The combined lifecycle result.</returns>
    public LogResult Drain()
    {
        return Pipeline.Drain();
    }

    /// <summary>
    /// Drains and closes every nested destination recursively.
    /// </summary>
    /// <returns>The combined lifecycle result.</returns>
    public LogResult Close()
    {
        return Pipeline.Close();
    }

    /// <summary>
    /// Returns a point-in-time health snapshot for this pipeline.
    /// </summary>
    /// <returns>A point-in-time pipeline health snapshot.</returns>
    public LogPipelineHealth Health()
    {
        LogPipelineHealth health = Pipeline.Health();
        health.Name = Name;
        return health;
    }

    /// <summary>
    /// Sets the shutdown timeout on every current nested destination.
    /// </summary>
    /// <param name="timeout">The maximum time allowed for draining or closing.</param>
    public void SetShutdownTimeout(TimeSpan timeout)
    {
        Pipeline.SetShutdownTimeout(timeout);
    }

    internal static LogSink InstallInto(LogPipeline destination, LogSinkPipeline pipeline)
    {
        LogSink routingSink = LogSink.Named("");
        if (destination == null)
        {
            routingSink.IsEnabled = false;
            routingSink.CompleteWrite(LogResult.FailureCode("pipeline.destination.null", "destination pipeline cannot be null"));
            return routingSink;
        }

        if (pipeline == null)
        {
            routingSink.IsEnabled = false;
            routingSink.CompleteWrite(LogResult.FailureCode("pipeline.child.null", "child pipeline cannot be null"));
            return routingSink;
        }

        if (pipeline.Pipeline == destination || LogSinkPipeline.ContainsPipeline(pipeline.Pipeline, destination))
        {
            routingSink.Name = pipeline.Name;
            routingSink.IsEnabled = false;
            routingSink.CompleteWrite(LogResult.FailureCode("pipeline.child.recursive", "child pipeline cannot contain its destination"));
            return routingSink;
        }

        routingSink = LogSink.PipelineSink(pipeline.Name, pipeline.Pipeline);
        routingSink.PublicSubPipeline = pipeline;
        destination.AddSink(routingSink);
        return routingSink;
    }

    internal static void ReplaceIn(LogPipeline destination, LogSinkPipeline[] pipelines)
    {
        if (destination == null)
        {
            return;
        }

        LogSinkPipeline.RemoveFrom(destination);
        if (pipelines == null)
        {
            return;
        }

        foreach (LogSinkPipeline pipeline in pipelines)
        {
            LogSinkPipeline.InstallInto(destination, pipeline);
        }
    }

    internal static LogSinkPipeline[] InspectPipelines(LogPipeline pipeline)
    {
        int count = LogSinkPipeline.CountPublicSubPipelines(pipeline);
        LogSinkPipeline[] pipelines = new LogSinkPipeline[count];
        if (pipeline == null)
        {
            return pipelines;
        }

        int index = 0;
        foreach (LogSink sink in pipeline.Sinks)
        {
            if (sink != null && sink.PublicSubPipeline != null)
            {
                pipelines[index] = sink.PublicSubPipeline;
                index = index + 1;
            }
        }

        return pipelines;
    }

    internal static LogSink[] InspectRoutingSinks(LogPipeline pipeline)
    {
        int count = LogSinkPipeline.CountPublicSubPipelines(pipeline);
        LogSink[] sinks = new LogSink[count];
        if (pipeline == null)
        {
            return sinks;
        }

        int index = 0;
        foreach (LogSink sink in pipeline.Sinks)
        {
            if (sink != null && sink.PublicSubPipeline != null)
            {
                sinks[index] = sink;
                index = index + 1;
            }
        }

        return sinks;
    }

    internal static void RemoveFrom(LogPipeline pipeline)
    {
        if (pipeline == null)
        {
            return;
        }

        LogSink[] retained = new LogSink[0];
        foreach (LogSink sink in pipeline.Sinks)
        {
            if (sink != null && sink.PublicSubPipeline == null)
            {
                retained = LogPipeline.AppendSink(retained, sink);
            }
        }

        pipeline.SetSinks(retained);
    }

    internal static int CountPublicSubPipelines(LogPipeline pipeline)
    {
        if (pipeline == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LogSink sink in pipeline.Sinks)
        {
            if (sink != null && sink.PublicSubPipeline != null)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static bool ContainsPipeline(LogPipeline root, LogPipeline expected)
    {
        if (root == null || expected == null)
        {
            return false;
        }

        if (root == expected)
        {
            return true;
        }

        foreach (LogSink sink in root.Sinks)
        {
            if (sink != null && sink.PublicSubPipeline != null && LogSinkPipeline.ContainsPipeline(sink.PublicSubPipeline.Pipeline, expected))
            {
                return true;
            }
        }

        return false;
    }
}
