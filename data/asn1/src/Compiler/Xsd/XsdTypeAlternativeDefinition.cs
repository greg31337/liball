namespace asn1;

internal class XsdTypeAlternativeDefinition
{
    internal string StableId = "";
    internal string Name = "";
    internal string NamespaceUri = "";
    internal string Kind = "";
    internal string AsnTypeDefinition = "";
    internal bool Nillable;
    internal XsdSimpleTypeDefinition? SimpleType;
    internal XsdComplexTypeDefinition? ComplexType;
    internal AsnEncodingInstruction[] XerInstructions = [];
}
