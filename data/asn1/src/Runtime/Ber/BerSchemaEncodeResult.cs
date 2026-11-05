namespace asn1;

internal class BerSchemaEncodeResult
{
    internal bool Success;
    internal BerTlv? Tlv;
    internal AsnDiagnostic[] Diagnostics = [];
}
