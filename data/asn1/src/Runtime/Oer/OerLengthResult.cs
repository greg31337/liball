namespace asn1;

internal class OerLengthResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal long Length;
    internal bool Minimal = true;
    internal byte[] Data = [];
    internal int ConsumedCount;
    internal int TotalConsumedCount;
    internal bool HasViolationOffset;
    internal int ViolationOffset;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
