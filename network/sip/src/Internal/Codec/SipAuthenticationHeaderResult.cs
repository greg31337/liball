namespace sip;

internal class SipAuthenticationHeaderResult
{
    internal bool Success;
    internal SipDigestChallenge? Challenge;
    internal SipDigestAuthorization? Authorization;
    internal SipBearerToken? BearerToken;
    internal int ErrorOffset = -1;
    internal string? ErrorMessage;
}
