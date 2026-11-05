namespace asn1;

internal class ParsedInformationObjectSetSyntax
{
    internal AsnInformationObjectSet ObjectSet = new AsnInformationObjectSet();
    internal ParsedObjectSetElementSyntax? RootElement;
    internal ParsedObjectSetElementSyntax? AdditionalElement;
    internal ParsedParameterizedReferenceSyntax? ParameterizedReference;
    internal ParsedInformationFromObjectsSyntax? InformationFromObjects;
    internal EvaluatedObjectSetMember[] RootMembers = [];
    internal EvaluatedObjectSetMember[] AdditionalMembers = [];
    internal AsnToken[] SourceTokens = [];
    internal ParsedRecoverySyntax[] Recoveries = [];
    internal AsnSourceSpan? Span;
}
