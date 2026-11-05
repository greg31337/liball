namespace fake;

/// <summary>Selects the severity label of generated log-entry text.</summary>
public enum LogSeverity
{
    /// <summary>Detailed diagnostic tracing.</summary>
    Trace = 0,

    /// <summary>Developer-oriented diagnostic information.</summary>
    Debug = 1,

    /// <summary>Ordinary informational output.</summary>
    Information = 2,

    /// <summary>A condition that may require attention.</summary>
    Warning = 3,

    /// <summary>A failed operation.</summary>
    Error = 4,

    /// <summary>A severe failure requiring immediate attention.</summary>
    Critical = 5
}
