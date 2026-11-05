namespace asn1;

internal class PerWholeNumberResult
{
    internal bool Success;
    internal AsnDecodeStatus Status;
    internal System.Numerics.BigInteger Value;
    internal byte[] Data = [];
    internal int BitCount;
    internal int OctetCount;
    internal bool OctetAligned;
    internal bool RequiresLengthDeterminant;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
