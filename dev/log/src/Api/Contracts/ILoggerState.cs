namespace log;

/// <summary>
/// Defines observation and counter maintenance for a concrete logger.
/// </summary>
public interface ILoggerState
{
    /// <summary>Returns the logger name.</summary>
    string GetName();
    /// <summary>Reports whether the logger intentionally discards events.</summary>
    bool GetIsNoOp();
    /// <summary>Reports whether the logger configuration is valid.</summary>
    bool GetIsValid();
    /// <summary>Returns the logger validation error.</summary>
    string GetErrorMessage();
    /// <summary>Returns the most recently accepted event.</summary>
    LogEvent? GetLastEvent();
    /// <summary>Returns the most recent write result.</summary>
    LogResult GetLastWriteResult();
    /// <summary>Returns the accepted-write count.</summary>
    int GetWrittenCount();
    /// <summary>Returns the dropped-event count.</summary>
    int GetDroppedCount();
    /// <summary>Returns the failure count.</summary>
    int GetFailureCount();
    /// <summary>Returns the diagnostic count.</summary>
    int GetDiagnosticCount();
    /// <summary>Returns the effective minimum level.</summary>
    LogLevel GetMinimumLevel();
    /// <summary>Clears the dropped-event count.</summary>
    void ClearDroppedCount();
    /// <summary>Clears the failure count.</summary>
    void ClearFailureCount();
    /// <summary>Clears the diagnostic count.</summary>
    void ClearDiagnosticCount();
    /// <summary>Clears all mutable counters.</summary>
    void ClearCounters();
}
