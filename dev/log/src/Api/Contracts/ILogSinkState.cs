namespace log;

/// <summary>
/// Defines observation and counter maintenance for a concrete sink.
/// </summary>
public interface ILogSinkState
{
    /// <summary>Reports whether the sink is enabled.</summary>
    bool GetIsEnabled();
    /// <summary>Returns the most recently accepted event.</summary>
    LogEvent? GetLastEvent();
    /// <summary>Returns the accepted-write count.</summary>
    int GetWrittenCount();
    /// <summary>Returns the dropped-event count.</summary>
    int GetDroppedCount();
    /// <summary>Returns the failure count.</summary>
    int GetFailureCount();
    /// <summary>Returns the diagnostic count.</summary>
    int GetDiagnosticCount();
    /// <summary>Returns the most recent sink result.</summary>
    LogResult GetLastResult();
    /// <summary>Reports whether the sink is closed.</summary>
    bool GetIsClosed();
    /// <summary>Clears the dropped-event count.</summary>
    void ClearDroppedCount();
    /// <summary>Clears the failure count.</summary>
    void ClearFailureCount();
    /// <summary>Clears the diagnostic count.</summary>
    void ClearDiagnosticCount();
    /// <summary>Clears all mutable counters.</summary>
    void ClearCounters();
}
