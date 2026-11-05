namespace asn1;

internal class PerTableOpenTypeResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal AsnTypeDescriptor? ResolvedType;
    internal AsnOpenTypeValue? OpenValue;
    internal byte[] Data = [];
    internal int BitCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
