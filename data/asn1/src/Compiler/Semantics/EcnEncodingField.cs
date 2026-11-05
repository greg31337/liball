namespace asn1;

internal class EcnEncodingField
{
    internal string Name = "";
    internal bool Optional;
    internal string OptionalClassReference = "";
    internal EcnSymbol? OptionalClassSymbol;
    internal EcnEncodingStructure? Structure;
    internal AsnSourceSpan? Span;
}
