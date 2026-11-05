namespace sip;

internal class SipInviteServerResult
{
    internal SipTransactionState State;
    internal bool StateChanged;
    internal bool TimedOut;
    internal bool TransportFailed;
    internal bool Retransmitted;
    internal bool RequestAbsorbed;
    internal bool AckAbsorbed;
    internal bool AckDelivered;
    internal bool ResponseDiscarded;
    internal bool CancelMatched;
    internal bool CancellationRequested;
    internal bool InviteTerminated;
    internal string? ErrorMessage;
    internal SipMessage[] OutboundMessages = new SipMessage[0];
    internal SipMessage[] DeliveredRequests = new SipMessage[0];
    internal string[] FiredTimerNames = new string[0];
}
