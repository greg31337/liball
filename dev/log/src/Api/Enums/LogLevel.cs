namespace log;

/// <summary>
/// Specifies the severity of a log event and the ordering used by level checks.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Fine-grained diagnostic information.
    /// </summary>
    Trace = 0,
    /// <summary>
    /// Diagnostic information useful during development or investigation.
    /// </summary>
    Debug = 1,
    /// <summary>
    /// Normal application progress and state changes.
    /// </summary>
    Info = 2,
    /// <summary>
    /// An unexpected condition that does not stop the current operation.
    /// </summary>
    Warn = 3,
    /// <summary>
    /// A failure in the current operation.
    /// </summary>
    Error = 4,
    /// <summary>
    /// A severe failure that may require the application to stop.
    /// </summary>
    Fatal = 5,
    /// <summary>
    /// Disables logging when used as a minimum level.
    /// </summary>
    Off = 6
}
