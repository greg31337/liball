namespace asn1;

internal class BerPrimitiveDecodeResult
{
    internal bool Success;
    internal AsnRuntimeValue? Value;
    internal AsnDiagnostic[] Diagnostics = [];
}
