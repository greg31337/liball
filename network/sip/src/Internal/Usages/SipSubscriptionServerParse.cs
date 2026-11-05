namespace sip;

internal class SipSubscriptionServerParse
{
    internal SipExtensionHeaderValue? Event;
    internal int ExpiresSeconds = -1;
    internal bool HasExpires;
    internal int StatusCode;
    internal string? ErrorMessage;
}
