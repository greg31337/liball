namespace sip;

internal class SipNonInviteServerResult
{
    internal SipTransactionState State;
    internal bool StateChanged;
    internal bool TransportFailed;
    internal bool Retransmitted;
    internal bool RequestAbsorbed;
    internal bool ResponseDiscarded;
    internal string? ErrorMessage;
    internal SipMessage[] OutboundMessages = new SipMessage[0];
    internal SipMessage[] DeliveredRequests = new SipMessage[0];
    internal string[] FiredTimerNames = new string[0];
}
