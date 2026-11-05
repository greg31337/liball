namespace asn1;

internal class PerOpenTypeResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal byte[] Data = [];
    internal int BitCount;
    internal int ContentBitCount;
    internal int PaddingBitCount;
    internal int AlignmentPaddingBitCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
