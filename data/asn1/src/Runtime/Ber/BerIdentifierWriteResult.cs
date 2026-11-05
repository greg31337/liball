namespace asn1;

internal class BerIdentifierWriteResult
{
    internal bool Success;
    internal bool LimitExceeded;
    internal byte[] Data = [];
    internal int WrittenCount;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
