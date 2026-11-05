namespace sip;

internal class SipDnsTarget
{
    internal bool Success;
    internal string? Domain;
    internal int Port = -1;
    internal SipTransportKind Transport;
    internal bool Secure;
    internal bool ExplicitPort;
    internal bool ExplicitTransport;
    internal string? ErrorMessage;
}
