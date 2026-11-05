namespace sip;

internal class SipTransactionLayerClock : ISipMonotonicClock
{
    long milliseconds;

    internal SipTransactionLayerClock(long initialMilliseconds)
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

    internal void SetMilliseconds(long value)
    {
        if (value < milliseconds)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
        milliseconds = value;
    }
}
