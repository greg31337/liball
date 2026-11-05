namespace sip;

internal class SipReferServerContext
{
    internal SipMessage Refer = new SipMessage();
    internal SipNameAddress ReferTo = new SipNameAddress();
    internal SipDialogId DialogId = new SipDialogId();
    internal string SubscriptionId = "";
    internal int NotificationCSeq;
    internal int ReferCSeq;
    internal int ExpiresSeconds;
    internal string? CorrelationId;
}
