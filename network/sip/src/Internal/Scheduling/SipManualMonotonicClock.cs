namespace sip;

internal class SipManualMonotonicClock : ISipMonotonicClock
{
    long milliseconds;

    internal SipManualMonotonicClock(long initialMilliseconds)
    {
        if (initialMilliseconds < 0)
        {
            throw new System.ArgumentOutOfRangeException("initialMilliseconds");
        }

        milliseconds = initialMilliseconds;
    }

    public long GetMilliseconds()
    {
        return milliseconds;
    }

    internal void AdvanceMilliseconds(long elapsedMilliseconds)
    {
        if (elapsedMilliseconds < 0)
        {
            throw new System.ArgumentOutOfRangeException("elapsedMilliseconds");
        }

        if (elapsedMilliseconds > long.MaxValue - milliseconds)
        {
            throw new System.ArgumentOutOfRangeException("elapsedMilliseconds");
        }

        milliseconds = milliseconds + elapsedMilliseconds;
    }
}
