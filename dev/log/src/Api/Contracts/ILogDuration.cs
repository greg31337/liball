using System;

namespace log;

/// <summary>
/// Defines observation and idempotent completion of one logger-bound duration measurement.
/// </summary>
public interface ILogDuration
{
    /// <summary>Completes the measurement and emits its event once.</summary>
    LogResult Complete();
    /// <summary>Reports whether completion has occurred.</summary>
    bool GetIsCompleted();
    /// <summary>Returns the measured duration, or zero before completion.</summary>
    TimeSpan GetElapsed();
    /// <summary>Returns the result from completion.</summary>
    LogResult GetResult();
    /// <summary>Returns the structured duration field name.</summary>
    string GetFieldName();
}
