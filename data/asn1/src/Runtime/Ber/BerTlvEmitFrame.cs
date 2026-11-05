namespace asn1;

internal class BerTlvEmitFrame
{
    internal BerTlv Node = new BerTlv();
    internal int NextChildIndex;
    internal bool HeaderWritten;
}
