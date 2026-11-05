namespace asn1;

internal class PerUsefulTypeResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal AsnRuntimeValue? Value;
    internal int BitCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
