namespace sip;

internal class SipViaParseResult
{
    internal bool Success;
    internal SipViaValue[] Values = new SipViaValue[0];
    internal int ErrorOffset = -1;
    internal string? ErrorMessage;
}

internal class SipViaKnownParameterState
{
    internal bool Branch;
    internal bool Received;
    internal bool RPort;
    internal bool MAddr;
    internal bool Ttl;
    internal bool Alias;
}
