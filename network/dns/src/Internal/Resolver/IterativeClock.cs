namespace dns;

internal interface IDnsIterativeClock
{
    DnsClockValue ReadClock();
}

internal class DnsIterativeClock : IDnsIterativeClock
{
    internal DnsIterativeClock()
    {
    }

    public DnsClockValue ReadClock()
    {
        return TransportTime.ReadClock();
    }
}
