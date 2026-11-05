namespace asn1;

internal class BerSchemaDecodeResult
{
    internal bool Success;
    internal AsnRuntimeValue? Value;
    internal AsnDiagnostic[] Diagnostics = [];
}
