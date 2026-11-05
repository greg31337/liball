namespace asn1;

internal class SizeConstraintResult
{
    internal IntegerConstraintSet? Domain;
    internal IntegerConstraintSet? Root;
    internal IntegerConstraintSet? Extension;
    internal IntegerConstraintSet? KnownSizes;
    internal System.Numerics.BigInteger RootLowerBound;
    internal System.Numerics.BigInteger RootUpperBound;
    internal System.Numerics.BigInteger RootCardinality;
    internal bool HasRootLowerBound;
    internal bool HasRootUpperBound;
    internal bool HasRootCardinality;
    internal bool OpenExtension;
}
