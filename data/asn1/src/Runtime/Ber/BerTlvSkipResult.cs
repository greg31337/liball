namespace asn1;

internal class BerTlvSkipResult
{
    internal AsnDecodeStatus Status;
    internal int SkippedCount;
    internal AsnDiagnostic[] Diagnostics = [];
}
