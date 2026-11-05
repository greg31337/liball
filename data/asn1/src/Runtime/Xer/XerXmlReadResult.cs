namespace asn1;

internal class XerXmlReadResult
{
    internal AsnDecodeStatus Status;
    internal XerXmlToken[] Tokens = [];
    internal int ConsumedCount;
    internal bool Complete;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
    internal int ViolationOffset;
}
