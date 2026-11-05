namespace asn1;

internal class BerLengthReadResult
{
    internal AsnDecodeStatus Status;
    internal long Length;
    internal bool Indefinite;
    internal bool Minimal = true;
    internal bool LimitExceeded;
    internal int ConsumedCount;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
