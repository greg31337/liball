namespace asn1;

internal class BerEndOfContentsResult
{
    internal AsnDecodeStatus Status;
    internal bool Matched;
    internal bool LimitExceeded;
    internal int ConsumedCount;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
