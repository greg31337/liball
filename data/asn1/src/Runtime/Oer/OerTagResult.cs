namespace asn1;

internal class OerTagResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal AsnTag? Tag;
    internal byte[] Data = [];
    internal int ConsumedCount;
    internal bool HasViolationOffset;
    internal int ViolationOffset;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
