namespace asn1;

internal class EcnContainedValueMapping
{
    internal EcnClassCategory ContainerCategory;
    internal EcnSymbol? ElementParameter;
    internal EcnReplacementDefinition? Replacement;
    internal bool Reversible;
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
