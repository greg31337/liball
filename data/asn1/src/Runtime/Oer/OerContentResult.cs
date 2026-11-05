namespace asn1;

internal class OerContentResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal byte[] Content = [];
    internal byte[] Data = [];
    internal int ConsumedCount;
    internal bool Minimal = true;
    internal bool HasViolationOffset;
    internal int ViolationOffset;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
