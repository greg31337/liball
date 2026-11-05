namespace asn1;

internal class EcnEncodingClassDefinition
{
    internal EcnEncodingClassDefinitionKind Kind;
    internal string Name = "";
    internal string AliasClassReference = "";
    internal EcnSymbol? Symbol;
    internal EcnSymbol? AliasSymbol;
    internal EcnEncodingStructure? Structure;
    internal EcnActualParameter[] AliasActualParameters = [];
    internal bool AliasBuiltIn;
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
