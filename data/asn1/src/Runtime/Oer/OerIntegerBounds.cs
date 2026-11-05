namespace asn1;

internal class OerIntegerBounds
{
    internal bool Visible;
    internal AsnIntervalDescriptor[] Intervals = [];
    internal bool HasLowerBound;
    internal System.Numerics.BigInteger LowerBound;
    internal bool HasUpperBound;
    internal System.Numerics.BigInteger UpperBound;
}
