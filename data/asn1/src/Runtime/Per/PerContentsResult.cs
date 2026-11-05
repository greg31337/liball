namespace asn1;

internal class PerContentsResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal byte[] Data = [];
    internal int ContentBitCount;
    internal AsnOpenTypeValue? OpenValue;
    internal int BitCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
