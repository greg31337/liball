namespace asn1;

internal class XerValueResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal AsnRuntimeValue? Value;
    internal XerXmlToken[] Tokens = [];
    internal int ConsumedCount;
    internal bool LimitExceeded;
    internal bool HasViolationOffset;
    internal int ViolationOffset;
    internal string ValuePath = "";
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
