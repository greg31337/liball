namespace asn1;

internal class BerIdentifierReadResult
{
    internal AsnDecodeStatus Status;
    internal AsnTag? Tag;
    internal bool Constructed;
    internal bool Minimal = true;
    internal bool LimitExceeded;
    internal int ConsumedCount;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
