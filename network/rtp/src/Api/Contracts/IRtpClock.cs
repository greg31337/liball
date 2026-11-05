namespace rtp;

/// <summary>
/// Defines injected monotonic and wall-clock time used by RTP state machines.
/// </summary>
public interface IRtpClock
{
    /// <summary>Returns the current monotonic time in stable implementation ticks.</summary>
    /// <returns>The current monotonic timestamp.</returns>
    long GetMonotonicTimeTicks();

    /// <summary>Returns the current UTC wall-clock time in .NET ticks.</summary>
    /// <returns>The current UTC timestamp.</returns>
    long GetUtcTimeTicks();

    /// <summary>Returns the current wall-clock time as a 64-bit NTP timestamp.</summary>
    /// <returns>The current NTP timestamp.</returns>
    ulong GetNtpTimestamp();
}
