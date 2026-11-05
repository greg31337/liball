using System;

namespace log;

internal class LogClock : ILogClock
{
    internal bool IsFixed;
    internal DateTimeOffset FixedUtcNow;

    internal LogClock()
    {
        IsFixed = false;
        FixedUtcNow = DateTimeOffset.MinValue;
    }

    internal static LogClock SystemClock()
    {
        return new LogClock();
    }

    internal static LogClock Fixed(DateTimeOffset fixedUtcNow)
    {
        LogClock clock = new LogClock();
        clock.SetFixedUtcNow(fixedUtcNow);
        return clock;
    }

    public DateTimeOffset UtcNow()
    {
        if (IsFixed)
        {
            return FixedUtcNow;
        }

        return DateTimeOffset.UtcNow;
    }

    internal void SetFixedUtcNow(DateTimeOffset fixedUtcNow)
    {
        IsFixed = true;
        FixedUtcNow = fixedUtcNow;
    }

    internal void UseSystemUtcNow()
    {
        IsFixed = false;
        FixedUtcNow = DateTimeOffset.MinValue;
    }
}
