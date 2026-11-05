namespace asn1;

internal class XsdTypeSubstitutionDefinition
{
    internal string StableId = "";
    internal string BaseTypeName = "";
    internal string BaseTypeNamespace = "";
    internal bool Nillable;
    internal XsdTypeAlternativeDefinition[] Alternatives = [];
    internal string TypeReferenceName = "";
    internal string AsnTypeDefinition = "";
    internal AsnEncodingInstruction[] XerInstructions = [];
    internal AsnSourceSpan? Span;
}
