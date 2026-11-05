namespace sip;

internal class SipReliableWrite
{
    internal byte[] Bytes = new byte[0];
    internal int Offset;
    internal int OfferedCount;
    internal long DeadlineMilliseconds = -1;
    internal SipCancellation? Cancellation;
    internal SipTransportMessage? Envelope;
    internal string? CorrelationId;
    internal bool KeepAlive;
    internal bool KeepAlivePong;
}
