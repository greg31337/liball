namespace sip;

internal class SipAuthenticationAuthorizationSet
{
    internal bool Found;
    internal bool Multiple;
    internal bool Malformed;
    internal SipDigestAuthorization? Authorization;
    internal SipBearerToken? BearerToken;
}
