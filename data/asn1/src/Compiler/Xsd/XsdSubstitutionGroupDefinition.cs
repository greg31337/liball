namespace asn1;

internal class XsdSubstitutionGroupDefinition
{
    internal string StableId = "";
    internal string HeadName = "";
    internal string HeadNamespace = "";
    internal XsdDeclarationDefinition? Head;
    internal XsdDeclarationDefinition[] Members = [];
    internal string TypeReferenceName = "";
    internal string AsnTypeDefinition = "";
    internal AsnEncodingInstruction[] XerInstructions = [];
    internal AsnSourceSpan? Span;
}
