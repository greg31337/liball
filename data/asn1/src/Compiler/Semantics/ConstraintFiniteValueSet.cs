namespace asn1;

internal class ConstraintFiniteValueSet
{
    internal ResolvedValueSyntax[] Values = [];
    internal NormalizedConstraintGraph? SourceGraph;
    internal bool Complete;
}
