using System;
using System.Diagnostics;

namespace rtp;

/// <summary>
/// Provides monotonic elapsed time and UTC-derived NTP timestamps from the system clock.
/// </summary>
public class SystemRtpClock : IRtpClock
{
    internal const long NtpEpochTicks = RtpNtpTime.NtpEpochTicks;
    internal const long TimeSpanTicksPerSecond = RtpNtpTime.TimeSpanTicksPerSecond;
    internal const ulong NtpFractionScale = RtpNtpTime.NtpFractionScale;

    internal long monotonicOrigin;

    /// <summary>
    /// Initializes a clock with a new monotonic elapsed-time origin.
    /// </summary>
    public SystemRtpClock()
    {
        monotonicOrigin = Stopwatch.GetTimestamp();
    }

    /// <summary>Returns elapsed monotonic time since construction in .NET ticks.</summary>
    /// <returns>The non-wall-clock elapsed timestamp.</returns>
    public long GetMonotonicTimeTicks()
    {
        return Stopwatch.GetElapsedTime(monotonicOrigin).Ticks;
    }

    /// <summary>Returns the current UTC wall-clock time in .NET ticks.</summary>
    /// <returns>The current UTC timestamp.</returns>
    public long GetUtcTimeTicks()
    {
        return DateTime.UtcNow.Ticks;
    }

    /// <summary>Returns the current UTC wall-clock time as an NTP timestamp.</summary>
    /// <returns>The current 32-bit-era NTP seconds and fractional seconds.</returns>
    public ulong GetNtpTimestamp()
    {
        return RtpNtpTime.FromUtcTicks(DateTime.UtcNow.Ticks);
    }
}
