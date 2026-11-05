namespace log;

/// <summary>
/// Defines runtime inspection and mutation of a logging threshold.
/// </summary>
public interface ILogLevelSwitch
{
    /// <summary>Returns the current minimum level.</summary>
    LogLevel GetLevel();
    /// <summary>Sets the current minimum level.</summary>
    void SetLevel(LogLevel level);
    /// <summary>Reports whether an event level is enabled.</summary>
    bool IsEnabled(LogLevel level);
}
