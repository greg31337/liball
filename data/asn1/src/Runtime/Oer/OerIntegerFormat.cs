namespace asn1;

internal class OerIntegerFormat
{
    internal bool Success;
    internal bool Signed = true;
    internal int FixedOctetCount;
    internal bool HasLowerBound;
    internal System.Numerics.BigInteger LowerBound;
    internal bool HasUpperBound;
    internal System.Numerics.BigInteger UpperBound;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
