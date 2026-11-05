namespace asn1;

internal class XsdWildcardDefinition
{
    internal string StableId = "";
    internal string Kind = "";
    internal string ProcessContents = "";
    internal string MappingMode = "";
    internal string NamespaceConstraint = "";
    internal string[] NamespaceTokens = [];
    internal XsdDeclarationDefinition[] AllowedElements = [];
    internal bool IncludesFallback;
    internal bool Pending;
    internal string AsnTypeDefinition = "";
    internal AsnConstraint[] Constraints = [];
    internal AsnEncodingInstruction[] XerInstructions = [];
    internal XerXmlToken? Token;
    internal AsnSourceSpan? Span;
}
