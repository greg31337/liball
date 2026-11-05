namespace sip;

internal class SipManagedTransport
{
    internal ISipTransport? Transport;
    internal SipTransportKind Kind;
    internal bool Closed;
}
