namespace asn1;

internal class PerBitReadResult
{
    internal AsnDecodeStatus Status;
    internal System.Numerics.BigInteger Value;
    internal byte[] Data = [];
    internal int ConsumedBitCount;
    internal int BitOffset;
    internal bool HasViolationBitOffset;
    internal int ViolationBitOffset;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
