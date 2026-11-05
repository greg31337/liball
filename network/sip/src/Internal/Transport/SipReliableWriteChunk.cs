namespace sip;

internal class SipReliableWriteChunk
{
    internal bool Available;
    internal byte[] Bytes = new byte[0];
    internal int Offset;
    internal int Count;
    internal bool KeepAlive;
    internal string? CorrelationId;
}
