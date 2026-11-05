namespace asn1;

internal class ParsedObjectSetElementSyntax
{
    internal string Kind = "";
    internal string ReferenceModule = "";
    internal string ReferenceName = "";
    internal ParsedInformationObjectSyntax? ObjectSyntax;
    internal ParsedInformationFromObjectsSyntax? InformationFromObjects;
    internal ParsedParameterizedReferenceSyntax? ParameterizedReference;
    internal ParsedObjectSetElementSyntax[] Children = [];
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
