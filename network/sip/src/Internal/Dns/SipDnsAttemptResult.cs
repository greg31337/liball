namespace sip;

internal class SipDnsAttemptResult
{
    internal bool Completed;
    internal bool TimedOut;
    internal bool UsedTcp;
    internal SipDnsWireResult? Response;
    internal string? ErrorMessage;
}
