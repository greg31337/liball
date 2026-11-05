namespace sip;

internal class SipPublicationServerParse
{
    internal SipPublicationRequest? Request;
    internal int Operation;
    internal int StatusCode;
    internal int MinimumExpires = -1;
    internal string? ErrorMessage;
}
