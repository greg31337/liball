namespace asn1;

internal class OerCollectionResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal AsnRuntimeValue? Value;
    internal byte[] Data = [];
    internal int ConsumedCount;
    internal bool HasViolationOffset;
    internal int ViolationOffset;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
