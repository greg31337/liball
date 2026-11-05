namespace sip;

internal class SipEndpointTransaction
{
    internal string? Id;
    internal string? Branch;
    internal string? CallId;
    internal int CSeq = -1;
    internal SipMethod Method;
    internal SipTransactionRole Role;
    internal SipTransactionState State;
    internal SipSendRequest? SendRequest;
}
