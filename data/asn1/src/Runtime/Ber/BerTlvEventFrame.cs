namespace asn1;

internal class BerTlvEventFrame
{
    internal BerTlv Node = new BerTlv();
    internal System.Collections.Generic.List<BerTlv> Children =
        new System.Collections.Generic.List<BerTlv>();
}
