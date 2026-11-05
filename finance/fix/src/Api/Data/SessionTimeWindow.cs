using System;

namespace fix;

/// <summary>Defines one active weekly session interval.</summary>
public class SessionTimeWindow
{
    /// <summary>The local start day.</summary>
    public DayOfWeek StartDay;
    /// <summary>The local start time.</summary>
    public TimeSpan StartTime;
    /// <summary>The local end day.</summary>
    public DayOfWeek EndDay;
    /// <summary>The local end time.</summary>
    public TimeSpan EndTime;
    /// <summary>Whether this interval is enabled.</summary>
    public bool Enabled = true;
}
