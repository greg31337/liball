namespace asn1;

internal class ParsedInformationObjectSyntax
{
    internal AsnInformationObject Object = new AsnInformationObject();
    internal ParsedObjectFieldSettingSyntax[] FieldSettingSyntax = [];
    internal ParsedInformationFromObjectsSyntax? InformationFromObjects;
    internal ParsedParameterizedReferenceSyntax? ParameterizedReference;
    internal string ReferenceModule = "";
    internal string ReferenceName = "";
    internal bool DefaultSyntax;
    internal bool DefinedSyntax;
    internal AsnToken[] DefinedSyntaxTokens = [];
    internal AsnToken[] SourceTokens = [];
    internal ParsedRecoverySyntax[] Recoveries = [];
    internal AsnSourceSpan? Span;
}
