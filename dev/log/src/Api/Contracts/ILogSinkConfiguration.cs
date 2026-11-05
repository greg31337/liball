namespace log;

/// <summary>
/// Defines filtering and routing configuration for a concrete sink.
/// </summary>
public interface ILogSinkConfiguration
{
    /// <summary>Sets the sink minimum level.</summary>
    void SetMinimumLevel(LogLevel level);
    /// <summary>Clears the sink minimum level.</summary>
    void ClearMinimumLevel();
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
    /// <summary>Clears the configured child pipeline.</summary>
    void ClearSubPipeline();
    /// <summary>Reports whether the child pipeline accepts a level.</summary>
    bool IsSubPipelineEnabled(LogLevel level);
}
