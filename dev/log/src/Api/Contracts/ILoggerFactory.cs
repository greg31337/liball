namespace log;

/// <summary>
/// Defines direct construction of root, named, runtime-switch, and no-op loggers.
/// </summary>
public interface ILoggerFactory
{
    /// <summary>Creates an unnamed root logger with the default informational minimum level.</summary>
    Logger CreateRootLogger();
    /// <summary>Creates an unnamed root logger with a fixed minimum level.</summary>
    Logger CreateRootLoggerWithLevel(LogLevel level);
    /// <summary>Creates a named logger with the default informational minimum level.</summary>
    Logger CreateNamedLogger(string? name);
    /// <summary>Creates a named logger with a fixed minimum level.</summary>
    Logger CreateNamedLoggerWithLevel(string? name, LogLevel level);
    /// <summary>Creates an unnamed root logger sharing a runtime level switch.</summary>
    Logger CreateRootLoggerWithSwitch(LogLevelSwitch? levelSwitch);
    /// <summary>Creates a named logger sharing a runtime level switch.</summary>
    Logger CreateNamedLoggerWithSwitch(string? name, LogLevelSwitch? levelSwitch);
    /// <summary>Creates an explicit no-op logger that drops every level.</summary>
    Logger CreateNoOpLogger();
}
