namespace asn1;

internal class NormalizedConstraintPartition
{
    internal ConstraintDomainIdentity? Domain;
    internal NormalizedConstraintNode? Expression;
    internal IntegerConstraintSet? IntegerSet;
    internal bool Extension;
    internal bool Open;
}
