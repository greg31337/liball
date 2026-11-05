namespace sip;

internal class SipTransactionRegistryEntry
{
    internal string? Id;
    internal SipTransactionRole Role;
    internal SipTransactionKey? Key;
    internal SipMessage? Request;
}
