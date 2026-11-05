namespace asn1;

internal class IntegerConstraintResult
{
    internal IntegerConstraintSet? Domain;
    internal IntegerConstraintSet? Root;
    internal IntegerConstraintSet? Extension;
    internal IntegerConstraintSet? KnownValues;
    internal bool OpenExtension;
}
