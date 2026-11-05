namespace sip;

internal class SipInstantMessageOperation
{
    internal SipInstantMessageRequest Request = new SipInstantMessageRequest();
    internal SipMessage Message = new SipMessage();
    internal SipUri Target = new SipUri();
    internal string CallId = "";
    internal string LocalTag = "";
    internal int CSeq;
    internal int AuthenticationAttemptCount;
    internal int RedirectCount;
    internal SipCredentialScope AuthenticationScope;
    internal bool InDialog;
}
