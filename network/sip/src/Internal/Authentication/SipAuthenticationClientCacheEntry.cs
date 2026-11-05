namespace sip;

internal class SipAuthenticationClientCacheEntry
{
    internal SipCredentialScope Scope;
    internal string TargetKey = "";
    internal string Realm = "";
    internal SipDigestChallenge Challenge = new SipDigestChallenge();
    internal SipDigestQuality Quality;
    internal long NonceCount;
    internal string? ClientNonce;
    internal SipDigestCalculation? LastCalculation;
    internal string? CorrelationId;
}
