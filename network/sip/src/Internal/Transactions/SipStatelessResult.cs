namespace sip;

internal class SipStatelessResult
{
    internal bool Success;
    internal bool Forwarded;
    internal bool DeliveredLocally;
    internal bool Malformed;
    internal bool Rejected;
    internal int StatusCode;
    internal string? ErrorMessage;
    internal SipMessage[] OutboundMessages = new SipMessage[0];
    internal SipMessage[] DeliveredMessages = new SipMessage[0];
}
