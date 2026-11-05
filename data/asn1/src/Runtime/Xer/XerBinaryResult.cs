namespace asn1;

internal class XerBinaryResult
{
    internal bool Success;
    internal byte[] Bytes = [];
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
    internal bool LimitExceeded;
}
