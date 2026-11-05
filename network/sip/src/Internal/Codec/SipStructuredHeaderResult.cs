namespace sip;

internal class SipStructuredHeaderResult
{
    internal bool Success;
    internal SipNameAddress? NameAddress;
    internal SipRouteValue[] RouteValues = new SipRouteValue[0];
    internal SipContactValue[] ContactValues = new SipContactValue[0];
    internal SipCSeqValue? CSeq;
    internal string? CallId;
    internal int MaxForwards = -1;
    internal int ErrorOffset = -1;
    internal string? ErrorMessage;
}
