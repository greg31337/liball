namespace asn1;

internal class XsdComponentDefinition
{
    internal string StableId = "";
    internal string Kind = "";
    internal string Name = "";
    internal string NamespaceUri = "";
    internal string SourceDocumentId = "";
    internal string SourceInstanceId = "";
    internal bool Redefinition;
    internal XerXmlToken? Token;
    internal AsnSourceSpan? Span;
}
