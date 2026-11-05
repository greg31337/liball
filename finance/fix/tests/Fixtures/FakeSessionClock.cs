using System;
using fix;

namespace fix.Tests;

internal class FakeSessionClock : ISessionClock
{
    internal DateTime CurrentUtcTime;
    internal int ReadCount;

    internal FakeSessionClock(DateTime currentUtcTime)
    {
        CurrentUtcTime = currentUtcTime;
    }

    public DateTime GetUtcNow()
    {
        ReadCount++;
        return CurrentUtcTime;
    }

    internal void SetUtcNow(DateTime currentUtcTime)
    {
        CurrentUtcTime = currentUtcTime;
    }

    internal void AdvanceClock(TimeSpan elapsed)
    {
        CurrentUtcTime = CurrentUtcTime.Add(elapsed);
    }
}
