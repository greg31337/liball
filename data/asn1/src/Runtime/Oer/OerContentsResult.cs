namespace asn1;

internal class OerContentsResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal byte[] Data = [];
    internal int ContentBitCount;
    internal AsnOpenTypeValue? OpenValue;
    internal int ConsumedCount;
    internal bool HasViolationOffset;
    internal int ViolationOffset;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
