namespace asn1;

internal class BerTlvSizeFrame
{
    internal BerTlv Node = new BerTlv();
    internal int NextChildIndex;
    internal long ContentLength;
    internal bool NestingCharged;
}
