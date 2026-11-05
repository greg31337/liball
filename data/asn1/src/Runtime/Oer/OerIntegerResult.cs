namespace asn1;

internal class OerIntegerResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal System.Numerics.BigInteger Value;
    internal byte[] Data = [];
    internal int ContentOctetCount;
    internal int ConsumedCount;
    internal int FixedOctetCount;
    internal bool Signed;
    internal bool Minimal = true;
    internal bool HasViolationOffset;
    internal int ViolationOffset;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
