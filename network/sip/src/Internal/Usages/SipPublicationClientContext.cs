namespace sip;

internal class SipPublicationClientContext
{
    internal SipPublicationRequest Request = new SipPublicationRequest();
    internal SipPublicationRequest? CommittedRequest;
    internal SipMessage Message = new SipMessage();
    internal SipUri Target = new SipUri();
    internal string PublicationId = "";
    internal string CallId = "";
    internal string LocalTag = "";
    internal string? EntityTag;
    internal int CSeq;
    internal int Operation;
    internal int ExpiresSeconds = -1;
    internal long RefreshAtMilliseconds = -1;
    internal long ExpiresAtMilliseconds = -1;
    internal int AuthenticationAttemptCount;
    internal int ConditionalRetryCount;
    internal SipCredentialScope AuthenticationScope;
    internal SipPublicationState State;
}
