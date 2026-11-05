namespace asn1;

internal class PerAlternativeResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal bool Extension;
    internal int Index;
    internal byte[] Data = [];
    internal int ContentBitCount;
    internal int BitCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
