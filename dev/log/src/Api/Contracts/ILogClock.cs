using System;

namespace log;

/// <summary>
/// Defines the UTC time source used for event timestamps and time-based filters.
/// </summary>
public interface ILogClock
{
    /// <summary>
    /// Returns the current UTC time.
    /// </summary>
    /// <returns>The current time with its UTC offset.</returns>
    DateTimeOffset UtcNow();
}
