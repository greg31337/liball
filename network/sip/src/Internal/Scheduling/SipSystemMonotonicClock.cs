namespace sip;

internal class SipSystemMonotonicClock : ISipMonotonicClock
{
    public long GetMilliseconds()
    {
        return System.Environment.TickCount64;
    }
}
