namespace sip;

internal class SipAuthenticationServerSpace
{
    internal SipCredentialScope Scope;
    internal string Realm = "";
    internal SipNonceOptions Options = new SipNonceOptions();
    internal SipDigestChallenge[] Challenges = new SipDigestChallenge[0];
    internal SipNonceReplayTracker? ReplayTracker;
}
