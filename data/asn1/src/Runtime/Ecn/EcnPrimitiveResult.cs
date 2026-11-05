namespace asn1;

internal class EcnPrimitiveResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal AsnRuntimeValue? Value;
    internal AsnTag? Tag;
    internal byte[] Data = [];
    internal int BitCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
