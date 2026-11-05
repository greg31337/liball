namespace sip;

internal class SipDnsCacheEntry
{
    internal string? Key;
    internal SipResolutionResult? Result;
    internal long ExpiresAtMilliseconds;
}
