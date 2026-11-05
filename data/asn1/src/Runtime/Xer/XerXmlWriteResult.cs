namespace asn1;

internal class XerXmlWriteResult
{
    internal bool Success;
    internal int WrittenCount;
    internal bool Complete;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
