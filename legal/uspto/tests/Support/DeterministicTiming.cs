using uspto;

namespace uspto.Tests;

internal class FakeTestClock : IRateLimitClock
{
    internal DateTimeOffset Current;

    internal FakeTestClock(DateTimeOffset current)
    {
        Current = current;
    }

    public DateTimeOffset GetUtcNow()
    {
        return Current;
    }

    internal void AdvanceMilliseconds(int milliseconds)
    {
        Current = Current.AddMilliseconds(milliseconds);
    }
}

internal class AdvancingRateLimitWait : IRateLimitWait
{
    internal FakeTestClock Clock;
    internal List<int> Waits = new List<int>();

    internal AdvancingRateLimitWait(FakeTestClock clock)
    {
        Clock = clock;
    }

    public void Wait(object sync, int milliseconds)
    {
        Waits.Add(milliseconds);
        Clock.AdvanceMilliseconds(milliseconds);
    }
}

internal class RecordingRetryDelay : IRetryDelay
{
    internal List<long> Waits = new List<long>();

    public void Wait(long milliseconds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Waits.Add(milliseconds);
    }
}

internal class FixedRetryRandom : IRetryRandom
{
    internal double Value;

    internal FixedRetryRandom(double value)
    {
        Value = value;
    }

    public double NextUnit()
    {
        return Value;
    }
}
