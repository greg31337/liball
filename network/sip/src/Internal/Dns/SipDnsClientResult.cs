namespace sip;

internal class SipDnsClientResult
{
    internal SipResolutionStatus Status;
    internal SipDnsWireResult? Response;
    internal string? ErrorMessage;
    internal string? NameServer;
    internal int Attempts;
    internal int CnameDepth;
    internal bool UsedTcp;
}
