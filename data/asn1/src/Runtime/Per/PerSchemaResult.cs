namespace asn1;

internal class PerSchemaResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal AsnRuntimeValue? Value;
    internal byte[] Data = [];
    internal int BitCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
