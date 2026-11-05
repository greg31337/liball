namespace asn1;

internal class PerBitWriteResult
{
    internal bool Success;
    internal byte[] Data = [];
    internal int WrittenBitCount;
    internal int PaddedBitCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
