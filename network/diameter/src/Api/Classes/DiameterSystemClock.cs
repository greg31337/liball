using System;

namespace diameter;

/// <summary>
/// Provides system UTC and monotonic time to Diameter components.
/// </summary>
public class DiameterSystemClock : IDiameterClock
{
    /// <summary>Initializes a system clock.</summary>
    public DiameterSystemClock()
    {
    }

    /// <summary>
    /// Reads the current Coordinated Universal Time.
    /// </summary>
    /// <returns>The current UTC time.</returns>
    public DateTimeOffset ReadUtcTime()
    {
        return DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Reads elapsed system time in milliseconds.
    /// </summary>
    /// <returns>A monotonic millisecond value whose differences represent elapsed time.</returns>
    public long ReadMonotonicMilliseconds()
    {
        return Environment.TickCount64;
    }
}
