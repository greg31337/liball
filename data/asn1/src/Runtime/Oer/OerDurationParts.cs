namespace asn1;

internal class OerDurationParts
{
    internal bool Success;
    internal bool[] Present = new bool[7];
    internal System.Numerics.BigInteger[] Values =
        new System.Numerics.BigInteger[7];
    internal bool FractionPresent;
    internal int FractionDigits;
    internal System.Numerics.BigInteger FractionValue;
    internal string ErrorMessage = "";
}
