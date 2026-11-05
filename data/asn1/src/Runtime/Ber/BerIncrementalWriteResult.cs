namespace asn1;

internal class BerIncrementalWriteResult
{
    internal bool Success;
    internal bool Done;
    internal int WrittenCount;
    internal long ExpectedCount;
    internal AsnDiagnostic[] Diagnostics = [];
}
