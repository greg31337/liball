namespace sip;

internal class SipNonInviteClientResult
{
    internal SipTransactionState State;
    internal bool StateChanged;
    internal bool TimedOut;
    internal bool TransportFailed;
    internal bool Retransmitted;
    internal bool ResponseAbsorbed;
    internal string? ErrorMessage;
    internal SipMessage[] OutboundMessages = new SipMessage[0];
    internal SipMessage[] DeliveredResponses = new SipMessage[0];
    internal string[] FiredTimerNames = new string[0];
}
