namespace sip;

internal class SipReferClientContext
{
    internal SipReferRequest Request = new SipReferRequest();
    internal SipMessage Message = new SipMessage();
    internal string CallId = "";
    internal int CSeq;
    internal int AuthenticationAttempts;
    internal SipCredentialScope AuthenticationScope;
    internal string SubscriptionId = "";
    internal SipDialogId? DialogId;
    internal bool ReferResponseFinal;
    internal bool SubscriptionFinal;
}
