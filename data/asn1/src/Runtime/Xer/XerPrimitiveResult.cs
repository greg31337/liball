namespace asn1;

internal class XerPrimitiveResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal AsnRuntimeValue? Value;
    internal XerXmlToken[] Tokens = [];
    internal int ConsumedCount;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
