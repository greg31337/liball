namespace asn1;

internal class BerTlvReadFrame
{
    internal BerTlv Node = new BerTlv();
    internal System.Collections.Generic.List<BerTlv> Children =
        new System.Collections.Generic.List<BerTlv>();
    internal int StartOffset;
    internal long EndOffset;
    internal long DeclaredLength;
    internal bool Indefinite;
}
