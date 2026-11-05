namespace asn1;

internal class BerPrimitiveEncodeResult
{
    internal bool Success;
    internal BerTlv? Tlv;
    internal AsnDiagnostic[] Diagnostics = [];
}
