namespace asn1;

internal class XsdDeclarationTypeUse
{
    internal string StableId = "";
    internal string Name = "";
    internal string NamespaceUri = "";
    internal string Kind = "";
    internal string AsnTypeDefinition = "";
    internal bool Anonymous;
    internal bool BuiltIn;
    internal bool PendingComplexMapping;
    internal XsdSimpleTypeDefinition? SimpleType;
    internal XsdComponentDefinition? ComplexType;
    internal XsdComplexTypeDefinition? MappedComplexType;
    internal XerXmlToken? Token;
    internal AsnSourceSpan? Span;
    internal AsnEncodingInstruction[] XerInstructions = [];
}
