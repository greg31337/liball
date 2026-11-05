namespace asn1;

internal class NormalizedComponentConstraintSet
{
    internal ResolvedTypeSyntax? GoverningType;
    internal NormalizedConstraintNode? SourceNode;
    internal NormalizedComponentConstraint[] Components = [];
    internal bool PartialSpecification;
    internal bool CollectionElement;
}
