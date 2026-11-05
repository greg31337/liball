namespace sip;

internal class SipDnsWireResult
{
    internal bool Success;
    internal bool Unrelated;
    internal bool Truncated;
    internal bool Authoritative;
    internal bool RecursionAvailable;
    internal int ResponseCode = -1;
    internal SipDnsWireRecord[] Records = new SipDnsWireRecord[0];
    internal string? ErrorMessage;
    internal int ErrorOffset;
}
