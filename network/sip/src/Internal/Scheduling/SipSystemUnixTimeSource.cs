namespace sip;

internal class SipSystemUnixTimeSource : ISipUnixTimeSource
{
    public long GetUnixTimeSeconds()
    {
        return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
