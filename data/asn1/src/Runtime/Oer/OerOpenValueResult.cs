namespace asn1;

internal class OerOpenValueResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal AsnTypeDescriptor? ResolvedType;
    internal AsnOpenTypeValue? OpenValue;
    internal byte[] Data = [];
    internal int ConsumedCount;
    internal bool HasViolationOffset;
    internal int ViolationOffset;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
